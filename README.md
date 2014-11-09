FreddiChat
==========
FreddiChat is a chat client and server solution written in C# using WCF and WPF. It uses NetNamedPipeBinding for connections. In its looks and interaction patterns it is rather similar to mIRC or other channel based clients. FreddiChat however only provides one channel per server.

Implementation
--------------
The source consists of two projects.

+ The client side implementation [FreddiChatClient](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatClient "FreddiChatClient on github")
+ The server side implementation [FreddiChatServer](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatServer "FreddiChatServer on github")

### FreddiChatClient ###
[FreddiChatClient](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatClient "FreddiChatClient on github") is a WPF application, which provides the user with an interface to connect to servers and chat with other users.

### FreddiChatServer ###
[FreddiChatServer](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatServer "FreddiChatServer on github") is a console application. It currently doesn't provide any possibility for interaction and configuration of the server, it just displays relevant calls to and operations of the server.

Getting started
---------------
1 Open FreddiChat.sln in Visual Studio
2 Right-click the FreddiChatServer project and select Build to build the server
3 Browse to FreddiChat\ChatServer\bin\Debug using Windows Explorer and double-click FreddieChatServer.exe to launch the server
4 Right-click the FreddiChatClient project and select Add Service Reference
5 Use address net.pipe://localhost/FreddiChat/mex
6 Use ChatServiceReference as name for the service reference
7 Press OK to create the service reference.
8 Right-click the FreddiChatClient project and select Build to build the client
9 Browse to FreddiChat\ChatClient\bin\Debug using Windows Explorer and double-click FreddieChatClient.exe to launch a client

Author
------
FreddiChat is written by [FreddieBoi](https://github.com/FreddieBoi "FreddieBoi on github") in C# using WCF (client, server) and WPF (client).
