using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using PubnubApi;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTest
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            int calls = int.Parse(ConfigurationManager.AppSettings["publishCalls"] ?? "500");
            // 1. Create helper instance
            // 2. subscribe to channel
            // 3. Publish messages by providing number of calls
            // 4. Generate log files
            // 5. Print summary on console

            PubNubHelper helper = new PubNubHelper();
            Console.WriteLine("Pubnub instance created...");

            helper.Subscribe();
            Console.WriteLine("Subscribed to channel");
            Console.WriteLine($"Making {calls} Publish calls..");
            await helper.Publish(calls);

            Console.WriteLine("print summary");
            Log.PrintSummmary();
            Console.WriteLine("Done with executions...\n Generating log files at executing exe path");
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
            pnConfiguration.Uuid = ConfigurationManager.AppSettings["uuid"] ?? "testUUID";
            PubNub = new Pubnub(pnConfiguration);

            PNConfiguration pnConfiguration2 = new PNConfiguration();
            pnConfiguration2.SubscribeKey = ConfigurationManager.AppSettings["Subscribekey"];
            pnConfiguration2.Uuid = ConfigurationManager.AppSettings["uuid2"] ?? "test2UUID";
            PubNub2 = new Pubnub(pnConfiguration2);
        }

        public void Subscribe()
        {
            string channel = ConfigurationManager.AppSettings["channel"] ?? "testchannel";
            PubNub2.Subscribe<string>()
                .Channels(new string[] { channel }).Execute();

            PubNub2.AddListener(new PnSubscribeCallback());
        }

        public async Task Publish(int number)
        {
            int delay = int.Parse(ConfigurationManager.AppSettings["delay"] ?? "200");
            string channel = ConfigurationManager.AppSettings["channel"] ?? "testchannel";
            for (int i = 0; i < number; i++)
            {
                string message = $"Message {i}";
                await PubNub.Publish().Channel(channel).Message(message).ExecuteAsync();
                Log.PublishLog(message);
                //PubNub.Publish().Channel(channel).Message(message).Execute
                //    (new PNPublishResultExt((result, status) => { Log.PublishLog(message); }));
                Thread.Sleep(delay);  // delay between calls
            }
        }
    }

    static class Log
    {
        static string recfileName = $"logs/Received_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm")}.log";
        static string pubfileName = $"logs/Publish_{DateTime.Now.ToString("MM_dd_yyyy_hh_mm")}.log";
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
                if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
                using (var sw = new StreamWriter(recfileName))
                {
                    foreach (var log in ReceiveLogs)
                    {
                        sw.WriteLine($"{log.Key}  :  {log.Value}");
                    }
                    sw.WriteLine("\n\n\n");
                    var firstLog = ReceiveLogs.FirstOrDefault();
                    var lastLog = ReceiveLogs.LastOrDefault();
                    var difference = (DateTime.Parse(lastLog.Key) - DateTime.Parse(firstLog.Key)).TotalMilliseconds;
                    var firstPublishLog = PublishLogs.FirstOrDefault();
                    var diff = (DateTime.Parse(lastLog.Key) - DateTime.Parse(firstPublishLog.Key));
                    sw.WriteLine($" First message received on {firstLog.Key}  \n Last message received on {lastLog.Key} \n\n  Time Elaspse {difference}ms");
                    sw.WriteLine($"\n\n Total time between first publish and last msg receive event is {diff.TotalMilliseconds}ms ~ {diff.TotalMinutes}minutes");
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
                if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
                using (var sw = new StreamWriter(pubfileName))
                {
                    foreach (var log in PublishLogs)
                    {
                        sw.WriteLine($"{log.Key}  :  {log.Value}");
                    }
                    sw.WriteLine("\n\n\n");
                    var firstLog = PublishLogs.FirstOrDefault();
                    var lastLog = PublishLogs.LastOrDefault();
                    var difference = (DateTime.Parse(lastLog.Key) - DateTime.Parse(firstLog.Key)).TotalMilliseconds;
                    var lastreceivedLog = ReceiveLogs.LastOrDefault();
                    var diff = (DateTime.Parse(lastreceivedLog.Key) - DateTime.Parse(firstLog.Key));
                    sw.WriteLine($" First message Publish on {firstLog.Key}  \n Last message published on {lastLog.Key} \n\n  Time Elaspse {difference}ms");
                    sw.WriteLine($"\n\n Total time between first publish and last msg receive event is {diff.TotalMilliseconds}ms ~ {diff.TotalMinutes}minutes");
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
            var lastReceived = DateTime.Parse(ReceiveLogs.LastOrDefault().Key);
            Console.WriteLine($"Total time between first publish and last msg receive event is {(lastReceived - firstPublish).TotalMilliseconds}ms ~ {(lastReceived - firstPublish).TotalMinutes}minutes");
            Console.WriteLine($"Total time between first publish and first msg receive event is {(firstReceived - firstPublish).TotalMilliseconds}ms");

        }
    }

    class PnSubscribeCallback : SubscribeCallback
    {
        public override void Message<T>(Pubnub pubnub, PNMessageResult<T> message)
        {
            Log.ReceiveLog($"{message.Publisher} on {message.Channel} : {message.Message}");
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
