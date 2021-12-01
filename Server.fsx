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

let mutable tweetCount = 0
let mutable retweetCount = 0
let dictOfUsers = new Dictionary<string, IActorRef>()
let statusOfUsers = new Dictionary<string, Boolean>()
let statsDictionary = Dictionary<string, int>()

let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern) in
    if m.Success then Some (List.tail [ for g in m.Groups -> g.Value ]) else None

let users = new DataTable()
users.Columns.Add("Username", typeof<string>)
users.Columns.Add("Password", typeof<string>)

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
    let mutable response = "Registration Successful"
    let row = users.NewRow()
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
        let user = (i.Field(users.Columns.Item(0)))
        usersList <- usersList @ [user]
    getRandomElement(usersList)

let getNRandomUsers(username: string, n: int) =
    let expr = "Username <> '"+username+"'"
    let usrs = (users.Select(expr))
    let userSeq = seq { yield! usrs}
    let mutable usersList = []
    for i in userSeq do
        let user = string(i.Field(users.Columns.Item(0)))
        usersList <- usersList @ [user]
    let mutable set = Set.empty
    while set.Count <> n do
        set <- set.Add(getRandomNum(0, n))
    let mutable nRandomUsers = []
    for element in set do
        try
            nRandomUsers <- nRandomUsers @ [usersList.Item(element)]
        with ex -> printfn "Exception: %A" ex
    nRandomUsers

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
    with ex -> response <- String.Format("You can only follow @{0} once.", followee)
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

let getTweetsForReTweets(tweetIdList: list<int>) =
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
    let mutable tweetIdList = []
    for i in retweetSeq do
        let twitId = (i.Field(retweets.Columns.Item(2)))
        tweetIdList <- tweetIdList @ [twitId]
    retweetList <- getTweetsForReTweets(tweetIdList)
    if retweetList.Length > 0 then
        List.zip (tweetIdList.[0 .. retweetList.Length-1]) (retweetList)
    else List.zip [] []

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

let getMyStats(username: string) = 
    let mutable expr = "Username = '"+username+"'"
    let mutable activityMetric = 0
    let twits = (tweets.Select(expr))
    let twitSeq = seq { yield! twits}
    let mutable twitIdList = []
    for i in twitSeq do
        let twit = (i.Field(tweets.Columns.Item(1)))
        twitIdList <- twitIdList @ [string(twit)]
    if twitIdList.Length > 0 then
        let s = String.Join(",", twitIdList)
        expr <- "TweetId IN ("+s+") and Username <> '"+username+"'"
        let retwits = (retweets.Select(expr))
        activityMetric <- activityMetric + retwits.Length
    expr <- "Username ='"+username+"'"
    let myretwits = (retweets.Select(expr))
    let myretwitSeq = seq { yield! myretwits}
    let mutable myretwitIdList = []
    for i in myretwitSeq do
        let myretwit = (i.Field(retweets.Columns.Item(1)))
        myretwitIdList <- myretwitIdList @ [string(myretwit)]
    if myretwitIdList.Length > 0 then
        let t = String.Join(",", myretwitIdList)
        expr <- "TweetId IN ("+t+") and Username <> '"+username+"'"
        let retwits = (retweets.Select(expr))
        activityMetric <- activityMetric + retwits.Length
    activityMetric   

let getTotalSystemMessages() =
    let tot_tweets = tweets.Rows.Count
    let tot_retweets = tweets.Rows.Count
    tot_retweets+tot_retweets

let Server (mailbox: Actor<_>) =
    let rec loop () = 
        actor {
        let! json = mailbox.Receive()
        let x = Json.deserialize<Dto> json
        let msg = x.message
        match msg with 
        | "RegisterRequest" as r ->
            let dto: Dto = {
                    message = "RegisterationResponse"
                    username = x.username
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
            let dto: Dto = {
                    message = "LoginResponse"
                    username = ""
                    password = ""
                    response = "You've successfully logged in!"
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
            mailbox.Sender() <! Json.serialize dto
        | "Logout" as l ->
            statusOfUsers.[x.username] <- false
            let dto: Dto = {
                    message = "LogoutResponse"
                    username = ""
                    password = ""
                    response = "You've successfully logged out!"
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
            mailbox.Sender() <! Json.serialize dto
        | "Followers" as f ->
            let dto: Dto = {
                    message = "MyFollowers"
                    username = ""
                    password = ""
                    response = ""
                    followers = getMyFollowers(x.username)
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
            mailbox.Sender() <! Json.serialize dto
        | "FollowRequest" as f ->
            let dto: Dto = {
                    message = "FollowResponse"
                    username = x.username
                    password = ""
                    response = follow(x.followee, x.follower)
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
            mailbox.Sender() <! Json.serialize dto
        | "Tweet" -> 
            tweetCount <- tweetCount + 1
            tweetIt(x.username, tweetCount, x.tweet)
            let allMyFollowers = getMyFollowers(x.username)
            for eachFollower in allMyFollowers do
                let dto: Dto = {
                        message = "NewTweet"
                        username = x.username
                        password = ""
                        response = ""
                        followers = []
                        tweetId = tweetCount
                        tweet = x.tweet
                        tweets = []
                        mentions = [("","")]
                        retweets = [(-1,"")]
                        tagTweets = [(-1,"")]
                        logoutMessage = ""
                        followee = ""
                        follower = ""
                        tag = ""
                    }
                dictOfUsers.Item(eachFollower) <! Json.serialize dto
        | "ReTweet" as r -> 
            retweetCount <- retweetCount + 1
            retweetIt(x.username, x.tweetId, retweetCount) |> ignore
            let allMyFollowers = getMyFollowers(x.username)
            let retwit = getTweetsForReTweets([x.tweetId])
            for eachFollower in allMyFollowers do
                let dto: Dto = {
                        message = "NewReTweet"
                        username = x.username
                        password = ""
                        response = ""
                        followers = []
                        tweetId = retweetCount
                        tweet = if retwit.Length > 0 then retwit.[0] else ""
                        tweets = []
                        mentions = [("","")]
                        retweets = [(-1,"")]
                        tagTweets = [(-1,"")]
                        logoutMessage = ""
                        followee = ""
                        follower = ""
                        tag = ""
                    }
                dictOfUsers.Item(eachFollower) <! Json.serialize dto
        | "Feed" as f ->
            let dto: Dto = {
                    message = "MyFeed"
                    username = x.username
                    password = ""
                    response = ""
                    followers = []
                    tweetId = -1
                    tweet = ""
                    tweets = getMyTweets(x.username)
                    mentions = getMyMentions(x.username)
                    retweets = [(-1,"")]
                    tagTweets = [(-1,"")]
                    logoutMessage = ""
                    followee = ""
                    follower = ""
                    tag = ""
                    }
            mailbox.Sender() <! Json.serialize dto
        | "SearchTag" as s ->
            let dto: Dto = {
                    message = "HashTagSearchResponse"
                    username = ""
                    password = ""
                    response = ""
                    followers = []
                    tweetId = -1
                    tweet = ""
                    tweets = []
                    mentions = []
                    retweets = [(-1,"")]
                    tagTweets = searchHashTag(x.tag)
                    logoutMessage = ""
                    followee = ""
                    follower = ""
                    tag = x.tag
                    }
            mailbox.Sender() <! Json.serialize dto
        | "ShowRetweets" as s ->
            let dto: Dto = {
                    message = "RetweetsResponse"
                    username = ""
                    password = ""
                    response = ""
                    followers = []
                    tweetId = -1
                    tweet = ""
                    tweets = []
                    mentions = []
                    retweets = getMyReTweets(x.username)
                    tagTweets = [(-1,"")]
                    logoutMessage = ""
                    followee = ""
                    follower = ""
                    tag = ""
                    }
            mailbox.Sender() <! Json.serialize dto
        | "RandomUsers" ->
            let dto: Dto = {
                    message = "RandomUsersResponse"
                    username = x.username
                    password = ""
                    response = ""
                    followers = getNRandomUsers(x.username, x.tweetId)
                    tweetId = -1
                    tweet = ""
                    tweets = []
                    mentions = []
                    retweets = []
                    tagTweets = [(-1,"")]
                    logoutMessage = ""
                    followee = ""
                    follower = ""
                    tag = ""
                    }
            mailbox.Sender() <! Json.serialize dto
        | "MyStats" ->
            printfn "#################################################################"
            for element in dictOfUsers do
                let metrics = getMyStats(element.Key)
                printfn "_____________________________________________________________"
                printfn "|%s's activity metrics are %d|" element.Key metrics
            printfn "#################################################################"
        | "SystemStats" ->
            printfn "Total message count in the system during the entire simulation process: %d" (getTotalSystemMessages())
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
                    port = 2552
                }
            }"
    let system = System.create "TwitterServer" configuration
    let serverId = "server"
    let _ = spawn system serverId (Server)
    system.WhenTerminated.Wait()

if fsi.CommandLineArgs = [| __SOURCE_FILE__ |] then
    main()




