#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"

#load "./Messages.fsx"

open System
open System.Data
open Akka.FSharp
open Akka.Actor
open System.Text.RegularExpressions
open System.Collections.Generic
open Messages

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

let dictOfUsers = new Dictionary<string, IActorRef>()
let statusOfUsers = new Dictionary<string, Boolean>()
let mutable tweetCount = 0
let mutable retweetCount = 0

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
    with ex -> printfn "Could not register:  %A" ex

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
        printfn "~ %A" i
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

let Server(mailbox: Actor<obj>) msg =
    let sender = mailbox.Sender()
    match box msg with 
    | :? RegisterRequest as r ->
        let response: RegisterationResponse = {response = registerUser(r.username, r.password)} 
        dictOfUsers.Add(r.username, sender)
        statusOfUsers.Add(r.username, true)
        sender <! response
    | :? Login as  l ->
        statusOfUsers.[l.username] <- true
        sender <! loginResponse
    | :? Logout as l ->
        statusOfUsers.[l.username] <- false
        sender <! logoutResponse
    | :? Followers as f ->
        let followers: MyFollowers = {
            followers = getMyFollowers(f.username)
        }
        sender <! followers
    | :? FollowRequest as f ->
        let followRes: FollowResponse = {
            response = follow(f.followee, f.follower)
        }
        sender <! followRes
    | :? Tweet as t -> 
        tweetCount <- tweetCount + 1
        tweetIt(t.username, tweetCount, t.text)
        let allMyFollowers = getMyFollowers(t.username)
        for eachFollower in allMyFollowers do
            let newTweet: NewTweet = {
                username = t.username
                messageId = tweetCount
                message = t.text
            }
            dictOfUsers.Item(eachFollower) <! newTweet
    | :? ReTweet as r -> 
        retweetCount <- retweetCount + 1
        retweetIt(r.username, r.tweetId, retweetCount) |> ignore
    | :? Feed as f ->
        let myFeed: MyFeed = {
            tweets = getMyTweets(f.username)
            mentions = getMyMentions(f.username)
        }
        sender <! myFeed
    | _ -> printfn "Invalid response(Server)"

let serverId = "server"
let _ = spawn system serverId (actorOf2(Server))
system.WhenTerminated.Wait()




