﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KLogger.Cores.Exceptions;
using KLogger.Libs;
using KLogger.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KLogger.Configs
{
    /// <summary>
    /// 로거의 설정. <see cref="Config.Create"/> 로 생성한다. 
    /// </summary>
    public class Config
    {
        public class AWS
        {
            /// <summary> Kinesis 의 Region </summary>
            [JsonProperty] public String Region { get; internal set; }

            /// <summary> <see cref="AccessID"/>, <see cref="SecretKey "/> 를 암호화 할 때 사용한 키. </summary>
            [JsonProperty] public String KEK { get; private set; }

            /// <summary> <see cref="KEK"/> 로 암호화한 AWS AccessID </summary>
            [JsonProperty] public String AccessID { get; private set; }

            /// <summary> <see cref="KEK"/> 로 복호화한 AccessID </summary>
            [JsonIgnore] public String DecryptedAccessID { get; internal set; }

            /// <summary> <see cref="KEK"/> 로 암호화한 AWS SecretKey </summary>
            [JsonProperty] public String SecretKey { get; private set; }

            /// <summary> <see cref="KEK"/> 로 복호화한 SecretKey </summary>
            [JsonIgnore] public String DecryptedSecretKey { get; internal set; }

            /// <summary> Kinesis Stream 의 이름 </summary>
            [JsonProperty] public String KinesisStreamName { get; internal set; }

            public void DecryptAccessIDAndSecretKey()
            {
                Byte[] kekBytes = new Byte[SimpleAES.AES_KEY_SIZE];
                Encoding.UTF8.GetBytes(KEK).ToArray().CopyTo(kekBytes, 0);

                Byte[] accessIDBytes = Convert.FromBase64String(AccessID);
                Byte[] secretKeyBytes = Convert.FromBase64String(SecretKey);

                DecryptedAccessID = SimpleAES.Decrypt(accessIDBytes, kekBytes);
                DecryptedSecretKey = SimpleAES.Decrypt(secretKeyBytes, kekBytes);

                KEK = KEK.HideString(1, true);  // 사용이 끝나고 가린다(Config 조회 시 가리는 효과).
            }
        }

        public class Watcher
        {
            /// <summary> 모니터링 동작 주기. 기본값은 <see cref="Const.WATCHER_INTERVAL_MS"/> </summary>
            [JsonProperty] public Int32 IntervalMS { get; internal set; }

            /// <summary> <see cref="IntervalMS"/> 동안 평균 로그 처리(Tick)시간 이 값보다 큰 경우 보고. 기본값은 <see cref="Const.REPORT_ELAPSED_MEAN_MS"/> </summary>
            [JsonProperty] public Int32 ReportElapsedMeanMS { get; internal set; }

            /// <summary> <see cref="IntervalMS"/> 동안 처리한 로그가 이 값보다 큰 경우 보고. 기본값은 <see cref="Const.REPORT_LOG_COUNT"/> </summary>
            [JsonProperty] public Int32 ReportLogCount { get; internal set; }

            /// <summary> <see cref="IntervalMS"/> 동안 로그 큐의 크기 평균이 이 값보다 큰 경우 보고. 기본값은 <see cref="Const.REPORT_QUEUE_MEAN_SIZE"/> </summary>
            [JsonProperty] public Int32 ReportQueueMeanSize { get; internal set; }

            /// <summary> <see cref="IntervalMS"/> 동안 로그 큐의 크기 표준편차가 이 값보다 큰 경우 보고. 기본값은 <see cref="Const.REPORT_QUEUE_STD_DEV"/> </summary>
            [JsonProperty] public Int32 ReportQueueStdDev { get; internal set; }

            public Watcher()
            {
                IntervalMS = Const.WATCHER_INTERVAL_MS;
                ReportElapsedMeanMS = Const.REPORT_ELAPSED_MEAN_MS;
                ReportLogCount = Const.REPORT_LOG_COUNT;
                ReportQueueMeanSize = Const.REPORT_QUEUE_MEAN_SIZE;
                ReportQueueStdDev = Const.REPORT_QUEUE_STD_DEV;
            }
        }
        
        public class SlackConfig
        {
            /// <summary> 보내는 사람(username) 앞에 붙을 접두사 </summary>
            [JsonProperty] public String NamePrefix { get; internal set; }

            /// <summary> 슬랙 WebhookUrl </summary>
            [JsonProperty] public String WebhookUrl { get; internal set; }

            /// <summary> Debug 수신 채널 </summary>
            [JsonProperty] public String DebugChannel { get; internal set; }

            /// <summary> Info 수신 채널 </summary>
            [JsonProperty] public String InfoChannel { get; internal set; }

            /// <summary> Warn 수신 채널 </summary>
            [JsonProperty] public String WarnChannel { get; internal set; }

            /// <summary> Error 수신 채널 </summary>
            [JsonProperty] public String ErrorChannel { get; internal set; }

            /// <summary> Fatal 수신 채널 </summary>
            [JsonProperty] public String FatalChannel { get; internal set; }

            /// <summary> :이름: 과 같은 IconEmoji </summary>
            [JsonProperty] public String IconEmoji { get; internal set; }

            /// <summary> 표기 시간(UTC)에 더할 시간(한국이면 +9). 기본값은 <see cref="Const.SLACK_UTC_ADD_HOUR"/> </summary>
            [JsonProperty] public Int32 UTCAddHour { get; internal set; }

            /// <summary> 메시지의 순서 보장(100% 보장은 안됨). 보장(1), 미보장(0) 기본값은 <see cref="Const.REPORT_TRY_ORDERING_MESSAGE"/> </summary>
            [JsonProperty] public Int32 TryOrderingMessage { get; internal set; }

            public SlackConfig()
            {
                IconEmoji = null;
                UTCAddHour = Const.SLACK_UTC_ADD_HOUR;
                TryOrderingMessage = Const.REPORT_TRY_ORDERING_MESSAGE;
            }
        }

        #region Not From File

        /// <summary> 빌드 AssemblyVersion. </summary>
        [JsonProperty] public String AssemblyVersion { get; internal set; }

        /// <summary> 빌드 당시의 git-hash. </summary>
        [JsonProperty] public String GitHash { get; internal set; }

        #endregion

        /// <summary> 식별자로 소문자로 강제 변환 </summary>
        [JsonProperty] public String ID { get; internal set; }

        /// <summary> 로그 처리 스레드 수. 기본값은 <see cref="Const.WORK_THREAD_COUNT"/> </summary>
        [JsonProperty] public Int32 WorkThreadCount { get; internal set; }

        /// <summary> 로그 처리(Tick) 동작 주기(ms). 기본값은 <see cref="Const.TICK_INTERVAL_MS"/> </summary>
        [JsonProperty] public Int32 TickIntervalMS { get; internal set; }

        /// <summary> 전송 완료 로그 처리 동작 주기(ms). 기본값은 <see cref="Const.COMPLETE_LOG_INTERVAL_MS"/> </summary>
        [JsonProperty] public Int32 CompletePutIntervalMS { get; internal set; }

        /// <summary> 최대 연속 오류 횟수로 도달 시 로거 일시정지. 기본값은 <see cref="Const.MAX_SERIAL_ERROR_COUNT"/> </summary>
        [JsonProperty] public Int32 MaxSerialErrorCount { get; internal set; }

        /// <summary> 최대 로그 큐(Queue) 크기로 도달 시, 로그를 추가(Push)할 수 없다. <see cref="UseThroughputControl"/> 이 켜져있다면 해당 큐의 크기도 포함한다. 기본값은 <see cref="Const.MAX_LOG_QUEUE_SIZE"/> </summary>
        [JsonProperty] public Int32 MaxLogQueueSize { get; internal set; }

        /// <summary> 단일 로그 최대 크기로 <see cref="Const.MAX_KINESIS_RECORD_BYTE"/> 보다 클 수 없다. 압축<see cref="CompressLogThresholdByte"/>) 을 사용하는 경우, 압축 이후의 크기를 사용한다. 기본값은 <see cref="Const.MAX_RECORD_BYTE"/> </summary>
        [JsonProperty] public Int32 MaxRecordByte { get; internal set; }

        /// <summary> 배치로 보낼 수 있는 최대 개수로 <see cref="Const.MAX_KINESIS_BATCH_SIZE"/> 보다 클 수 없다. 기본값은 <see cref="Const.MAX_BATCH_SIZE"/> </summary>
        [JsonProperty] public Int32 MaxBatchSize { get; internal set; }

        /// <summary> 배치로 보내기 위해 로그가 모이기를 대기하는 시간(ms). 너무 작으면 로그를 모아 보내지 못하게 된다. 기본값은 <see cref="Const.MAX_BATCH_WAIT_TIME_MS"/> </summary>
        [JsonProperty] public Int32 MaxBatchWaitTimeMS { get; internal set; }

        /// <summary> 최대 로그 재전송 횟수로 도달하면 로그를 버리고 통지. 기본값은 <see cref="Const.MAX_RETRY_SEND_COUNT"/> </summary>
        [JsonProperty] public Int32 MaxRetrySendCount { get; internal set; }

        /// <summary> 변환한 로그가 이 값보다 크면 압축(Byte). 기본값은 <see cref="Const.COMPRESS_LOG_THRESHOLD_BYTE"/> </summary>
        [JsonProperty] public Int32 CompressLogThresholdByte { get; internal set; }

        /// <summary> 샤드 개수에 따른 전송량 제어 기능 on(1), off(0). 꺼져 있으면 최대한 많이 보내려고 노력한다. 기본값은 <see cref="Const.USE_THROUGHPUT_CONTROL"/> </summary>
        [JsonProperty] public Int32 UseThroughputControl { get; internal set; }

        /// <summary> 로그 타입 별 푸시 무시 기능 on(1), off(0). 기본값은 <see cref="Const.USE_IGNORE_LOG_TYPES"/> </summary>
        [JsonProperty] public Int32 UseIgnoreLogType { get; internal set; }

        /// <summary> AWS 설정 </summary>
        [JsonProperty] public AWS AWSs { get; internal set; }

        /// <summary> <see cref="SlackWebhook"/> 설정 </summary>
        [JsonProperty] public SlackConfig SlackConfigs { get; internal set; }

        /// <summary> <see cref="Watcher"/>(모니터링) 설정 </summary>
        [JsonProperty] public Watcher Watchers { get; internal set; }

        /// <summary> 푸시를 무시할 로그 타입의 초깃값 </summary>
        [JsonProperty] public HashSet<String> IgnoreLogTypes { get; internal set; } = new HashSet<String>();

        /// <summary> 리포터 타입. 기본값은 <see cref="Const.REPORTER_TYPE"/> </summary>
        [JsonProperty] [JsonConverter(typeof(StringEnumConverter))] public ReporterType ReporterType { get; internal set; }

        /// <summary> 리포터 레벨. 기본값은 <see cref="Const.REPORT_LEVEL"/> </summary>
        [JsonProperty] [JsonConverter(typeof(StringEnumConverter))] public ReportLevelType ReportLevel { get; internal set; }

        /// <summary> <see cref="Config"/> 자신을 문자열로 한 줄 변환 </summary>
        [JsonIgnore] public String ConfigString { get; internal set; }

        /// <summary> <see cref="Config"/> 자신을 문자열로 보기좋게 변환 </summary>
        [JsonIgnore] public String ConfigStringPretty { get; internal set; }

        public Config()
        {
            // 설정 파일에 없는 항목은 기본값이 사용된다.
            WorkThreadCount = Const.WORK_THREAD_COUNT;
            TickIntervalMS = Const.TICK_INTERVAL_MS;
            CompletePutIntervalMS = Const.COMPLETE_LOG_INTERVAL_MS;
            MaxSerialErrorCount = Const.MAX_SERIAL_ERROR_COUNT;
            MaxLogQueueSize = Const.MAX_LOG_QUEUE_SIZE;
            MaxRecordByte = Const.MAX_RECORD_BYTE;
            MaxBatchSize = Const.MAX_BATCH_SIZE;
            MaxBatchWaitTimeMS = Const.MAX_BATCH_WAIT_TIME_MS;
            MaxRetrySendCount = Const.MAX_RETRY_SEND_COUNT;
            CompressLogThresholdByte = Const.COMPRESS_LOG_THRESHOLD_BYTE;
            Watchers = new Watcher();
            UseIgnoreLogType = Const.USE_IGNORE_LOG_TYPES;
            ReporterType = Const.REPORTER_TYPE;
            ReportLevel = Const.REPORT_LEVEL;
        }

        /// <summary>
        /// <see cref="Config"/> 인스턴스를 파일로부터 생성한다
        /// </summary>
        /// <param name="configPath"> 파일 경로 </param>
        /// <returns> 생성한 Config </returns>
        public static Config Create(String configPath)
        {
            using (StreamReader streamReader = File.OpenText(configPath))
            {
                String jsonString = streamReader.ReadToEnd();

                var config = JsonConvert.DeserializeObject<Config>(jsonString);

                config.Preprocess();

                return config;
            }
        }

        // 설정 값 조정.
        private void Preprocess()
        {
            DecryptAccessIDAndSecretKey();
            WriteBuildInfo();
            ToLowerIgnoreLogTypes();
            StoreConfigString();
            Assert();
        }

        // TODO: 추가 필요.
        private void Assert()
        {
            if (WorkThreadCount < 1)
            {
                throw new LoggerException($"Invalid Config: WorkThreadCount{WorkThreadCount}");
            }

            if (Const.MAX_KINESIS_BATCH_SIZE < MaxBatchSize)
            {
                throw new LoggerException($"Invalid Config: MAX_KINESIS_BATCH_SIZE({Const.MAX_KINESIS_BATCH_SIZE}) < MaxBatchSize({MaxBatchSize})");
            }

            if (Const.MAX_KINESIS_RECORD_BYTE < MaxRecordByte)
            {
                throw new LoggerException($"Invalid Config: MAX_KINESIS_RECORD_BYTE({Const.MAX_KINESIS_RECORD_BYTE}) < MaxRecordByte({MaxRecordByte})");
            }

            if (AWSs == null                                  ||
                String.IsNullOrEmpty(AWSs.DecryptedAccessID)  ||
                String.IsNullOrEmpty(AWSs.DecryptedSecretKey) ||
                String.IsNullOrEmpty(AWSs.KinesisStreamName)  ||
                String.IsNullOrEmpty(AWSs.Region))
            {
                throw new LoggerException("invalid config: invalid 'AWSs' property");
            }
        }

        private void DecryptAccessIDAndSecretKey()
        {
            AWSs.DecryptAccessIDAndSecretKey();
        }

        private void WriteBuildInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyVersion = assembly.GetName().Version.ToString();
            GitHash = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
        }

        private void ToLowerIgnoreLogTypes()
        {
            if (IgnoreLogTypes != null)
            {
                IgnoreLogTypes = new HashSet<String>(IgnoreLogTypes.ToList().ConvertAll(x => x.ToLower()));
            }
        }

        private void StoreConfigString()
        {
            const Int32 SHOW_LENGTH = 4;

            ConfigString = JsonConvert.SerializeObject(this);
            ConfigStringPretty = JsonConvert.SerializeObject(this, Formatting.Indented);

            String accessID = AWSs.DecryptedAccessID;
            String secretKey = AWSs.DecryptedSecretKey;

            String safeAccessID = accessID.HideString(SHOW_LENGTH, true);
            String safeSecretKey = secretKey.HideString(SHOW_LENGTH, true);

            // 원본 대신에 복호화한 것을 부분적으로 보여준다. 원본도 보여주려 했는데 너무 길어서 보기가 좋지 않음.
            ConfigString = ConfigString.Replace(AWSs.AccessID, $"{safeAccessID}").Replace(AWSs.SecretKey, $"{safeSecretKey}");
            ConfigStringPretty = ConfigStringPretty.Replace(AWSs.AccessID, $"{safeAccessID}").Replace(AWSs.SecretKey, $"{safeSecretKey}");
        }
    }
}
