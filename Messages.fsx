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