FreddiChat
==========
FreddiChat is a chat client and server solution written in C# using WCF and WPF. It uses NetNamedPipeBinding for connections. In its looks and interaction patterns it is rather similar to mIRC or other channel based clients. FreddiChat however only provides one channel per server.

Implementation
--------------
The source consists of two projects:
+ FreddiChatClient
+ FreddiChatServer

### FreddiChatClient ###
[FreddiChatClient](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatClient "FreddiChatClient on github") is a WPF application, which provides the user with an interface to connect to servers and chat with other users.

### FreddiChatServer ###
[FreddiChatServer](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatServer "FreddiChatServer on github") is a console application. It currently doesn't provide any possibility for interaction and configuration of the server, it just displays relevant calls to and operations of the server.

Author
------
FreddiChat is written by [FreddieBoi](https://github.com/FreddieBoi "FreddieBoi on github") in C# using WCF (client, server) and WPF (client).
