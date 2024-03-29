# FreddiChat

FreddiChat is a simple chat client and server solution written in C# using WCF (client, server) and WPF (client). It uses NetNamedPipeBinding, NetTcpBinding or WSDualHttpBinding for connections. It's minimalistic in its looks and its interaction patterns are rather similar to mIRC or other channel based clients. FreddiChat however only provides one channel per server.

## Features

- **Connect** to any server using desired name (type `/connect user host`)
- **Broadcast** messages to all users
- **Whisper** message to specific user (double-click desired name or type `/w user`)
- **Reply** to last received whisper (type `/r `)
- **Links** are automatically marked up and clickable
- **History** of previous commands or messages (use up and down arrow keys)
- **Clear** chat history (type `/clear`)
- **Disconnect** from server (type `/disconnect`)
- **Quit** at any time (type `/quit`)

## Implementation

The source consists of two projects.

- [FreddiChatClient](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatClient "FreddiChatClient on github") is a WPF application, which provides the user with an interface to connect to servers and chat with other users.
- [FreddiChatServer](https://github.com/FreddieBoi/FreddiChat/tree/master/ChatServer "FreddiChatServer on github") is a console application, which provides a chat service. It displays relevant calls to and operations of the server, providing very limited interaction and configuration.

### Getting started

Follow the steps below to build the server and the client.

1. Open `FreddiChat.sln` in `Visual Studio`
2. Select `Build Solution` to build the server and the client
3. Browse to `FreddiChat\ChatServer\bin\Debug` and execute `FreddieChatServer.exe` to start the server
   - Select `net.pipe` as protocol when prompted
   - Select `localhost` as hostname when prompted
4. Browse to `FreddiChat\ChatClient\bin\Debug` and execute `FreddieChatClient.exe` to start a client
   - Type `/connect Anonymous net.pipe://localhost` to connect

### Modes

- **Named Pipe** using the `NetNamedPipeBinding`, e.g. `net.pipe://localhost/FreddiChat/`
- **TCP** using the `NetTcpBinding`, e.g. `net.tcp://localhost/FreddiChat/`
- **HTTP** using the `WSDualHttpBinding` (a port must be specified), e.g. `http://localhost:8080/FreddiChat/`

### Security

_Warning: No security is applied for connections._

Security mode `None` is used. Anyone can access the service and messages are not encrypted.

### Code generation

The service client is generated using the ServiceModel Metadata Utility Tool (`svcutil.exe`). It must be regenerated when changing the service contracts.

Follow the steps below to regenerate the service client.

1. Open `FreddiChat.sln` in `Visual Studio`
2. Right-click the `FreddiChatServer` project and select `Build` to build the server
3. Browse to `FreddiChat\ChatServer\bin\Debug` and execute `FreddieChatServer.exe` to start the server
   - Select `net.pipe` as protocol when prompted
   - Select `localhost` as hostname when prompted
4. Start `Developer Command Prompt` for `Visual Studio`
5. Execute `svcutil.exe /language:cs /noConfig /namespace:*,FreddiChatClient.Communications /out:FreddiChat\ChatClient\Communications\GeneratedChatServiceClient.cs net.pipe://localhost/FreddiChat/mex` to regenerate the service client

### Debugging

To prevent the `Visual Studio` debugger from breaking on `LoadFromContext occurred` and `NotMarshalable occurred`, disable the `Enable UI Debugging Tools for XAML` option in `Tools > Options > Debugging > General`.

## Build status

![Build status](https://github.com/FreddieBoi/FreddiChat/actions/workflows/main.yml/badge.svg)

## License

FreddiChat is written by [FreddieBoi](https://github.com/FreddieBoi "FreddieBoi on github"). See the [LICENSE](https://github.com/FreddieBoi/FreddiChat/blob/master/LICENSE) file for license rights and limitations (BEER-WARE).
