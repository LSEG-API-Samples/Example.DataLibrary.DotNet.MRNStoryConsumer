
## Overview 

**Update**: October 2021

This example demonstrates how to use the delivery layer from the RDP.NET library to retrieve MRN_STORY data and provide an implementation to concatenate the MRN fragments from the update message. And then decompressing the concatenated data to JSON plain-text and print the MRN Story data to console output.

The example app utilizes Core Delivery from the library to create a WebSocket session to connect to the WebSocket server on Refinitiv Real-Time Distribution System (RTDS) and Refinitiv Real-Time -- Optimized (RTO - formerly known as ERT in the Cloud). 

To build run the example user has to install .NET Core 3 SDK and the user must modify the credential in the following section in Program.cs before building the app.

* Modify the following section for testing the application with RTDS 3.2.x or higher version. 
```
#region RTDSCredential
    private const string RTDSUser = "<DACS User>";
    private const string appID = "<App ID>";
    private const string position = "<Your IP or Host Name>/net";
    private const string WebSocketHost = "<WebSocket Server>:<Websocket Port>";
#endregion
```

* Modify the following section for testing the application with RDP or RTO.

```
#region RDPUserCredential
   private const string RDPUser = "<Machine ID>/<Your Email>";
   private const string RDPPassword = "<RDP Password>";
   private const string RDPAppKey = "<RDP AppKey>";
#endregion
```

## Build and Run the app

In command-line mode, go to the project folder which contains .csproj and type the following command:
```
dotnet run
```

Or if you want to build the project to executable file you can type the following command:

```
dotnet build -c release -r win10-x64 -o ./release-x64
```
It will create an executable file in a release-x64 folder. Then you can run MRNStoryConsumer_Method1.exe from the release folder.

The above command is for windows 10 64 bit. If you want to build it on Linux or macOS, you can change it to one of the lists from [.NET Core RID Catalog](https://docs.microsoft.com/th-th/dotnet/core/rid-catalog). 

For example, 

On the macOS, you have to change it to "osx-x64".
```
dotnet build -c release -r osx-x64 -o ./release-x64
```
It should generate executable file MRNStoryConsumer_Method1 under the release-x64 folder.

You can use the same command to build any project.

Anyway, if you have Visual Studio Code and installed .NET Core Development plugin, you can open the folder and click Start Debug or Start without Debug from the editor.