# TwitterClone-Akka

A twitter simulator using akka actor model in F#

## COP5615–Distributed Operating System Principles– Project 4 Part 1

**Project members:**

`Anmol Lingan Gouda Patil (UFID: 19673150)`

`Shashanka Bhat (UFID: 34524261)`

  

**Basic Requirement:** Finished

**Bonus:** Finished

  
### How to run:

>Before running the scripts, the ip address of the machine needs to be edited in configuration of the server file(Server.fsx).

>The same ip needs to be used afterwards for further process.

1. Sever/Twitter Engine start:
To start the twitter engine , open a terminal and type the following command:\
    **dotnet fsi Server.fsx**

2. Client start:
To start the users/clients and a simulation of these users in the twitter world typw the following command in another terminal:\
**dotnet fsi Client.fsx `<ipAddress-of-Server>`  `<number-of-users>`  `<simulation-time-in-seconds>`**


##### * Note the term subscribe and follow are used interchangeably in the report.

### What is Working?

* Clients/users register with the Twitter Engine to start their activity in the system.

* Users can tweet, add hashtags to tweet, mention other users and also retweet some tweets they get in their feed.

* Users can follow other users to recieve any tweets the followee tweets.

* Users can query tweets that contain a specific hashtag, can check his/her mentions, can check his feed(contains recent tweets and mentions), can see his followers.

* To simulate liveness a user can choose to logout and login at any time during the simulation. By logging out the user doesn't recieve any kind of messages and all his messages are stored in the actor inbox. When he is live any live tweet, retweet of his followee and any mentions of his/her(s) is delivered to them in real time just like a push notification.
* The statistics are calculated and shown in the server terminal.
* Simulation can be done in both Zipf and Randomized distribution mode.

* For Zipf

    ![alt](/img/zipf-def.png)

* For Randomized distribution there is no such constraint on the followers count for a user.

* Following are the main menu option provided to any user when they log in:
    ![alt](/img/action-3.png)

* Logout page options:
    
    ![alt](/img/logout.png)


### Some inferences:

* We found that when the users followed a Zipf Distribution with respect to the number of followers each user has, the activity metrics of all the users also followed a near Zipf Distribution.
* Here activity metrics for a user is defined as the number of times the users's tweets and retweets have been retweeted by all of his followers.

* Mathematically,
    ![alt](/img/activity-metrics.png)
* Here are some results for various activity-metrics vs users for Zipf distribution.
    ![alt](/img/10usermetric.PNG)
    ![alt](/img/50usersmetric.PNG)
    ![alt](/img/100usermetric.PNG)

We also found out that  when there was no distribution enforced on the number of followers the activity was roughly a Gaussian distribution.

* Here are some results for various activity-metrics vs users for random distribution.
    ![alt](/img/50userrandom.PNG)
    ![alt](/img/100userrandom.PNG)
 
### Performance metrics:
* Total number of messages during the simulation period is defined as follows.
    ![alt](/img/tot_msgs.png)

* Here are some results for the same
    ![alt](/img/message_comparison.png)

* The system is able to handle users at scale.
* As the number of users increase the total messages in the system increase too.

## System in action:
* ![alt](/img/action-1.png)

* ![alt](/img/action-2.png)

* ![alt](/img/action-4.png)

* ![alt](/img/action-5.png)

* ![alt](/img/action-6.png)
