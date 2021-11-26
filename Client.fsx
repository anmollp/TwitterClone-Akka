#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"

#load "./Messages.fsx"
#load "./RandTweets.fsx"
#load "./Utilities.fsx"
#load "./Server.fsx"

open Akka.Configuration
open Akka.FSharp
open Akka.Actor
open Messages
open RandTweets
open Utilities
open Server
open System.Diagnostics
open System.Collections.Generic
open FSharp.Json

let configuration =
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote.helios.tcp {
                hostname = localhost
                port = 9000
            }
        }"
    )

let mutable ipAddress = ""
let mutable clientId = ""
let mutable numNodes = 0
let mutable numMessages = 0
let actorList =  List<IActorRef>()
let config = JsonConfig.create(allowUntyped = true)

match fsi.CommandLineArgs with 
    | [|_; ip; nodes; messages|] -> 
        ipAddress <- ip
        numNodes <- int(nodes)
        numMessages <- int(messages)
    | _ -> printfn "Error: Invalid Arguments."

let homepage = "
Welcome to Chirp! What would you like to do today?
Press the corresponding number for action
[1] Tweet
[2] Retweet
[3] Followers
[4] Follow
[5] Feed
[6] Search a HashTag
[7] Your Retweets
[8] Logout
"

let logoutpage = "
Welcome to Chirp! What would you like to do today?
Press the corresponding number for action
[9] Login
"

let remoteSys = System.create "TwitterClient" configuration
let server = remoteSys.ActorSelection("akka.tcp://TwitterServer@"+ipAddress+":2552/user/server")

let home : Dto = {
    message = "Home"
    username = ""
    password = ""
    response = ""
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

let User(userName: string)(mailbox: Actor<_>) =
    let rec loop() =
        actor {
            let! json = mailbox.Receive()
            let x = Json.deserialize<Dto> json
            let msg = x.message
            match msg with
            | "Register" as r ->
                printfn "Register with an username and password to continue on Chirp! ..."
                printf "Username: /"
                printf "Password: "
                let userpassword = ranStr 10
                let dto: Dto = {
                    message = "RegisterRequest"
                    username = userName
                    password = userpassword
                    response = ""
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
                server.Tell(Json.serialize dto, mailbox.Self)
            | "RegisterationResponse" as r ->
                printfn "%s" x.response
                mailbox.Self <! Json.serialize home
            | "LoginResponse" as l ->
                printfn "%s" x.response
                mailbox.Self <! home
            | "FollowResponse" as f ->
                printfn "%s" x.response
                mailbox.Self <! home
            | "MyFollowers" as m ->
                if x.followers.Length = 0 then
                    printfn "You dont have any followers..."
                else
                    printfn "Your followers:"
                    for i in x.followers do
                        printfn "%s" i
                mailbox.Self <! home
            | "NewTweet" as n ->
                printfn "Here's a new tweet from @%s" x.username
                printfn "<%d> %s" x.tweetId x.tweet
                mailbox.Self <! home
            | "NewReTweet" as r ->
                printfn "Here's a new retweet from @%s" x.username
                printfn "<%d> %s" x.tweetId x.tweet
                mailbox.Self <! home
            | "Feed" as f -> 
                let feed : Feed = {
                    username = userName
                }
                server.Tell(feed, mailbox.Self)
            | "MyFeed" as m ->
                printfn "Your recent tweets"
                for i in x.tweets do
                    let (id, data) = i
                    printfn "<%d> %s" id data
                printfn "Your recent mentions"
                for i in x.mentions do
                    let (mentioner, data) = i
                    printfn "%s mentioned you in [%s]" mentioner data
                mailbox.Self <! home
            | "HashTagSearchResponse" as h ->
                if x.tagTweets.Length = 0 then
                    printfn "No such hash tag"
                else
                    printfn "Your search results:"
                    for i in x.tagTweets do
                        let (id, data) = i
                        printfn "<%d> %s" id data
                mailbox.Self <! home
            | "RetweetsResponse" as r ->
                if x.retweets.Length = 0 then
                    printfn "You dont have any retweets..."
                else
                    printfn "Your retweets:"
                    for i in x.retweets do
                        let (id, data) = i
                        printfn "<%d> %s" id data
                mailbox.Self <! home
            | "Stop" as s ->
                mailbox.Context.System.Terminate() |> ignore
            | "Home" as h ->
                printfn "%s" homepage 
                let userInput = getRandomNum(1, 9)
                match userInput with
                | 1 -> 
                    printfn "Tweet by typing, mentioning your friends with an @ ..."
                    let mutable userTweet = getRandomElement(randTweets)
                    if shouldMention() then
                        userTweet <- "@"+getRandomUser()+" "+ userTweet 
                    let dto: Dto = {
                            message = "Tweet"
                            username = userName
                            password = ""
                            response = ""
                            followers = []
                            tweetId = -1
                            tweet = userTweet
                            tweets = [(-1,"")]
                            mentions = [("","")]
                            retweets = [(-1,"")]
                            tagTweets = [(-1,"")]
                            logoutMessage = ""
                            followee = ""
                            follower = ""
                            tag = ""
                    }
                    server.Tell(Json.serialize dto, mailbox.Self)
                    mailbox.Self <! Json.serialize home
                | 2 ->
                    printfn "Like that tweet? Retweet with the tweet id ..."
                    let input = getRandomTweetId()
                    if input <> -1 then
                        let dto: Dto = {
                            message = "ReTweet"
                            username = userName
                            password = ""
                            response = ""
                            followers = []
                            tweetId = input
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
                        server.Tell(Json.serialize dto, mailbox.Self)
                    else
                        mailbox.Self <! Json.serialize home
                | 3 -> 
                    let followers: Followers = {
                        username = userName
                    }
                    server.Tell(followers, mailbox.Self)
                | 4 -> 
                    let followee = getRandomUser()
                    if followee <> userName && followee <> "" then
                        try
                            let followReq: FollowRequest = {
                                followee = followee
                                follower = userName
                            }
                            server.Tell(followReq, mailbox.Self)
                        with ex ->
                                printfn "What did you say? %A" ex
                    else
                        mailbox.Self <! home
                | 5 -> 
                    let feed : Feed = {
                        username = userName
                    }
                    server.Tell(feed, mailbox.Self)
                | 6 ->
                    printfn "Enter a search term preceeded by # ..."
                    let userInput = getRandomElement(hashTags)
                    let searchTag: SearchTag = {
                        tag = userInput
                    }
                    server.Tell(searchTag, mailbox.Self)
                | 7 ->
                    let myRetweets: ShowRetweets = {
                        username = userName
                    }
                    server.Tell(myRetweets, mailbox.Self)
                | 8 -> 
                    let logout : Logout = {
                        username = userName
                        message = "Logout"
                    }
                    server.Tell(logout, mailbox.Self)
                | _ -> printfn "What was that? ..."
            | "LogoutResponse" as l ->
                printfn "%s" x.message
                printfn "%s" logoutpage
                let userInput = getRandomNum(4, 9)
                match userInput with
                | 9 ->
                    let login : Login = {
                        username = userName
                        message = "Login"
                    }
                    server.Tell(login, mailbox.Self)
                | _ -> 
                    let retryLogin: LogoutResponse = {
                        message = "You've successfully logged out!"
                    }
                    mailbox.Self <! retryLogin
            | _ -> printfn "Invalid response(Client)"
            return! loop()
        }
    loop()

let stopWatch = Stopwatch()
for i in 1..numNodes+1 do
    clientId <- "user-" + string(i)
    let client = spawn remoteSys clientId (User(clientId))
    actorList.Add(client)
    let registerDto: Dto = {
        message = "Register"
        username = ""
        password = ""
        response = ""
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
    client <! Json.serialize  registerDto

let stop: Stop = {
    message = "STOP"
}

while true do
    let elapsed = stopWatch.ElapsedMilliseconds
    if elapsed >= (3000000 |> int64) then
        actorList.[0] <! stop

// remoteSys.WhenTerminated.Wait()