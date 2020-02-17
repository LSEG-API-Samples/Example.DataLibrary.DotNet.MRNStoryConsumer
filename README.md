# Create MRN Real-Time News Story Consumer app using .NET Core and RDP.NET Library

## Introduction

It's been a while we receive a question from the .NET developer about easy to use solutions that can help them save the time to develop the application for retrieving market data from Elektron via both the TREP and Elektron Real-Time(ERT) in Cloud.

Typically, ERT in Cloud and TREP version 3.2.x and later version provide a Websocket connection for a developer so that they can use any WebSocket client library to establish a connection and communicate with the WebSocket server directly. Anyway, to retrieve data from ERT in Cloud, it requires additional steps that the connecting user must authenticate themselves before a session establishing with the server. Moreover, the application also needs to design the application's workflow to control the JSON request and response message along with maintaining the ping and pong, which is a heartbeat message between the client app and the server-side. As a result, the workflow to create the application is quite a complicated process. 

[The Refinitiv Data Platform Libraries for .NET (RDP.NET)](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries) is a new community-based library which is designed as ease-of-use interfaces and it can help eliminate the development process complexity. The below picture depicts the architecture of RDP Libraries. The below picture depicts the architecture of RDP Libraries.

![RDPArchitecture](https://developers.refinitiv.com/sites/default/files/inline/images/RDP.png)

And the following picture is an architecture and layers from the Libraries.

![RDPlayer](https://developers.refinitiv.com/sites/default/files/inline/images/AbstractLayer3.png)

The RDP.NET are ease-of-use APIs defining a set of uniform interfaces providing the developer access to the Refinitiv Data Platform and Elektron. The APIs are designed to provide consistent access through multiple access channels; developers can choose to access content from the desktop, through their deployed streaming services or directly to the cloud.  The interfaces encompass a set of unified Web APIs providing access to both streaming (over WebSockets) and non-streaming (HTTP REST) data available within the platform.

This article will provide a sample usage to create a .NET Core console app to retrieve MRN News Story from TREP or ERT in Cloud using [RDP.NET library](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries).  It will describe how to use an interfaces from Core/Delivery Layer to request a Streaming data especially a real-time News from MRN Story data. This article also provides an example application that implements a function to manage a JSON request and response message. It will show you how to manually concatenate and decompress MRN data fragments. Moreover, the article will show you alternate options from a Content Layer to retrieve the same MRN Story data.



## Prerequisites

- .NET Core SDK 3.1 or later versions.
- [Visual Studio Code](https://code.visualstudio.com/)/Text editor or Visual Studio 2017/2019. 
- Understand concepts and basic usage of Refinitiv Data Platform (RDP) Libraries for .NET. You can read [the Introduction to RDP Libraries document](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries/docs?content=62446&type=documentation_item) from developer portal.

## Using .NET Core with RDP.NET Library

At the time we write this article, developer can get the library from nuget.org. There are two main library you can add to your project.

* [Refinitiv DataPlatform for .NET](https://www.nuget.org/packages/Refinitiv.DataPlatform/) is a Core/Delivery Layer.
* [Refinitiv DataPlatform Content](https://www.nuget.org/packages/Refinitiv.DataPlatform.Content/) is a library for Content Layer.

We will use the RDP.NET library with .NET Core SDK so that you can test the codes on a various platform which support .NET Core SDK such as Linux and macOS. You can write the codes in Visual Studio Codes and use the command line or terminal to run the sample app.

### Add RDP.NET to .NET Core Console App

Let start by creating .NET Core console app. Run command prompt or terminal and create a folder such as rdpmrnconsumer. Then Navigate to the folder you created and type the following:

```
dotnet new console
dotnet run
```

dotnet new creates an up-to-date rdpmrnconsumer.csproj project file with the dependencies necessary to build a console app. It also creates a Program.cs, a basic file containing the entry point for the application. It will shows messge "Hello World" when you type **dotnet run** command.

Next step type following command to add Refinitiv.DataPlatfrom and its dependencies from [Nuget](https://www.nuget.org/packages/Refinitiv.DataPlatform/) to the project. Current version at the time we write this article is a pre-release version 1.0.0-alpha

```
dotnet add package Refinitiv.DataPlatform --version 1.0.0-alpha
```

Then add following mudules to Program.cs.

```csharp
using Refinitiv.DataPlatform;
using Refinitiv.DataPlatform.Core;
```

### Create Session and use Stream API to retrieve real-time content

According to the [RDP.NET online document](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries/docs?content=62446&type=documentation_item), in order to access the platform via RDP/ERT and Elektron, access requires authentication and connection semantics in order to communicate and retrieve content. the RDP Libraries provide this managed access through an interface called a Session. Session is the interface to a specific access channel and is responsible for defining authentication details, managing connection resources, and implementing the necessary protocol to manage the life cycle of the Session. There are two type of Session that we need to create in this application:

* PlatformSession (RDP, ERT in Cloud)
* DeployedPlatformSession (Elektron,TREP/ADS)

The Session correspond to each of the access channels available and provides unique properties to support the specific requirements for that interface. Once the Session has been established, developers can use a Stream API which is the streaming interface delivering real-time content over WebSockets to request and process the response. As part of the Delivery layer,a Stream provides a simple, yet flexible design to access streaming content delivered using Event-driven callbacks defined within your application.

The Stream allows you to subscribe to items of the different supporting models (MarketPrice, MarketByPrice, Machine Readable News (MRN), etc) available via a streaming connection to the platform. With the current design, a Stream is meant to be used with the Event Driven operation mode.

#### Create Session for deployed TREP/ADS

Open Program.cs and add the following codes which defined a credentials required by TREP/ADS websocket connection to Program class.

```csharp
#region TREPCredential
    private const string TREPUser = "<DACS User>";
    private const string appID = "<App ID>";
    private const string position = "<Your IP /Host Name>/net";
    private const string WebSocketHost = "<WebSocket Server>:<Websocket Port>";
#endregion
```

Add the following codes to the Main method to create a Session for DeployedPlatform and pass parameters under region TREPCredential to create session. In this example, we just print the state and message returned by the callback to console output.

```csharp
ISession session=null;
session = CoreFactory.CreateSession(new DeployedPlatformSession.Params()
                    .Host(WebSocketHost)
                    .WithDacsUserName(TREPUser)
                    .WithDacsApplicationID(appID)
                    .WithDacsPosition(position)
                    .OnState((s, state, msg) =>
                    {
                        Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                        _sessionState = state;
                    })
                    .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})")));
```
Note that we will add ___sessionState__ variable to the class to sync the session state after we call Open or OpenAsync. We will check the session state before sending Item Request. Please add the following codes to Prgram class.

```csharp
private static Session.State _sessionState = Session.State.Closed;
```
Then you need to pass a callback function to OnState and OnEvent to monitor the Session state and receiving events from RDP internal library. The msg variable in the OnEvent message is a JObject which is JSON message containing the content for a specific event. It could be an error from the backend, and below is a sample JSON message when the DACS user is not valid.

```json
{
  "Contents": "api, unknown to system."
}
```

#### Create Session for Refinitiv Data Platfrom (RDP) or ERT in Cloud

Add the following codes which defined a credentials required by RDP websocket connection to Program class. 

```
#region RDPUserCredential
   private const string RDPUser = "<Machine ID>/<Your Email>";
   private const string RDPPassword = "<RDP Password>";
   private const string RDPAppKey = "<RDP AppKey>";
#endregion
```

Then add the following codes in the Main method to create PlatformSession. The Library's internal mechanism will handle the request token and maintain the life cycle of the token on behalf of the application. Application does not need to add any codes to handle access token.

```csharp
 ISession session=null;
 session = CoreFactory.CreateSession(new PlatformSession.Params()
                    .OAuthGrantType(new GrantPassword().UserName(RDPUser)
                        .Password(RDPPassword))
                    .AppKey(RDPAppKey)
                    .WithTakeSignonControl(true)
                    .OnState((s, state, msg) =>
                    {
                        Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                        _sessionState = state;
                    })
                    .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})")));
```
After we created the session, then we need to call the __session.Open()__ Or __session.OpenAsync()__ to open the session. The Websocket library (which is [websocketsharp.core](https://www.nuget.org/packages/websocketsharp.core/)) used by RDP.NET will establish a connection to the server and manage to send authentication requests and process the response and maintain the connectivity between the client app and server.

As we said earlier, when it has an error about the credential, the RDP.NET library returns the error message with the code from the RDP gateway. Below is a sample JSON message when the user passes an invalid username or password.

```json
{
  "HTTPStatusCode": 400,
  "HTTPReason": "Bad Request",
  "Contents": {
    "error": "access_denied",
    "error_description": "Invalid username or password."
}
```

## Retrieving MRN Story content

There are two approaches the application can use to retrieve data for market price or another domain model.

- __The first approach__ is to use the Stream API provided by Deliver layer to request data and implement the application's codes to handle the data returned from the callback. Delivery Layer also provide interface for the user to set the item name and domain model so that the application can use the Stream API to request data for any supported domain model. However, the application has to understand the structure of the JSON message for the specific domain it requested. In the example we will request MRN Story data from the NewsAnalystics domain, therefore, we will apply instructions from [MRN Data Model]((https://developers.refinitiv.com/sites/default/files/ThomsonReutersMRNElektronDataModelsv210_2.pdf)) in order to process the data correctly.

- __The second approach__ is to add Nuget package Refinitiv.DataPlatform.Content and then use method ContentFactory.CreateMachineReadableNews to create MachineReadableNews object. MachineReadableNews is the class from Content Layer from the Introduction topics and it was designed to retrieve MRN contents and provide callback OnNews to return MRN JSON Message to the application layer.

We will create a separate project to demonstrate the usage of each approach. 

### Using Stream API to retrieve MRN Story data

Let start with the first approach. The following sample codes demonstrate how to open the item stream request using Stream API which is the class under Delivery layer. The API provide DeliveryFactory.CreateStream to create ItemStream. After the application call stream.open() it will send a new item request to Elektron or ERT in Cloud. Then the application can process the JSON data returns from the callback functions defined in OnRefresh, OnUpdate, and OnStatus. The application can use method __WithFields()__ to specify field lists for a view request, and it can set __WithStreaming(false)__ to send snapshot requests.

To use the class from Delivery layer, you need to use the following modules.

```csharp
using Refinitiv.DataPlatform.Delivery;
using Refinitiv.DataPlatform.Delivery.Stream;
```

Then you can add the following codes to Main method. Application just print the variable msg to console output.

```csharp
 using (IStream stream = DeliveryFactory.CreateStream(
                new ItemStream.Params().Session(session)
                    .Name("<ItemName>")
                    .WithDomain("<Domain Model>")
                    .OnRefresh((s, msg) => Console.WriteLine($"{msg}\n\n"))
                    .OnUpdate((s, msg) => Console.WriteLine(msg))
                    .OnError((s, msg) => Console.WriteLine(msg))
                    .OnStatus((s, msg) => Console.WriteLine($"{msg}\n\n"))))
{
    stream.Open();
}
```
Variable **msg** is a JObject and it contains the JSON message provided by the server. The following sample JSON message is a result when an application requests ItemName "AUD=" with the "MarketPrice" Domain Model. If you did not call method WithDomain it will use "MarketPrice" as default.

```JSON
{
  "ID": 2,
  "Type": "Refresh",
  "Key": {
    "Service": "ELEKTRON_EDGE",
    "Name": "AUD="
  },
  "State": {
    "Stream": "Open",
    "Data": "Ok",
    "Text": "All is well"
  },
  "Qos": {
    "Timeliness": "Realtime",
    "Rate": "TickByTick"
  },
  "PermData": "AwEsUmw=",
  "SeqNumber": 23280,
  "Fields": {
    "PROD_PERM": 526,
    "RDNDISPLAY": 153,
    "DSPLY_NAME": "OTP BANK RT  BUD",
    "TIMACT": "08:53:00",
    "NETCHNG_1": 0.0032,
    "HIGH_1": 0.6732,
    "LOW_1": 0.668,
    "CURRENCY": "USD",
    "ACTIV_DATE": "2020-02-04",
    "OPEN_PRC": 0.6691,
    "HST_CLOSE": 0.6691,
    "BID": 0.6723,
    "BID_1": 0.6723,
    "BID_2": 0.6722,
    "ASK": 0.6724,
    "ASK_1": 0.6724,
    "ASK_2": 0.6725,
    "ACVOL_1": 31157,
    ...
    "ASKHI1_MS": "08:29:57.721",
    "ASKHI2_MS": "08:29:21.89",
    "ASKHI3_MS": "08:28:54.218",
    "ASKHI4_MS": "08:28:36.991",
    "ASKHI5_MS": "08:27:56.986",
    "ASKLO1_MS": "01:10:43.164",
    "ASKLO2_MS": "01:10:33.66",
    "ASKLO3_MS": "01:09:33.74",
    "ASKLO4_MS": "01:03:14.09",
    "ASKLO5_MS": "00:39:09.974",
    "BIDHI1_MS": "08:29:57.381",
    "BIDHI2_MS": "08:29:22.909",
    "BIDHI3_MS": "08:28:49.656",
    "BIDHI4_MS": "08:28:38.286",
    "BIDHI5_MS": "08:28:00.819",
    "BIDLO1_MS": "01:10:54.241",
    "BIDLO2_MS": "01:10:43.426",
    "BIDLO3_MS": "01:09:52.16",
    "BIDLO4_MS": "00:55:01.75",
    "BIDLO5_MS": "00:39:30.107",
    "MIDHI1_MS": "08:29:57.38",
    "MIDLO1_MS": "01:10:43.164",
    "BID_HR_MS": "08:00:00.727"
  }
}
```

Typically the structure of the response message is the same as the structure mentioned in [Elektron Websocket](https://developers.refinitiv.com/elektron/websocket-api/quick-start). Hence it could say that the RDP.NET API responsible for managing the connection, send a request, and process a response message. It also maintains connectivity between the client app and the server on behalf of the application. The application has to create its own function or data model to process the content returned from the callback. The developer needs to know the item name, domain name, and structure of the JSON message returned from the Websocket Server to process data correctly.

In our example app, we need to set the Name param to __"MRN_STORY"__ and set Domain Model to __"NewsTextAnalytics"__. Refer to structure of MRN Story from [MRN DATA MODELS AND ELEKTRON IMPLEMENTATION GUIDE](https://developers.refinitiv.com/sites/default/files/ThomsonReutersMRNElektronDataModelsv210_2.pdf), the refresh and update message provided by MRN feed is a list of key-value pairs of the FID provided under the Fields, and the data inside the update message of the MRN Story is quite different from the Market Price domain. It is because the FRAGMENT FID is compressed with gzip compression technology, thus requiring the consumer to concatenate and decompress the fragment data to reveal the JSON plain-text from that FID. What we need to add to this application is the data caching and the way to verifying and decompressing the MRN Story fragment to the JSON plain-text.

The following JSON message is a sample of MRN update and fragment data the application needs to handle correctly.

```json
{
  "ID": 2,
  "Type": "Update",
  "Domain": "NewsTextAnalytics",
  "UpdateType": "Unspecified",
  "DoNotConflate": true,
  "DoNotRipple": true,
  "DoNotCache": true,
  "Key": {
    "Service": "ELEKTRON_DD",
    "Name": "MRN_STORY"
  },
  "PermData": "AwEBEBU8",
  "SeqNumber": 30910,
  "Fields": {
    "TIMACT_MS": 30796584,
    "ACTIV_DATE": "2020-02-05",
    "MRN_TYPE": "STORY",
    "MRN_V_MAJ": "2",
    "MRN_V_MIN": "10",
    "TOT_SIZE": 816,
    "FRAG_NUM": 1,
    "GUID": "Pt66HqTl__2002052OxPAmtJPzgacG/ircD/ehPTGzrtAlbTUohheh",
    "MRN_SRC": "HK1_PRD_A",
    "FRAGMENT": "H4sIAAAAAAAC/8VUTXPjNgy991dgNHu0LPkj6UY3J07TdBJHjbWHttvZoSTYYkNRWhK04+30vy9grdN0Jp3prSeBEAE8vgfgz0gZuq2jLLI5nZ//+Lkw0ShSodZoK/RR9lu0yrP85jL6fRSVXX3gm+nZD1hO0wTS97MZLO7helkkR/vmvvho4/hkZ5AbReThMtgndC9Hhx7dDj1Qg+D0tiGwHQF10IfSaN+Asgcode2hc9BtNug8VJ31ukaHtVwsEYJ1qHxnVWlwBD5UHOZBgen2EiuhChrOPmTgYoqg7risFHPYCwxL0Cr3hAQ7ZQKO/4a4MVixoW3lsOV7ykDVKLtF8UmBZEhrcIfGD9m3eodctOqCJXS9cnQA1fYGgXSLghufsQrEdW4t9E5VpCsGr9vedbtjmbezsw+fWQ8mZAPvJuM0TVqSF05SqCQqKZVzaKDnmFZbLsHvOghNHtEKMUM8A3x5o1zYKXcQGTwCw1UtknC919R04ajKEV+NPdpa2y109sQX61Fr0qzKmPU9SJiyNZcz5iiXskwisUQkQYpz2fg1maXy2kPJVURsH0qPn4Po8QYZPvnWBG3wJI9qVY2vpTEH2LiuPTbUkTLhet9o7glxlYJP6BrQnYBhzTpsGNu//PeSRX1L+Kr/3uq8U8sNsu21MYKTSTeh5pAXdMp7lkFQs/6dSDKG99lslk3Ox7P52WmE/jE4sGBQFMcfLc/mRjtPVwN8HsZpOk3jdBqnZ0V6yjO/mP3KNxtUtdEW+VZ+tyiKdQYXF9/H8Qr38EvnnmD+vw0oo9OC/rRzPn2apvyQs+nDc75o6af8y1ZVN4l21TLBJi9uvjhamLL40DUNNhJtPSnZUA8b3lG8mwyPZlBbeSwKTS0zy8fi0LNrymcewOEQET5T0hul5Z70mqBm/2qd5XfFWpyhXJOiwPsv4jqUBS+P4D/cp3/IXpDFeJ1N2HOdzdPhM+fPfTb5ecnfR96Zi5TpPtmDuZpml4urYrCu8seH5WDerpYfBuvuejUYD4/LR1m6pJ5wfRyNisFPRlFwW7Z5Ec9G0Y6J5xH8D+3w13dfAbNX2PnrBQAA"
  }
}
```

The FRAGMENT field is a fragment of compressed JSON for the MRN Story. We will add the data structure to the console application to concatenate the data fragment correctly.

#### Convert the MRN update message to Object

To process the data from the update message, we will create a model class to keep a Fields value, which is a list of key-value pairs for all the fields related to the item and the first or subsequent fragment. Then we need to use the Newtonsoft JSON.NET library to deserialize the JSON data to the object and access the field and its value from the object instead.

Below is a MrnStoryData class, which is a model class for the update message. We will add the class to new file MRNStoryData.cs.

```csharp
public class MrnStoryData
    {
        public MrnStoryData()
        {
        }
        public MessageTypeEnum MsgType { get; set; }
        public long PROD_PERM { get; set; }
        public long RECORDTYPE { get; set; }
        public string RDN_EXCHD2 { get; set; }
        public double CONTEXT_ID { get; set; }
        public long DDS_DSO_ID { get; set; }
        public string SPS_SP_RIC { get; set; }
        public string ACTIV_DATE { get; set; }
        public long TIMACT_MS { get; set; }
        public string GUID { get; set; }
        public string MRN_V_MAJ { get; set; }
        public MrnTypeEnum MRN_TYPE { get; set; }
        public string MRN_V_MIN { get; set; }
        public string MRN_SRC { get; set; }
        public long FRAG_NUM { get; set; }
        public long TOT_SIZE { get; set; }
        public byte[] FRAGMENT { get; set; }
        public long FragmentSize { get; set; }
        ...
    }
```
Then we will change the Stream API to request MRN Story data instead:
```csharp
 using (IStream stream = DeliveryFactory.CreateStream(
                new ItemStream.Params().Session(session)
                    .Name("MRN_STORY")
                    .WithDomain("NewsTextAnalytics")
                    .OnRefresh((s, msg) => Console.WriteLine($"{msg}\n\n"))
                    .OnUpdate((s, msg) => ProcessMRNUpdateMessage(msg))
                    .OnError((s, msg) => Console.WriteLine(msg))
                    .OnStatus((s, msg) => Console.WriteLine($"{msg}\n\n"))))
            {
                // Open the stream...
                stream.Open();

                // Wait for data to come in then hit any key to close the stream...
                Console.ReadKey();
            }
```
The application should verify the completion of the update message before calling the JSON.NET library to deserialize the JSON data to the class. We will add ProcessMRNUpdateMessage to Program class and add below snippet of codes to verify the data before convert it to the MrnStoryData object.

```csharp
private static void ProcessMRNUpdateMessage(JObject updatemsg)
        {
  
            if (updatemsg == null) throw new ArgumentNullException(nameof(updatemsg));
            if (updatemsg.ContainsKey("Domain") && updatemsg["Domain"].Value<string>() == "NewsTextAnalytics" 
                                                && updatemsg.ContainsKey("Fields") && updatemsg["Fields"]!=null)
            {
                
                var mrnUpdateData=updatemsg["Fields"].ToObject<MrnStoryData>();
                ProcessFieldData(mrnUpdateData);
            }

        }
```
Next step, we will keep the MrnStoryData in the .NET Dictionary, where the number of a new Story Fragment is a key in the Dictionary. Below is a snippet of codes of the Dictionary object.

```csharp
#region MRNDataProcessing
      private static readonly Dictionary<int, Model.MrnStoryData> _mrnDataList = new Dictionary<int, Model.MrnStoryData>();
      private static int UpdateCount { get; private set; }  
#endregion
```

To process content from MrnStoryData object, we will create the ProcessFieldData function in the Program class and add two main app logics to the function. You can find the full source files from [Github](https://github.com/Refinitiv-API-Samples/Example.RDP.RDPDotNetMRNStoryConsumer).

```csharp
 private static bool ProcessFieldData(Model.MrnStoryData mrnData)
 {
   //Codes to handle MRN fragments
 }
```
The first step in this function is to add a new Fragment or updating Fragment with the data from the subsequent updates. The second step is the codes to verify whether or not the update is the last fragment, and we can decompress the data using the JSON library.

Below is the snippet of codes for the first part. It uses the UpdateCount variable to keep track of the Fragment Count.

```csharp
            mrnData.MsgType = Enum.MessageTypeEnum.Update;
            var newUpdateByteArray = mrnData.FRAGMENT ?? throw new ArgumentNullException("mrnData.FRAGMENT");
            var newUpdateFragmentSize = (int?)newUpdateByteArray?.Length ?? 0;

            if (mrnData.FRAG_NUM == 1 && mrnData.TOT_SIZE > 0)
            {
                //Shrink FRAGMENT size to TOT_SIZE
                mrnData.FRAGMENT = new byte[mrnData.TOT_SIZE];
                Buffer.BlockCopy(newUpdateByteArray ?? throw new InvalidOperationException(), 0,
                    mrnData.FRAGMENT, 0, (int)newUpdateFragmentSize);
                mrnData.FragmentSize = newUpdateFragmentSize;
                _mrnDataList.Add(UpdateCount, mrnData);
            }
            else if (mrnData.FRAG_NUM > 1)
            {
                if (_mrnDataList[UpdateCount].MRN_SRC == mrnData.MRN_SRC && _mrnDataList[UpdateCount].GUID == mrnData.GUID)
                {
                    var tmpByteArray = _mrnDataList[UpdateCount].FRAGMENT;
                    var tmpTotalSize = _mrnDataList[UpdateCount].TOT_SIZE;
                    var tmpFragmentSize = _mrnDataList[UpdateCount].FragmentSize;

                    _mrnDataList[UpdateCount] = mrnData;
                    _mrnDataList[UpdateCount].FRAGMENT = tmpByteArray;
                    _mrnDataList[UpdateCount].TOT_SIZE = tmpTotalSize;
                    _mrnDataList[UpdateCount].FragmentSize = tmpFragmentSize;

                    Buffer.BlockCopy(newUpdateByteArray, 0,
                        _mrnDataList[UpdateCount].FRAGMENT,
                        (int)_mrnDataList[UpdateCount].FragmentSize, (int)newUpdateFragmentSize);

                    // Calculate current Fragment Size
                    _mrnDataList[UpdateCount].FragmentSize += newUpdateFragmentSize;
                }
                else
                {
                    var msg =
                        $"Cannot find previous update with the same GUID {mrnData.GUID}. This update will be skipped.";
                    Console.WriteLine($"Error {DateTime.Now} {msg}");
                    UpdateCount++;
                }
            }
```

The next step is to verify whether or not the size of concatenated fragment data equal to Fragment size and then we can decompress it to JSON plain-text. In the snippet codes, we will create StoryData class which is a class to hold data for the MRN Story. We will convert the JSON data to the class and pass it to display function or just print the value of Headline and Body to console output.

```csharp
               // Check if the update contains complete MRN Story 
            if (_mrnDataList[UpdateCount].IsCompleted)
            {
                Console.WriteLine($"GUID:{_mrnDataList[UpdateCount].GUID}");
                _mrnDataList[UpdateCount].JsonData = DataUtils
                    .UnpackByteToJsonString(_mrnDataList[UpdateCount].FRAGMENT).GetAwaiter().GetResult();
                if (_mrnDataList[UpdateCount].MRN_TYPE == Enum.MrnTypeEnum.STORY)
                {

                    var mrnobj = JsonConvert.DeserializeObject<Model.StoryData>(_mrnDataList[UpdateCount].JsonData);

                    Console.WriteLine($"========================= Story update=======================");
                    Console.WriteLine($"AltId:{mrnobj.AltId}");
                    Console.WriteLine($"Headline:{mrnobj.Headline}\n");
                    Console.WriteLine($"Body:{mrnobj.Body}");
                    Console.WriteLine("==============================================================");
                    //Console.WriteLine($"Story Update {DateTime.Now}\n {_mrnDataList[UpdateCount].JsonData}");
                }
                else
                {
                    // In case that item name is not MRN_STORY just print JSON message to console
                    string jsonFormatted = JValue.Parse(_mrnDataList[UpdateCount].JsonData).ToString(Formatting.Indented);
                    Console.WriteLine(jsonFormatted);
                }

                UpdateCount++;
               ;
            }
            else
            {
                if (_mrnDataList[UpdateCount].FragmentSize > _mrnDataList[UpdateCount].TOT_SIZE)
                {
                    var msg = $"Received message with GUID={_mrnDataList[UpdateCount].GUID} has a size greater than total message size. This update will be skipped.";
                    Console.WriteLine(msg);
                    Console.WriteLine($"Error {DateTime.Now} {msg}");
                    UpdateCount++;
                }
            }

```

Note that the sample codes were created to demonstrate the necessary step to concatenate, and decompress the MRN data fragments. The algorithm may not cover all scenarios that might happen while running the example app. You can run the project downloaded from [Github](https://github.com/Refinitiv-API-Samples/Example.RDP.RDPDotNetMRNStoryConsumer) with your TREP or ERT in Cloud account to see the result. You can modify the codes to use your data structure and add your algorithm to the codes. 

The first approach requires more steps and codes to concatenate and verify the fragments before decompressing the MRN Story data to JSON plain-text. This approach may be suitable for a developer who wants want to leverage the functionality of the Session or Delivery layer provided by the library. And they want to control the algorithm to handle the MRN data themself. Some peoples may wish to logs or keep the refresh and update message along with the status in their storage or database system. 



### Using RDP Content library to retreive MRN Story

We will talk about the second approach, which uses more shorter and easier codes. RDP.NET library provides a more easier way to retrieve particular content such as Price and MachineReadableNews or MRN data. To use the library for the content layer, we need to add the Nuget RDP.NET Content library to the project. You can use the following CLI to add the package as well. Note that the current version at the time we write this article still be a pre-release version 1.0.0-alpha.

```
dotnet add package Refinitiv.DataPlatform.Content --version 1.0.0-alpha
```

Then add following modules to Program.cs.

```csharp
using Refinitiv.DataPlatform.Content;
using Refinitiv.DataPlatform.Content.Streaming;
```

To retrieve MRN Story data, you can use ContentFactory rather than DeliveryFactory to create MachineReadableNews object. The application can use ContentFactory.CreateMachineReadableNews to create the object like the following snippet codes.

```csharp
using (MachineReadableNews mrnNews =
                ContentFactory.CreateMachineReadableNews(new MachineReadableNews.Params()
                              .Session(session)
                              .WithNewsDatafeed("MRN_STORY")
                              .OnError((e,msg)=>Console.WriteLine(msg))
                              .OnStatus((e, msg) => Console.WriteLine(msg))
                              .OnNews((e, msg) => Console.WriteLine(msg))))
            {
                mrnNews.Open();
            }
```

From the codes, you need to pass the session object to Session param and then set WithNewsDatafeed param to "MRN_STORY" if you wish to request MRN_STORY. Note that you can also change WithNewsDataFeed to retrieve MRN_TRNA and MRN_TRSI as well. 

When using the ContentFactory, you don't need to worry about concatenating and verifying the fragments. The Content layer will take care of this process on behalf of the application. The developer just need to define a function to process data from OnNews callback. The msg variable from OnNews is JObject and it holds the MRN JSON data contains the same structure as described in the MRN Data Model. So you can create your model class and deserialize the JSON data to the class and then pass the object back to the application layer.  

The second approach is easier to use and it saves your time. Also it suitable for application developers who want to use small effort to request MRN content and interested only MRN data in JSON plain-text format. Anyway, if the application wants to access the raw data from the Refresh and Update message provided by the WebSocket Server. This one is not a recommended approach.

## Troubleshoot RDP.NET issue

If you found an issue when using the library, RDP.NET library normally create log file for you therefore you can open the file to review the messages that the library send and receive.

RDP.NET library uses NLog library inside, and by default, it will create a logger file name RDPLog_<pid>.log under a running directory. You can change the default logger level to trace all messages so you can review the credentials and JSON messages the application send and receive from the server from log file.

To change the Log level, you can add the following codes before creating the session.

```csharp
Log.Level = NLog.LogLevel.Trace;
```
You can use the information from the RDP.NET log file to analyze the data behavior and investigate the issue. The log also contains the authentication request and response along with the headers it send and receive from server. Below is a sample trace log generated by the library.

```
2020-02-11 15:30:28.4134|Info|Refinitiv.DataPlatform.Log|1|Changed min log level from: <default> to Trace for rule:
	logNamePattern: (:All) levels: [ Info Warn Error Fatal ] appendTo: [ logfile ]
2020-02-11 15:30:28.4858|Debug|Refinitiv.DataPlatform.Log|1|Created a Platform Session with parameters:
"{
	Grant: GrantPassword {
		User: xxx
		Password: ********
		Scope: trapi
	}
	TakeSignonControl: True
	AppKey: xxx
	DACS User Name: xxx
	DACS Position: xxxx
	DACS Application ID: 256
}
"
2020-02-11 15:30:28.5426|Debug|Refinitiv.DataPlatform.Core.PlatformSessionCore|1|Sending HTTP request:
"Method: POST, RequestUri: 'https://api.refinitiv.com/auth/oauth2/v1/token', Version: 1.1, Content: System.Net.Http.FormUrlEncodedContent, Headers:
{
  Accept: application/json
  Authorization: Basic
  Content-Type: application/x-www-form-urlencoded
}"
2020-02-11 15:30:31.2920|Debug|Refinitiv.DataPlatform.Core.SessionCore|7|HTTP Response: "StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.HttpConnectionResponseContent, Headers:
{
  Date: Tue, 11 Feb 2020 08:30:31 GMT
  Transfer-Encoding: chunked
  Connection: keep-alive
  X-Amzn-Trace-Id: Root=1-5e426625-0625c9c408143160a0900ef0
  X-Served-By: region=us-east-1; cid=47df87a1-3f87-4ab6-95c7-2e42e0b4791b
  X-Tr-Requestid: xxx
  Content-Type: application/json
}"
2020-02-11 15:30:31.6036|Debug|Refinitiv.DataPlatform.Core.PlatformSessionCore|7|RDP Grant Password Authorization succeeded. Status: "{
  "HTTPStatusCode": 200,
  "HTTPReason": "OK"
}".  Content: "{
  "access_token": "eyJ0eXAiOiJ*********4wnvqHNPl0u-p5jdetCjs3Wgcj5XMaC8GaqHYBeSkA",
  "refresh_token": "7ca************a29",
  "expires_in": "300",
  "scope": "... trapi.transfer-job.ctrl trapi.user-framework.mobile.crud trapi.user-framework.recently-used.crud trapi.user-framework.workspace.crud",
  "token_type": "Bearer"
}"
2020-02-11 15:30:31.6230|Debug|Refinitiv.DataPlatform.Core.PlatformSessionCore|7|Sending HTTP request:
"Method: GET, RequestUri: 'https://api.refinitiv.com/streaming/pricing/v1/?dataformat=tr_json2', Version: 1.1, Content: <null>, Headers:
{
  Accept: application/json
  Authorization: Bearer eyJ0eXAiOiJhdCt******************tCjs3Wgcj5XMaC8GaqHYBeSkA
}"
2020-02-11 15:30:32.2321|Debug|Refinitiv.DataPlatform.Core.SessionCore|7|HTTP Response: "StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.HttpConnectionResponseContent, Headers:
{
  Date: Tue, 11 Feb 2020 08:30:32 GMT
  Connection: keep-alive
  ...
  Content-Length: 1576
}"
2020-02-11 15:30:32.3310|Info|Refinitiv.DataPlatform.Core.PlatformSessionCore|7|Platform Session Successfully Authenticated
2020-02-11 15:30:32.3343|Debug|Refinitiv.DataPlatform.Delivery.DeliveryFactory|1|Creating an ItemStream with parameters:
"{
	Session: Platform Session
	Service: <Default service defined within server>
	Name: MRN_STORY
	Domain: NewsTextAnalytics
	Fields: <Default to ALL fields>
	Streaming: True
	Extended Parameters: <Default: no extended parameters>
}
"
2020-02-11 15:30:32.3343|Debug|Refinitiv.DataPlatform.Core.PlatformSessionCore|7|RDP expects a token refresh to occur every 300 seconds.  Our session will attempt to refresh every 270 seconds.
2020-02-11 15:30:32.4299|Debug|Refinitiv.DataPlatform.Core.PlatformSessionCore|7|Platform Session State is Opened
2020-02-11 15:30:32.4299|Debug|Refinitiv.DataPlatform.Delivery.Stream.StreamCore|1|Request to Open stream.  Current state: Closed.  Stream HashCode: 3429838
2020-02-11 15:30:32.5204|Info|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|1|Streaming Connection initiated using:
	Host: apac-3.pricing.streaming.edp.thomsonreuters.com:443
	AuthToken: eyJ0eXAiOiJhdCt***************qHNPl0u-p5jdetCjs3Wgcj5XMaC8GaqHYBeSkA
	AuthPosition: 10.42.61.126
	ApplicationID: 256
2020-02-11 15:30:33.5117|Info|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|4|Successfully connected into the WebSocket server: apac-3.pricing.streaming.edp.thomsonreuters.com:443
2020-02-11 15:30:33.5253|Debug|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|4|Stream request: "{
  "ID": 1,
  "Domain": "Login",
  "Key": {
    "Name": "u8009179",
    "Elements": {
      "ApplicationId": "256",
      "AppKey": "256",
      "Position": "10.42.61.126",
      "AuthenticationToken": "eyJ0eXAiOi*******tCjs3Wgcj5XMaC8GaqHYBeSkA"
    },
    "NameType": "AuthnToken"
  }
}"
2020-02-11 15:30:36.3569|Trace|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|7|Refresh response: "{
  "ID": 1,
  "Type": "Refresh",
  "Domain": "Login",
  "Key": {
    "Name": "AQIC5wM2L**************zEAAjI2%23",
    "Elements": {
      "AllowSuspectData": 1,
      "ApplicationId": "256",
      "ApplicationName": "ADS",
      "AuthenticationErrorCode": 0,
      "AuthenticationErrorText": {
        "Type": "AsciiString",
        "Data": null
      },
      "AuthenticationTTReissue": 15xxxx0,
      "Position": "1xxxx",
      "ProvidePermissionExpressions": 1,
      "ProvidePermissionProfile": 0,
      "SingleOpen": 1,
      "SupportEnhancedSymbolList": 1,
      "SupportOMMPost": 1,
      "SupportPauseResume": 0,
      "SupportStandby": 0,
      "SupportBatchRequests": 7,
      "SupportViewRequests": 1,
      "SupportOptimizedPauseResume": 0
    }
  },
  "State": {
    "Stream": "Open",
    "Data": "Ok",
    "Text": "Login accepted by host ads-premium-xxxx."
  },
  "Elements": {
    "PingTimeout": 30,
    "MaxMsgSize": 61430
  }
}"
2020-02-11 15:30:36.3711|Info|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|7|Successfully logged into streaming server apac-3.pricing.streaming.edp.thomsonreuters.com:443
2020-02-11 15:30:36.4017|Debug|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|7|Stream request: "{
  "Key": {
    "Name": "MRN_STORY"
  },
  "Domain": "NewsTextAnalytics",
  "ID": 2
}"
2020-02-11 15:30:36.5003|Trace|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|7|Refresh response: "{
  "ID": 2,
  "Type": "Refresh",
  "Domain": "NewsTextAnalytics",
  "Key": {
    "Service": "ELEKTRON_DD",
    "Name": "MRN_STORY"
  },
  "State": {
    "Stream": "Open",
    "Data": "Ok",
    "Text": "*All is well"
  },
  "Qos": {
    "Timeliness": "Realtime",
    "Rate": "JitConflated"
  },
  "PermData": "AwEBEAAc",
  "SeqNumber": 27232,
  "Fields": {
    "PROD_PERM": 10001,
    "ACTIV_DATE": "2020-02-08",
    "RECORDTYPE": 30,
    ...
    "MDUTM_NS": null,
    "FRAG_NUM": 1,
    "TOT_SIZE": 0,
    "FRAGMENT": null
  }
}"
2020-02-11 15:30:36.5309|Debug|Refinitiv.DataPlatform.Delivery.Stream.StreamCore|7|Stream: 3429838 Open request completed.  State: Opened
2020-02-11 15:30:36.7629|Trace|Refinitiv.DataPlatform.Delivery.Stream.StreamConnection|7|Update response: "{
  "ID": 2,
  "Type": "Update",
  "Domain": "NewsTextAnalytics",
  "UpdateType": "Unspecified",
  "DoNotConflate": true,
  "DoNotRipple": true,
  "DoNotCache": true,
  "Key": {
    "Service": "ELEKTRON_DD",
    "Name": "MRN_STORY"
  },
  "PermData": "AwEBEBcM",
  "SeqNumber": 27262,
  "Fields": {
    "TIMACT_MS": 30636638,
    "ACTIV_DATE": "2020-02-11",
    "MRN_TYPE": "STORY",
    "MRN_V_MAJ": "2",
    "MRN_V_MIN": "10",
    "TOT_SIZE": 1389,
    "FRAG_NUM": 1,
    "GUID": "Anpmr10Da_2002112CSi7yoVtSWMkKUghC2+r5RsrvYXMxFUxOzCaN",
    "MRN_SRC": "SGW_PRD_A",
    "FRAGMENT": "H4sIAAAAAAAC/41W23LiRhB9z1d08bK7tZgFYXzhDWN212UbO+DbbkilRqgNA9IMGY3AdioflN/Il+X0CPmyqUqlimJaren76W79UVOpP0lq3ZrpmVXmWs1jVavXVJFoNlPOa91fasPLbm94Oaz9Wq/FNnnE3YnpnY+vBqPj3jm9730efjo6u7g4PxqMvnygHRopk+ReJbRynHtmlzDZFTvltTXMKc0sJ2RNoswyp4QnJrOsU71YMmVq6uyOzSBvEj2bc8KmQcfK09SaaVok7NjQko1hl9NaGRod9UkbYrAdq6nX0GdXUEtzqOA0L1aJ8hzugllo/8TgO9GqXKwXtIGHNGdPiV5r2EtiTlknIjExz8GoNCdEUxiKJapsxWnSmLy+kSm1hKGY4V5CMBqU7tbbEXHhLCEFpMQnpKAyJf7QRqcICYFB342ky4G1yH2IrYwFFh3SE6tUmW3cW7MNurBLWlvrnvRLaiZmHRSJO6k12szwTi5VHsyt5DIX9VCd6YUJwZy+yuwprI95WjiNpObiEjJHFta2+bUzMVCFgkgQBCwFYYQ+MTEnTi/uG3SLkDdKik02e2Jvjd/o5ZJTuQ5trMW/pVPTuadMS32QQPMcFooVyhojSTPkAqVUKoVVWqCIoiGcGfId1Q8APDtl48vUhOwp5yu3yGguNlAnInW4VaKgck3AtFCmUE5Tm7xFBSt9lCpETKme+eAdb+1PTAA5lD1Dw4fsCWYMzwB8RhJupPCS/hDlk0olCGczwcAMfDISQ+gH5Wb8Jkfi4XPFpVAnwy9kLGdwGz80WpF6JYl5jQ24KqY8I7trlaaoUl1g6t7AnTTgskEIZUV/SPwNKHFVelMJrnnKWSwQRT1fl4rdRgILxYY+Lfit/HgCvihEFRC3QrvDKwoghI2hUrl/65Nor8RRedQR93NMlKVgcC1G0dNAF9NS5bl31mYN+ir1A3ptiogD3KpREXwT0dXEzMHwqIVRwSaHR1pu/g2SUN6yMVBOGShzpe59Gc/cpsmOStbSHFnQAwhbKEsslIui3YPQ+Y3QyHPm+3AJ4RQew+Deuuy/5TuVvFQ7xoSEAkGCmEcYb+2/Fl+W5qN6p7nVIJmphs9zWYwtx/MEs9bkiUIvYvI6Zszl5mEj6lBROGrWox86ACXsQHflnQzSjDnULPQ98s4GLQ1gpLiuw+TtDe52BC4PIZ8T8x5LhUachJlNskUGmPE201wnzhQQwtvn98p/UGbVMGmdPrZbFDWps9ekveZ+80Opa2L+/gtsvIDSBvUAdVIFprXLHZeFDzPDqxhLp2IJyGOuQAghLJnVo8Pa8TmpcoZDIEd/lTy0Gbs1Jw2syHsN1PSxcjzL+hTjO81op9W6ah50281ue6/R6bS+4+acVYImZtwaX130T297V/2v9O64gvpzOWLNiS+XzDvIadH7vJV/i5rNqNWK+mO9/2hv/Pj2fHl6PZv3o4+uM8rd+tvd+cPn64eLp74airSRHYoVfnGPJY7djc0xK1A+2fUpLmSc53i8elyBFeFZZ9uHmucH/2mVKm1wD6UXRx34w7F8CQiviMcYNwW+D2ow47tFruKU8SYv4gVPffhwOOq2otYumCAOyzNqV+dhee5G4dyyd9vlIS+/dNvhf/80HL3y/6w87nCcd1s/H5fn7TCcku1Rd9QbHjd6Y9DDqHt0dn1XUv3zy+G3ihyPX6jmQUkPrkcXl6/IwZb+3i+Jr6PxK6pVkifD4/ELVak6GQ9GL1TFPRuelUR1jkc3/a307eAaxGV3NzrsHBzs7kX78r3l1ZLH/Hsh32K1bqteKzBCzRTfYO16DUMwxxfV/8Dgnz/9A1u5/QPoCQAA"
  }
}"
...
```
## Build and Run example application

You can download the solution from [Github](https://github.com/Refinitiv-API-Samples/Example.RDP.RDPDotNetMRNStoryConsumer). There are two project folders in the solution. The first project is a folder named __MRNStoryConsumer_Method1__. It's the project for the first approach. And the second one is folder __MRNStoryConsumer_Method2__. It's the project for the second approach.

Before building and runing the app, you have to modify the credential for TREP or ERT in Cloud in the Program.cs file . If you wish to test with the TREP server please set useRDP variable in the Main method to false and then save the file. 

### Build and Run the project

In command-line mode, go to the project folder which contains .csproj and type the following command:
```
dotnet run
```

Or if you want to build the project to executable file you can type the following command:

```
dotnet build -c release -r win10-x64 -o ./release-x64
```
It will create an executable file in a release-x64 folder. Then you can run MRNStoryConsumer_Method1.exe directly from the release folder.

The above command is for windows 10 64 bit. If you want to build it on Linux or macOS, you can change it to one of the lists from [.NET Core RID Catalog](https://docs.microsoft.com/th-th/dotnet/core/rid-catalog). 

For example, 

On the macOS, you have to change it to "osx-x64".
```
dotnet build -c release -r osx-x64 -o ./release-x64
```
It should generate executable file MRNStoryConsumer_Method1 under the release-x64 folder.

You can use the same command to build any project.

Anyway, if you have Visual Studio Code and installed .NET Core Development plugin, you can open the project folder and click Start Debug or Start without Debug from the editor.

## Summary

This article provides a brief details of the RDP.NET library. It shows a sample usage to demonstrate how to use the library in the application to retrieve real-time MRN Story data from TREP, RDP, or ERT in Cloud. There are two main approaches provided in this article to process the MRN data.  The first one is to manually process the MRN Story update message and implement data caching algorithm to verify the MRN fragment and decompress the MRN Story data to JSON plain-text. The second approach is an easier way to retrieve the MRN Story data using the RDP.NET Content library. The RDP.NET Content library provides ContentFactory class and method CreateMachineReadableNews to create MachineReadableNews object. It provides OnNews callback function which returns MRN Story JSON data. The second approach is quite easy to use and should save development times. By the way, it may not be suitable for the project that requires access to original data from the Refresh and Update message provided by the WebSocket Server. Hence the recommended approach depending on the condition and preference of the user.

## Download

You can download full source files and projects from [GitHub](https://github.com/Refinitiv-API-Samples/Example.RDP.RDPDotNetMRNStoryConsumer).

## References

* [The Refinitiv Data Platform Libraries for .NET (RDP.NET)](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries) 

* [The Introduction to RDP Libraries document](https://developers.refinitiv.com/refinitiv-data-platform/refinitiv-data-platform-libraries/docs?content=62446&type=documentation_item)

* [Refinitiv DataPlatform for .NET Nuget](https://www.nuget.org/packages/Refinitiv.DataPlatform/)

* [Refinitiv DataPlatform Content Nuget](https://www.nuget.org/packages/Refinitiv.DataPlatform.Content/)

* [Elektron Websocket](https://developers.refinitiv.com/elektron/websocket-api/quick-start)

* [MRN DATA MODELS AND ELEKTRON IMPLEMENTATION GUIDE](https://developers.refinitiv.com/sites/default/files/ThomsonReutersMRNElektronDataModelsv210_2.pdf)