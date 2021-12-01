namespace Messages

type Dto = {
    message: string
    username: string
    password: string
    response: string
    followers: list<string>
    tweetId: int
    tweet: string
    tweets: list<int * string>
    mentions: list<string * string>
    retweets: list<int * string>
    tagTweets: list<int * string>
    logoutMessage: string
    followee: string
    follower: string
    tag: string
}
