using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace KLogger.Libs
{
    internal class SlackWebhook
    {
        #region ignore convention - slack protocol
        
        // 슬랙이 요청하는 키(key)로 대소문자도 지켜야 한다. 예약어와 겹치면 아래 속성을 사용한다.
        // [JsonProperty(PropertyName = "default")]
        class Payload
        {
            public String channel { get; set; }
            public String username { get; set; }
            public String icon_emoji { get; set; }
            public Attachment[] attachments { get; set; }
        }
        // https://api.slack.com/docs/message-attachments
        class Attachment
        {
            public String title { get; set; }
            public String text { get; set; }
            public String color { get; set; }
            public String footer { get; set; }
        }

        #endregion

        public Int32 SendingCount => _sendingCount;

        // SEND_RATE_LIMIT_SEC 초 동안,
        // SEND_RATE_LIMIT_COUNT 개 넘게 전송 요청을 보내면 요청은 무시되고,
        // SEND_RATE_WARNING_INTERVAL_SEC 마다 경고 메시지를 보낸다.
        private const Int32 SEND_RATE_LIMIT_SEC = 1;
        private const Int32 SEND_RATE_LIMIT_COUNT = 10;
        private const Int32 SEND_RATE_WARNING_INTERVAL_SEC = 10;

        private readonly Encoding _encoding = new UTF8Encoding();
        private readonly Uri _webhookUrl;
        private readonly String _defaultChannel;
        private readonly String _defaultUserName;
        private readonly String _defaultIconEmoji;
        private readonly Int32 _addUTCHour;
        private readonly String _ip;
        private readonly String _machineName;

        private Int64 _sequence;
        private Int32 _sendingCount;
        private DateTime _sendRateTime;
        private Int32 _sendRateCount;
        private DateTime _sendRateWarningTime;

        public SlackWebhook(String webhookUrl, String defaultChannel, String defaultUserName, String defaultIconEmoji, Int32 addUTCHour)
        {
            _webhookUrl = new Uri(webhookUrl);
            _defaultChannel = defaultChannel;
            _defaultUserName = defaultUserName;
            _defaultIconEmoji = defaultIconEmoji;
            _addUTCHour = addUTCHour;
            _ip = GetLocalIPAddress() ?? String.Empty;
            _machineName = Environment.MachineName;
            _sequence = -1;
            _sendingCount = 0;
            _sendRateTime = DateTime.MinValue;
            _sendRateCount = 0;
            _sendRateWarningTime = DateTime.MinValue;
        }

        public void SendAsync(String channel,
                              String userName,
                              String title,
                              String text,
                              String color,
                              String iconEmoji,
                              Boolean isUrgent = false)
        {
            SendImpl(channel, userName, title, text, color, iconEmoji, false, isUrgent);
        }

        public void SendSync(String channel,
                             String userName,
                             String title,
                             String text,
                             String color,
                             String iconEmoji,
                             Boolean isUrgent = false)
        {
            SendImpl(channel, userName, title, text, color, iconEmoji, true, isUrgent);
        }

        public Boolean WaitForAsyncSend(Int32 waitMS)
        {
            var sw = new Stopwatch();
            sw.Start();

            while (0 < _sendingCount)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(10));

                if (waitMS < sw.ElapsedMilliseconds)
                {
                    return false;
                }
            }

            sw.Stop();

            return true;
        }

        private void SendImpl(String channel,
                              String userName,
                              String title,
                              String text,
                              String color,
                              String iconEmoji,
                              Boolean sync,
                              Boolean isUrgent)
        {
            channel = channel   ?? _defaultChannel;
            userName = userName ?? _defaultUserName;
            iconEmoji = iconEmoji ?? _defaultIconEmoji;

            if (isUrgent == false)
            {
                DateTime now = DateTime.Now;
                UpdateSendRate(now);

                if (IsOverSendRate())
                {
                    SendWarningSendRate(now, channel, userName);
                    return;
                }
            }

            Interlocked.Increment(ref _sequence);

            if (sync)
            {
                SendMessageSync(channel, userName, title, text, color, iconEmoji);
            }
            else
            {
                SendMessageAsync(channel, userName, title, text, color, iconEmoji);
            }
        }

        private void UpdateSendRate(DateTime now)
        {
            if (TimeSpan.FromSeconds(SEND_RATE_LIMIT_SEC) < now - _sendRateTime)
            {
                Interlocked.Exchange(ref _sendRateCount, 1);
            }
            else
            {
                Interlocked.Increment(ref _sendRateCount);
            }

            _sendRateTime = now;
        }

        private Boolean IsOverSendRate()
        {
            return SEND_RATE_LIMIT_COUNT < _sendRateCount;
        }

        private void SendWarningSendRate(DateTime now, String channel, String userName)
        {
            const String TITLE = "WARNING";
            const String COLOR = "warning";
            const String ICON_EMOJI = ":warning:";

            if (TimeSpan.FromSeconds(SEND_RATE_WARNING_INTERVAL_SEC) < now - _sendRateWarningTime)
            {
                _sendRateWarningTime = now;

                String warningMessage = $"Too Many Messages(`{_sendRateCount}/{SEND_RATE_LIMIT_SEC}` s). Request Ignored.";
                SendMessageAsync(channel, userName, TITLE, warningMessage, COLOR, ICON_EMOJI);
            }
        }

        private void SendMessageSync(String channel,
                                     String userName,
                                     String title,
                                     String text,
                                     String color,
                                     String iconEmoji)
        {
            using (var webClient = new WebClient())
            {
                var postData = new NameValueCollection
                               {
                                   ["payload"] = CreatePayloadJson(channel,
                                                                   userName,
                                                                   title,
                                                                   text,
                                                                   color,
                                                                   iconEmoji)
                               };

                webClient.UploadValues(_webhookUrl, "POST", postData);
            }
        }

        private void SendMessageAsync(String channel,
                                      String userName,
                                      String title,
                                      String text,
                                      String color,
                                      String iconEmoji,
                                      Action<String> completeAction = null)
        {
            Interlocked.Increment(ref _sendingCount);

            using (var webClient = new WebClient())
            {
                var postData = new NameValueCollection
                               {
                                   ["payload"] = CreatePayloadJson(channel,
                                                                   userName,
                                                                   title,
                                                                   text,
                                                                   color,
                                                                   iconEmoji)
                               };

                webClient.UploadValuesCompleted += (sender, e) =>
                                                   {
                                                       Interlocked.Decrement(ref _sendingCount);
                                                       completeAction?.Invoke(_encoding.GetString(e.Result));
                                                   };

                webClient.UploadValuesAsync(_webhookUrl, "POST", postData);
            }
        }

        private String CreatePayloadJson(String channel,
                                         String userName,
                                         String title,
                                         String text,
                                         String color,
                                         String iconEmoji)

        {
            var attachment = new Attachment
                             {
                                 title = title,
                                 text = text,
                                 color = color,
                                 footer = CreateFooter()
                             };

            return JsonConvert.SerializeObject(new Payload
                                               {
                                                   channel = channel,
                                                   username = userName,
                                                   icon_emoji = iconEmoji,
                                                   attachments = new[] { attachment }
                                               });
        }

        private String CreateFooter()
        {
            String date = (DateTime.UtcNow + TimeSpan.FromHours(_addUTCHour)).ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"[{_sequence}] `{date}` from `{_machineName}({_ip})`";
        }

        private String GetLocalIPAddress()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return String.Empty;
        }
    }
}
