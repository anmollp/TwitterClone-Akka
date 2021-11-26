namespace Messages

type Register = {
    message: string
}

type Home = {
    message: string
}

type RegisterRequest = {
    username: string
    password: string
}

type RegisterationResponse = {
    response: string
}

type Tweet = {
    username: string
    text: string
}

type ReTweet = {
    username: string
    tweetId: int
}

type FollowRequest = {
    followee: string
    follower: string
}

type FollowResponse = {
    response: string
}

type Followers = {
    username: string
}

type MyFollowers = {
    followers: list<string>
}

type Feed = {
    username: string
}

type MyFeed = {
    tweets: list<int * string>
    mentions: list<string * string>
}

type Logout = {
    username: string
    message: string
}

type LogoutResponse = {
    message: string
}

type Login = {
    username: string
    message: string
}

type LoginResponse = {
    message: string
}

type NewTweet = {
    username: string
    messageId: int
    message: string
}

type SearchTag = {
    tag: string
}

type HashTagSearchResponse = {
    tagTweets: list<int * string>
}

type ShowRetweets = {
    username: string
}

type RetweetsResponse = {
    retweets: list<int * string>
}

type NewReTweet = {
    username: string
    messageId: int
    message: string
}

type Stop = {
    message: string
}

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
