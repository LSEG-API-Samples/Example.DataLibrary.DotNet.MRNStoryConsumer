using System;
using System.Threading;
using System.Collections.Generic;
using Refinitiv.DataPlatform;
using Refinitiv.DataPlatform.Core;
using Refinitiv.DataPlatform.Delivery;
using Refinitiv.DataPlatform.Delivery.Stream;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace MRNStoryConsumer_Method1
{
    class Program
    {
        #region TREPCredential
            private const string TREPUser = "<DACS Username>";
            private const string appID = "<App ID>";
            private const string position = "<Ip or Hostname>/net";
            private const string WebSocketHost = "<ADS/Websocker Server IP or Hostname>:<port>";
        #endregion

        #region RDPUserCredential
            private const string RDPUser = "<RDP Username>";
            private const string RDPPassword = "<RDP Password>";
            private const string RDPAppKey = "<App Key>";
        #endregion

        #region MRNDataProcessing
            private static readonly Dictionary<int, Model.MrnStoryData> _mrnDataList = new Dictionary<int, Model.MrnStoryData>();
            private static int UpdateCount =0;
        #endregion
        private static Session.State _sessionState = Session.State.Closed;
        private static int runtime=60000000;
        static void Main(string[] args)
        {
            // Set Logger level to Trace
            Log.Level = NLog.LogLevel.Trace;

            bool useRDP=false;
            ISession session;
            if(!useRDP)
            {
                System.Console.WriteLine("Start DeploytedPlatformSession");
                session = CoreFactory.CreateSession(new DeployedPlatformSession.Params()
                    .Host(WebSocketHost)
                    .WithDacsUserName(TREPUser)
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
            }
            session.Open();
            if(_sessionState==Session.State.Opened)
            {
                System.Console.WriteLine("Session is now Opened");
                System.Console.WriteLine("Sending MRN_STORY request");
                using (IStream stream = DeliveryFactory.CreateStream(
                    new ItemStream.Params().Session(session)
                        .Name("MRN_STORY")
                        .WithDomain("NewsTextAnalytics")
                        .OnRefresh((s, msg) => Console.WriteLine($"{msg}\n\n"))
                        .OnUpdate((s, msg) => ProcessMRNUpdateMessage(msg))
                        .OnError((s, msg) => Console.WriteLine(msg))
                        .OnStatus((s, msg) => Console.WriteLine($"{msg}\n\n"))))
                {
                    stream.Open();
                    Thread.Sleep(runtime);
                }
            }
        }
        private static void ProcessMRNUpdateMessage(JObject updatemsg)
        {
  
            if (updatemsg == null) throw new ArgumentNullException(nameof(updatemsg));
            if (updatemsg.ContainsKey("Domain") && updatemsg["Domain"].Value<string>() == "NewsTextAnalytics" 
                                                && updatemsg.ContainsKey("Fields") && updatemsg["Fields"]!=null)
            {
                
                var mrnUpdateData=updatemsg["Fields"].ToObject<Model.MrnStoryData>();
                ProcessFieldData(mrnUpdateData);
            }

        }
        private static void ProcessFieldData(Model.MrnStoryData mrnData)
        {


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

        }
        
    }
}
