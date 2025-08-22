using System;
using System.Threading;
using System.Collections.Generic;
using LSEG.Data;
using LSEG.Data.Core;
using LSEG.Data.Delivery.Stream;
using LSEG.Data.Content;
using LSEG.Data.Content.News;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


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

        #region DPUserCredential
            private const string DPClientID = "<Client ID>";
            private const string DPClientSecret = "<Client Secret>";            
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
                System.Console.WriteLine("Start DeployedPlatformSession");
                session = PlatformSession.Definition()
                        .Host(WebSocketHost)
                        .DacsUserName(RTDSUser)
                        .DacsApplicationID(appID)
                        .DacsPosition(position)
                        .GetSession()
                        .OnState((state, msg, s) =>
                        {
                            Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                            _sessionState = state;
                        })
                        .OnEvent((eventCode, msg, s) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})"));
            }
            else
            {
                System.Console.WriteLine("Start Data Platform Session");
                session = PlatformSession.Definition().OAuthGrantType(
                        new ClientCredentials().ClientID(DPClientID).ClientSecret(DPClientSecret))
                         .GetSession()
                         .OnEvent((eventCode, msg, s) => Console.WriteLine($"{DateTime.Now}: {msg}. (Event: {eventCode})"))
                         .OnState((state, msg, s) =>
                         {
                             Console.WriteLine($"{DateTime.Now}:  {msg}. (State: {state})");
                             _sessionState = state;
                         });
            }
            session.Open();
            if(_sessionState==Session.State.Opened)
            {
                System.Console.WriteLine("Session is now Opened");
                System.Console.WriteLine("Sending MRN_STORY request");
                using var mrnNews = MachineReadableNews.Definition()
                    .NewsDatafeed(MachineReadableNews.Datafeed.MRN_STORY).GetStream()
                    .OnError((err, stream) => Console.WriteLine($"{DateTime.Now}:{err}"))
                    .OnStatus((status, stream) => Console.WriteLine(status))
                    .OnNewsStory((newsItem, stream) => ProcessNewsContent(newsItem.Raw));
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
