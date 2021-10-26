using System;
using System.Threading;
using System.Collections.Generic;
using Refinitiv.DataPlatform;
using Refinitiv.DataPlatform.Core;
using Refinitiv.DataPlatform.Delivery.Stream;
using Refinitiv.DataPlatform.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refinitiv.DataPlatform.Content.News;

namespace MRNStoryConsumer_Method2
{
    class Program
    {
        #region RTDSCredential
            private const string RTDSUser = "<DACS Username>";
            private const string appID = "<App ID>";
            private const string position = "<Ip or Hostname>/net";
            private const string WebSocketHost = "<ADS/Websocker Server IP or Hostname>:<port>";
        #endregion

        #region RDPUserCredential
            private const string RDPUser = "<RDP User/Email>";
            private const string RDPPassword = "<RDP Password>";
            private const string RDPAppKey = "<App Key>";
        #endregion
        private static Session.State _sessionState = Session.State.Closed;
        private static readonly int runtime=60000000;
        static void Main()
        {
            // Set Logger level to Trace
            Log.Level = NLog.LogLevel.Trace;

            bool useRDP=true;
            ISession session;
            if(!useRDP)
            {
                System.Console.WriteLine("Start DeploytedPlatformSession");
                session = CoreFactory.CreateSession(new DeployedPlatformSession.Params()
                    .Host(WebSocketHost)
                    .WithDacsUserName(RTDSUser)
                    .WithDacsApplicationID(appID)
                    .WithDacsPosition(position)
                    .OnState((s, state, msg) =>
                    {
                        Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                        _sessionState=state;
                    })
                    .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})")));
            }else
            {
                System.Console.WriteLine("Start RDP PlatformSession");
                session = CoreFactory.CreateSession(new PlatformSession.Params()
                    .WithOAuthGrantType(new GrantPassword().UserName(RDPUser)
                        .Password(RDPPassword))
                    .AppKey(RDPAppKey)
                    .WithTakeSignonControl(true)
                    .OnState((s, state, msg) =>
                    {
                        Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                        _sessionState = state;
                    })
                    .OnEvent((s, eventCode, msg) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})")));
            }
            session.Open();
            if(_sessionState==Session.State.Opened)
            {
                System.Console.WriteLine("Session is now Opened");
                System.Console.WriteLine("Sending MRN_STORY request");
                using var mrnNews = MachineReadableNews.Definition().OnError((stream, err) => Console.WriteLine($"{DateTime.Now}:{err}"))
                    .OnStatus((stream, status) => Console.WriteLine(status))
                    .NewsDatafeed(MachineReadableNews.Datafeed.MRN_STORY)
                    .OnNewsStory((stream, newsItem) => ProcessNewsContent(newsItem.Raw));
                {
                    mrnNews.Open();
                    Thread.Sleep(runtime);
                }
                
            }
        }
      
        private static void ProcessNewsContent(JObject msg)
        {
            System.Console.WriteLine("***************** RAW JSON Data *******************");
            System.Console.WriteLine(msg);
            System.Console.WriteLine("***************************************************");
            var mrnobj = msg.ToObject<Model.StoryData>();

                    Console.WriteLine($"========================= Story update=======================");
                    System.Console.WriteLine($"GUID:{mrnobj.Id}");
                    Console.WriteLine($"AltId:{mrnobj.AltId}");
                    Console.WriteLine($"Headline:{mrnobj.Headline}\n");
                    Console.WriteLine($"Body:{mrnobj.Body}");
                    Console.WriteLine("==============================================================");
        }
    }
}
