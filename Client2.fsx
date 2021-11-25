#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: FSharp.Json"

#load "./Messages.fsx"

open Akka.Configuration
open Akka.FSharp
open Messages

let configuration =
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote.helios.tcp {
                hostname = localhost
                port = 9001
            }
        }"
    )

let mutable ipAddress = ""
let mutable clientId = ""
let mutable userName = ""

match fsi.CommandLineArgs with 
    | [|_; ip; id|] -> 
        ipAddress <- ip
        clientId <- id
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

let home : Home = {
    message = "home"
}

let User(mailbox: Actor<obj>) msg =
    match box msg with
    | :? Register as r ->
        printfn "Register with an username and password to continue on Chirp! ..."
        printf "Username: "
        let userInput = System.Console.ReadLine()
        userName <- userInput.Trim()
        printf "Password: "
        let userpassword = System.Console.ReadLine().Trim()
        let request: RegisterRequest = {
            username = userName
            password = userpassword
        }
        server.Tell(request, mailbox.Self)
    | :? RegisterationResponse as r ->
         printfn "%s" r.response
         mailbox.Self <! home
    | :? LoginResponse as l ->
        printfn "%s" l.message
        mailbox.Self <! home
    | :? FollowResponse as f ->
        printfn "%s" f.response
        mailbox.Self <! home
    | :? MyFollowers as m ->
        if m.followers.Length = 0 then
            printfn "You dont have any followers..."
        else
            printfn "Your followers:"
            for i in m.followers do
                printfn "%s" i
        mailbox.Self <! home
    | :? NewTweet as n ->
        printfn "Here's a new tweet from @%s" n.username
        printfn "<%d> %s" n.messageId n.message
        mailbox.Self <! home
    | :? NewReTweet as r ->
        printfn "Here's a new retweet from @%s" r.username
        printfn "<%d> %s" r.messageId r.message
        mailbox.Self <! home
    | :? Feed as f -> 
        let feed : Feed = {
            username = userName
        }
        server.Tell(feed, mailbox.Self)
    | :? MyFeed as m ->
        printfn "Your recent tweets"
        for i in m.tweets do
            let (id, data) = i
            printfn "<%d> %s" id data
        printfn "Your recent mentions"
        for i in m.mentions do
            let (mentioner, data) = i
            printfn "%s mentioned you in [%s]" mentioner data
        mailbox.Self <! home
    | :? HashTagSearchResponse as h ->
        if h.tagTweets.Length = 0 then
            printfn "No such hash tag"
        else
            printfn "Your search results:"
            for i in h.tagTweets do
                let (id, data) = i
                printfn "<%d> %s" id data
        mailbox.Self <! home
    | :? RetweetsResponse as r ->
        if r.retweets.Length = 0 then
            printfn "You dont have any retweets..."
        else
            printfn "Your retweets:"
            for i in r.retweets do
                let (id, data) = i
                printfn "<%d> %s" id data
        mailbox.Self <! home
    | :? Home as h ->
        printfn "%s" homepage 
        let userInput = System.Console.ReadLine()
        let input = userInput.Trim() |> int
        match input with
        | 1 -> 
            printfn "Tweet by typing, mentioning your friends with an @ ..."
            let userTweet = System.Console.ReadLine()
            let tweet: Tweet = {
                username = userName
                text = userTweet
            }
            server.Tell(tweet, mailbox.Self)
            mailbox.Self <! home
        | 2 ->
            printfn "Like that tweet? Retweet with the tweet id ..."
            let userInput = System.Console.ReadLine()
            let mutable input = -1
            try 
                input <- int(userInput.Trim())
                let retweet: ReTweet = {
                    username = userName
                    tweetId = input
                }
                server.Tell(retweet, mailbox.Self)
            with ex ->
                    printfn "What did you say? %A" ex
        | 3 -> 
            let followers: Followers = {
                username = userName
            }
            server.Tell(followers, mailbox.Self)
        | 4 -> 
            let userInput = System.Console.ReadLine()
            let mutable followee = ""
            try 
                followee <- userInput.Trim()
                let followReq: FollowRequest = {
                    followee = followee
                    follower = userName
                }
                server.Tell(followReq, mailbox.Self)
            with ex ->
                    printfn "What did you say? %A" ex
        | 5 -> 
            let feed : Feed = {
                username = userName
            }
            server.Tell(feed, mailbox.Self)
        | 6 ->
            printfn "Enter a search term preceeded by # ..."
            let userInput = System.Console.ReadLine().Trim()
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
    | :? LogoutResponse as l ->
        printfn "%s" l.message
        printfn "%s" logoutpage
        let userInput = int(System.Console.ReadLine().Trim())
        match userInput with
        | 9 ->
            let login : Login = {
                username = userName
                message = "Login"
            }
            server.Tell(login, mailbox.Self)
        | _ -> printfn "What was that? ..."
    | _ -> printfn "Invalid response(Client)"

let client = spawn remoteSys clientId (actorOf2(User))
printfn "%A" client.Path

let register: Register = {
    message = "Register"
}
client <! register
remoteSys.WhenTerminated.Wait()