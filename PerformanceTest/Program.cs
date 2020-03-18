using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using PubnubApi;
using System.Threading;

namespace PerformanceTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // 1. Create helper instance
            // 2. subscribe to channel
            // 3. Publish messages by providing number of calls
            // 4. Generate log files
            // 5. Print summary on console

            PubNubHelper helper = new PubNubHelper();
            helper.Subscribe();     
            helper.Publish(500);

            Log.PrintSummmary();

            Log.WritePublishLogsInFile();
            Log.WriteReceiveLogsInFile();

            Console.ReadLine();
        }
    }

    class PubNubHelper
    {
        Pubnub PubNub { get; set; }
        Pubnub PubNub2 { get; set; }
        public PubNubHelper()
        {
            PNConfiguration pnConfiguration = new PNConfiguration();
            pnConfiguration.SubscribeKey = ConfigurationManager.AppSettings["Subscribekey"];
            pnConfiguration.PublishKey = ConfigurationManager.AppSettings["Publishkey"];
            pnConfiguration.Uuid = ConfigurationManager.AppSettings["uuid"]??"testUUID";
            PubNub = new Pubnub(pnConfiguration);

            PNConfiguration pnConfiguration2 = new PNConfiguration();
            pnConfiguration2.SubscribeKey = ConfigurationManager.AppSettings["Subscribekey"];
            pnConfiguration2.Uuid = ConfigurationManager.AppSettings["uuid2"] ?? "test2UUID";
            PubNub2 = new Pubnub(pnConfiguration2);
        }

        public void Subscribe()
        {
            string channel = ConfigurationManager.AppSettings["channel"]??"testchannel";
            PubNub2.Subscribe<string>()
                .Channels(new string[] { channel }).Execute();

            PubNub2.AddListener(new PnSubscribeCallback());
        }

        public void Publish(int number)
        {
            string channel = ConfigurationManager.AppSettings["channel"]??"testchannel";
            for (int i = 0; i < number; i++)
            {
                string message = $"Message {i}";
                PubNub.Publish().Channel(channel).Message(message).ExecuteAsync();
                Log.PublishLog(message);
                Thread.Sleep(200);  // 200ms delay
            }
        }
    }

    static class Log
    {
        static Dictionary<string, string> ReceiveLogs = new Dictionary<string, string>();
        static Dictionary<string, string> PublishLogs = new Dictionary<string, string>();
        public static void ReceiveLog(string message)
        {
            string time = DateTime.Now.ToString("hh:mm:ss.fff");
            ReceiveLogs.Add(time, message);
        }
        public static void PublishLog(string message)
        {
            string time = DateTime.Now.ToString("hh:mm:ss.fff");
            PublishLogs.Add(time, message);
        }
        public static void WriteReceiveLogsInFile()
        {
            try
            {
                /**/
                string fileName = $"Received_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm")}.log";
                using (var sw = new StreamWriter(fileName))
                {
                    foreach (var log in ReceiveLogs)
                    {
                        sw.WriteLine($"{log.Key}  :  {log.Value}");
                    }
                    sw.WriteLine("\n\n\n");
                    var firstLog = ReceiveLogs.FirstOrDefault();
                    var lastLog = ReceiveLogs.LastOrDefault();
                    var difference = (DateTime.Parse(lastLog.Key) - DateTime.Parse(firstLog.Key)).Milliseconds;
                    sw.WriteLine($" First message received on {firstLog.Key}  \n Last message received on {lastLog.Key} \n\n  Time Elaspse {difference}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception WriteReceiveLogsInFile: {ex.StackTrace}");
            }
        }
        public static void WritePublishLogsInFile()
        {
            try
            {
                /**/
                string fileName = $"Publish_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm")}.log";
                using (var sw = new StreamWriter(fileName))
                {
                    foreach (var log in PublishLogs)
                    {
                        sw.WriteLine($"{log.Key}  :  {log.Value}");
                    }
                    sw.WriteLine("\n\n\n");
                    var firstLog = PublishLogs.FirstOrDefault();
                    var lastLog = PublishLogs.LastOrDefault();
                    var difference = (DateTime.Parse(lastLog.Key) - DateTime.Parse(firstLog.Key)).Milliseconds;
                    sw.WriteLine($" First message Publish on {firstLog.Key}  \n Last message published on {lastLog.Key} \n\n  Time Elaspse {difference}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception WritePublishLogsInFile: {ex.StackTrace}");
            }
        }
        public static void PrintSummmary()
        {
            var firstPublish = DateTime.Parse(PublishLogs.FirstOrDefault().Key);
            var firstReceived = DateTime.Parse(ReceiveLogs.FirstOrDefault().Key);

            var lastPublish = DateTime.Parse(PublishLogs.LastOrDefault().Key);
            var lastReceived = DateTime.Parse(ReceiveLogs.LastOrDefault().Key);

            Console.WriteLine($"Total time between first publish and last msg receive event is ${(lastReceived - firstPublish).Milliseconds}ms");

            Console.WriteLine($"\n Total time between first publish and first msg receive event is ${(firstReceived-firstPublish).Milliseconds}ms");
            Console.WriteLine($" Total time between last publish and first msg receive event is ${(firstReceived - lastPublish).Milliseconds}ms");

        }
    }

    class PnSubscribeCallback : SubscribeCallback
    {
        public override void Message<T>(Pubnub pubnub, PNMessageResult<T> message)
        {
            Log.ReceiveLog($"{message.Publisher}on:{message.Channel}:{message.Message}");
        }

        public override void MessageAction(Pubnub pubnub, PNMessageActionEventResult messageAction)
        {

        }

        public override void ObjectEvent(Pubnub pubnub, PNObjectApiEventResult objectEvent)
        {

        }

        public override void Presence(Pubnub pubnub, PNPresenceEventResult presence)
        {
 
        }

        public override void Signal<T>(Pubnub pubnub, PNSignalResult<T> signal)
        {
 
        }

        public override void Status(Pubnub pubnub, PNStatus status)
        {
                
        }
    }
}
