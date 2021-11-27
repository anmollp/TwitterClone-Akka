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

let stopDto: Dto = {
            message = "STOP"
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
                mailbox.Self <! Json.serialize home
            | "FollowResponse" as f ->
                printfn "%s" x.response
                mailbox.Self <! Json.serialize home
            | "MyFollowers" as m ->
                if x.followers.Length = 0 then
                    printfn "You dont have any followers..."
                else
                    printfn "Your followers:"
                    for i in x.followers do
                        printfn "%s" i
                mailbox.Self <! Json.serialize home
            | "NewTweet" as n ->
                printfn "Here's a new tweet from @%s" x.username
                printfn "<%d> %s" x.tweetId x.tweet
                mailbox.Self <! Json.serialize home
            | "NewReTweet" as r ->
                printfn "Here's a new retweet from @%s" x.username
                printfn "<%d> %s" x.tweetId x.tweet
                mailbox.Self <! Json.serialize home
            | "MyFeed" as m ->
                printfn "Your recent tweets"
                for i in x.tweets do
                    let (id, data) = i
                    printfn "<%d> %s" id data
                printfn "Your recent mentions"
                for i in x.mentions do
                    let (mentioner, data) = i
                    printfn "%s mentioned you in [%s]" mentioner data
                mailbox.Self <! Json.serialize home
            | "HashTagSearchResponse" as h ->
                if x.tagTweets.Length = 0 then
                    printfn "No such hash tag"
                else
                    printfn "Your search results:"
                    for i in x.tagTweets do
                        let (id, data) = i
                        printfn "<%d> %s" id data
                mailbox.Self <! Json.serialize home
            | "RetweetsResponse" as r ->
                if x.retweets.Length = 0 then
                    printfn "You dont have any retweets..."
                else
                    printfn "Your retweets:"
                    for i in x.retweets do
                        let (id, data) = i
                        printfn "<%d> %s" id data
                mailbox.Self <! Json.serialize home
            | "STOP" as s ->
                mailbox.Self <! PoisonPill.Instance
            | "Home" as h ->
                printfn "%s" homepage
                if numMessages <= 0 then
                    printfn "*********Sending STOP*************"
                    mailbox.Self <! Json.serialize stopDto
                else
                    let userInput = getRandomNum(1, 8)
                    printfn "%s Pressed ******************* [%d] ********************" userName userInput 
                    match userInput with
                    | 1 -> 
                        printfn "Tweet by typing, mentioning your friends with an @ ..."
                        numMessages <- numMessages - 1
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
                        let dto: Dto = {
                                message = "Followers"
                                username = userName
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
                        server.Tell(Json.serialize dto, mailbox.Self)
                    | 4 -> 
                        let followee = getRandomUser()
                        if followee <> userName && followee <> "" then
                            let dto: Dto = {
                                message = "FollowRequest"
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
                                followee = followee
                                follower = userName
                                tag = ""
                            }
                            server.Tell(Json.serialize dto, mailbox.Self)
                        else
                            mailbox.Self <! Json.serialize home
                    | 5 -> 
                        let dto: Dto = {
                                message = "Feed"
                                username = userName
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
                        server.Tell(Json.serialize dto, mailbox.Self)
                    | 6 ->
                        printfn "Enter a search term preceeded by # ..."
                        let userInput = getRandomElement(hashTags)
                        let dto: Dto = {
                                message = "SearchTag"
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
                                tag = userInput
                            }
                        server.Tell(Json.serialize dto, mailbox.Self)
                    | 7 ->
                        let dto: Dto = {
                                message = "ShowRetweets"
                                username = userName
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
                        server.Tell(Json.serialize dto, mailbox.Self)
                    | 8 -> 
                        let dto: Dto = {
                                message = "Logout"
                                username = userName
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
                        server.Tell(Json.serialize dto, mailbox.Self)
                    | _ -> printfn "What was that? ..."
            | "LogoutResponse" as l ->
                printfn "%s" x.response
                printfn "%s" logoutpage
                let userInput = getRandomNum(4, 9)
                match userInput with
                | 9 ->
                    let dto: Dto = {
                            message = "Login"
                            username = userName
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
                    server.Tell(Json.serialize dto, mailbox.Self)
                | _ -> 
                    let dto: Dto = {
                            message = "LogoutResponse"
                            username = userName
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
                    mailbox.Self.Tell(Json.serialize dto, mailbox.Self)
            | _ -> printfn "Invalid response(Client)"
            return! loop()
        }
    loop()

let stopWatch = Stopwatch()
for i in 1..numNodes do
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

remoteSys.WhenTerminated.Wait()