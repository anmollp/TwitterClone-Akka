#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"

#load "./Messages.fsx"
#load "./Utilities.fsx"

open System
open System.Data
open Akka.FSharp
open Akka.Actor
open System.Text.RegularExpressions
open System.Collections.Generic
open Messages
open Utilities
open FSharp.Json

let loginResponse: LoginResponse = {
    message = "You've successfully logged in!"
}

let logoutResponse: LogoutResponse = {
    message = "You've successfully logged out!"
}

let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern) in
    if m.Success then Some (List.tail [ for g in m.Groups -> g.Value ]) else None

let users = new DataTable()
users.Columns.Add("Username", typeof<string>);
users.Columns.Add("Password", typeof<string>);

users.PrimaryKey <- [|users.Columns.["Username"]|]

let tweets = new DataTable()
tweets.Columns.Add("Username", typeof<string>)
tweets.Columns.Add("TweetId", typeof<int>)
tweets.Columns.Add("Tweet", typeof<string>)

tweets.PrimaryKey <- [|tweets.Columns.["TweetId"]|]

let retweets = new DataTable()
retweets.Columns.Add("Username", typeof<string>)
retweets.Columns.Add("ReTweetId", typeof<int>)
retweets.Columns.Add("TweetId", typeof<int>)

retweets.PrimaryKey <- [|tweets.Columns.["ReTweetId"]|]

let followers = new DataTable()
followers.Columns.Add("Username", typeof<string>)
followers.Columns.Add("Follower", typeof<string>)

followers.PrimaryKey <- [|followers.Columns.["Username"]; followers.Columns.["Follower"]|]

let mentions = new DataTable()
mentions.Columns.Add("Username", typeof<string>)
mentions.Columns.Add("Mentioner", typeof<string>)
mentions.Columns.Add("Tweet", typeof<string>)

let registerUser(username: string, password: string) = 
    let row = users.NewRow()
    let mutable response = "Registration Successful"
    row.SetField("Username",username)
    row.SetField("Password",password)
    try 
        users.Rows.Add(row)
    with ex -> response <- "Could not register: " + string(ex)
    response

let getRandomUser() =
    let expr = ""
    let usrs = (users.Select(expr))
    let userSeq = seq { yield! usrs}
    let mutable usersList = []
    for i in userSeq do
        let user = (i.Field(users.Columns.Item(1)))
        usersList <- usersList @ [user]
    getRandomElement(usersList)

let getRandomTweetId() =
    let expr = ""
    let twits = (tweets.Select(expr))
    let twitSeq = seq { yield! twits}
    let mutable tweetList = []
    for i in twitSeq do
        let twitId = (i.Field(tweets.Columns.Item(1)))
        tweetList <- tweetList @ [twitId]
    getRandomIntegerElement(tweetList)

let tweetIt(username: string, tweetCount: int, tweet: string) = 
    let row = tweets.NewRow()
    row.SetField("TweetId", tweetCount)
    row.SetField("Username", username)
    row.SetField("Tweet", tweet)
    try 
        tweets.Rows.Add(row)
    with ex -> printfn "Could not tweet:  %A" ex

    if tweet.Contains("@") then
        match tweet with
        | Regex "@([0-9A-Za-z]*)" [ mention ] -> 
            let row = mentions.NewRow()
            row.SetField("Username", mention)
            row.SetField("Mentioner", username)
            row.SetField("Tweet", tweet)
            mentions.Rows.Add(row) |> ignore
        | _ -> printfn "Not a username"
        
let retweetIt(username: string, tweetId: int, retweetCount: int) = 
    let row = retweets.NewRow()
    row.SetField("ReTweetId", retweetCount)
    row.SetField("TweetId", tweetId)
    row.SetField("Username", username)
    try 
        retweets.Rows.Add(row)
    with ex -> printfn "Could not retweet:  %A" ex

let follow(followee: string, follower: string) =
    let mutable response = String.Format("You started following {0}.", followee)
    let row = followers.NewRow()
    row.SetField("Username", followee)
    row.SetField("Follower", follower)
    try 
        followers.Rows.Add(row)
    with ex -> response <- "Could not follow: " + string(ex)
    response

let getMyFollowers(username: string) =
    let expr = "Username = '"+username+"'"
    let followrs = (followers.Select(expr))
    let followSeq = seq { yield! followrs}
    let mutable followersList = []
    for i in followSeq do
        let followr = (i.Field(followers.Columns.Item(1)))
        followersList <- followersList @ [followr]
    followersList

let getMyTweets(username: string) =
    let expr = "Username = '"+username+"'"
    let twits = (tweets.Select(expr))
    let tweetSeq = seq { yield! twits}
    let mutable tweetList = []
    let mutable tweetIdList = []
    for i in tweetSeq do
        let twitId = (i.Field(tweets.Columns.Item(1)))
        tweetIdList <- tweetIdList @ [twitId]
        let twit = (i.Field(tweets.Columns.Item(2)))
        tweetList <- tweetList @ [twit]
    List.zip tweetIdList tweetList

let getRetweetsFromTweets(tweetIdList: list<int>) =
    let s = String.Join(",", tweetIdList)
    if tweetIdList.Length = 0 then
        []
    else
        let expr = "TweetId IN ("+s+")"
        let twits = (tweets.Select(expr))
        let tweetSeq = seq { yield! twits}
        let mutable tweetList = []
        for i in tweetSeq do
            let twit = (i.Field(tweets.Columns.Item(2)))
            tweetList <- tweetList @ [twit] 
        tweetList

let getMyReTweets(username: string) =
    let expr = "Username = '"+username+"'"
    let retwits = (retweets.Select(expr))
    let retweetSeq = seq { yield! retwits}
    let mutable retweetList = []
    let mutable retweetIdList = []
    for i in retweetSeq do
        let retwitId = (i.Field(retweets.Columns.Item(1)))
        retweetIdList <- retweetIdList @ [retwitId]
    retweetList <- getRetweetsFromTweets(retweetIdList)
    List.zip retweetIdList retweetList

let getMyMentions(username: string) =
    let expr = "Username = '"+username+"'"
    let ments = (mentions.Select(expr))
    let mentionSeq = seq { yield! ments}
    let mutable mentionList = []
    let mutable mentionerList = []
    for i in mentionSeq do
        let mentioner = (i.Field(mentions.Columns.Item(1)))
        mentionerList <- mentionerList @ [mentioner]
        let mention = (i.Field(mentions.Columns.Item(2)))
        mentionList <- mentionList @ [mention]
    List.zip mentionerList mentionList

let searchHashTag(hashTag: string) =
    let expr = "Tweet LIKE '*"+hashTag+"*'"
    let hashTags = (tweets.Select(expr))
    let hashTagsSeq = seq{ yield! hashTags}
    let mutable searchList = []
    let mutable searchIdList = []
    for i in hashTagsSeq do
        let tagId = (i.Field(tweets.Columns.Item(1)))
        searchIdList <- searchIdList @ [tagId]
        let tagTweet = (i.Field(tweets.Columns.Item(2)))
        searchList <- searchList @ [tagTweet]
    List.zip searchIdList searchList

let Server (mailbox: Actor<_>) =
    let mutable tweetCount = 0
    let mutable retweetCount = 0
    let dictOfUsers = new Dictionary<string, IActorRef>()
    let statusOfUsers = new Dictionary<string, Boolean>()
    let rec loop () = 
        actor {
        let! json = mailbox.Receive()
        let x = Json.deserialize<Dto> json
        let msg = x.message
        match msg with 
        | "RegisterRequest" as r ->
            let dto: Dto = {
                    message = "RegisterationResponse"
                    username = ""
                    password = ""
                    response = registerUser(x.username, x.password)
                    followers = []
                    tweetId = -1
                    tweet = ""
                    tweets = [(-1,"")]
                    mentions = [("","")]
                    retweets = [(-1,"")]
                    tagTweets = [(-1,"")]
                    logoutMessage = ""
                    followee = ""
                    follower = ""
                    tag = ""
                }
            dictOfUsers.Add(x.username, mailbox.Sender())
            statusOfUsers.Add(x.username, true)
            mailbox.Sender() <! Json.serialize dto
        | "Login" as  l ->
            statusOfUsers.[x.username] <- true
            mailbox.Sender() <! loginResponse
        | "Logout" as l ->
            statusOfUsers.[x.username] <- false
            mailbox.Sender() <! logoutResponse
        | "Followers" as f ->
            let followers: MyFollowers = {
                followers = getMyFollowers(x.username)
            }
            mailbox.Sender() <! followers
        | "FollowRequest" as f ->
            let followRes: FollowResponse = {
                response = follow(x.followee, x.follower)
            }
            mailbox.Sender() <! followRes
        | "Tweet" as t -> 
            tweetCount <- tweetCount + 1
            tweetIt(x.username, tweetCount, x.tweet)
            let allMyFollowers = getMyFollowers(x.username)
            for eachFollower in allMyFollowers do
                let newTweet: NewTweet = {
                    username = x.username
                    messageId = tweetCount
                    message = x.tweet
                }
                dictOfUsers.Item(eachFollower) <! newTweet
        | "ReTweet" as r -> 
            retweetCount <- retweetCount + 1
            retweetIt(x.username, x.tweetId, retweetCount) |> ignore
            let allMyFollowers = getMyFollowers(x.username)
            let retwit = getRetweetsFromTweets([x.tweetId])
            for eachFollower in allMyFollowers do
                let newReTweet: NewReTweet = {
                    username = x.username
                    messageId = retweetCount
                    message = retwit.[0]
                }
                dictOfUsers.Item(eachFollower) <! newReTweet
        | "Feed" as f ->
            let myFeed: MyFeed = {
                tweets = getMyTweets(x.username)
                mentions = getMyMentions(x.username)
            }
            mailbox.Sender() <! myFeed
        | "SearchTag" as s ->
            let searchResponse: HashTagSearchResponse = {
                tagTweets = searchHashTag(x.tag)
            }
            mailbox.Sender() <! searchResponse
        | "ShowRetweets" as s ->
            let retweets: RetweetsResponse = {
                retweets = getMyReTweets(x.username)
            }
            mailbox.Sender() <! retweets
        | _ -> printfn "Invalid response(Server)"

        return! loop ()
        }
    loop ()

let main() =
    let configuration =
        Configuration.parse
            @"akka {
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    }
                remote.helios.tcp {
                    hostname = 10.20.215.4
                    // hostname = 10.140.238.225
                    port = 2552
                }
            }"
    let system = System.create "TwitterServer" configuration
    printfn "Entering"
    let serverId = "server"
    let _ = spawn system serverId (Server)
    system.WhenTerminated.Wait()

if fsi.CommandLineArgs = [| __SOURCE_FILE__ |] then
    printfn "Called"
    main()




