FreddiChat
==========
FreddiChat is a simple chat client and server solution written in C# using WCF (client, server) and WPF (client). It uses NetNamedPipeBinding, NetTcpBinding or WSDualHttpBinding for connections. In its looks and interaction patterns it is rather similar to mIRC or other channel based clients. FreddiChat however only provides one channel per server.

Implementation
--------------
The source consists of two projects.

+ [FreddiChatClient](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatClient "FreddiChatClient on github") is a WPF application, which provides the user with an interface to connect to servers and chat with other users.
+ [FreddiChatServer](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatServer "FreddiChatServer on github") is a console application, which provides a chat service. It displays relevant calls to and operations of the server, providing very limited interaction and configuration.

Features
--------
+ **Connect** to any server using desired name
+ **Broadcast** messages to all users
+ **Whisper** message to specific user (double-click desired name or type `/w user`)
+ **Reply** to last received whisper (type `/r `)
+ **Links** are automatically marked up and clickable
+ **History** of previous commands or messages (use up and down arrow keys)
+ **Clear** chat history (type `/clear`)
+ **Disconnect** from server (type `/disconnect`)
+ **Quit** at any time (type `/quit`)

Modes
-----
+ **Named Pipe** using the `NetNamedPipeBinding`, e.g. `net.pipe://localhost/FreddiChat/`
+ **TCP** using the `NetTcpBinding`, e.g. `net.tcp://localhost/FreddiChat/`
+ **HTTP** using the `WSDualHttpBinding` (a port must be specified), e.g. `http://localhost:8080/FreddiChat/`

Security
--------
*Warning: No security is applied for connections.*

Security mode `None` is used. Anyone can access the service and messages are not encrypted.

Getting started
---------------
Follow the steps below to build the server and the client.

1. Open `FreddiChat.sln` in `Visual Studio`
2. Right-click the `FreddiChatServer` project and select `Build` to build the server
3. Browse to `FreddiChat\ChatServer\bin\Debug` and execute `FreddieChatServer.exe` to start the server
    + Select `net.pipe` as protocol when prompted
4. Right-click the `FreddiChatClient` project and select `Add Service Reference`
    + Use `net.pipe://localhost/FreddiChat/mex` as address for the service reference
    + Use `ChatServiceReference` as name for the service reference
    + Press `OK` to create the service reference
8. Right-click the `FreddiChatClient` project and select `Build` to build the client
9. Browse to `FreddiChat\ChatClient\bin\Debug` and execute `FreddieChatClient.exe` to start a client

License
-------
FreddiChat is written by [FreddieBoi](https://github.com/FreddieBoi "FreddieBoi on github"). See the [LICENSE](https://github.com/FreddieBoi/FreddiChat/blob/master/LICENSE) file for license rights and limitations (BEER-WARE).
