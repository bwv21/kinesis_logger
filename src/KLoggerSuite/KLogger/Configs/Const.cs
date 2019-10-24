using System;
using KLogger.Types;

namespace KLogger.Configs
{
    /// <summary> 로거의 상수를 정의. </summary>
    public class Const
    {
        /// <summary> 레코드(행) 즉, 하나의 로그를 구별하는 식별자. </summary>
        public const Char LOG_DELIMITER = '\n'; // 람다와도 연동되므로 수정해서는 안된다(시스템 고정값).

        // https://docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html
        // https: //aws.amazon.com/ko/kinesis/data-streams/pricing
        #region AWS 제약이므로 스펙 변경을 공식적으로 발표하면 수정한다

        /// <summary> Kinesis에 배치로 보낼 수 있는 최대 개수. 500(AWS 제약). </summary>
        public const Int32 MAX_KINESIS_BATCH_SIZE = 500;

        /// <summary> Kinesis 단일 레코드 최대 크기. 1MB(AWS 제약). </summary>
        public const Int32 MAX_KINESIS_RECORD_BYTE = 1048576;

        /// <summary> 1초 동안 샤드당 받을 수 있는 바이트. 1MB(AWS 제약). </summary>
        public const Int32 BYTE_FOR_SECOND_PER_SHARD_BYTE = 1048576;

        /// <summary> 1초 동안 샤드당 받을 수 있는 레코드. 1000(AWS 제약). </summary>
        public const Int32 RECORD_FOR_SECOND_PER_SHARD_COUNT = 1000;

        #endregion

        #region Default Config

        /// <summary> <see cref="Config.WorkThreadCount"/> 의 기본값. </summary>
        public const Int32 WORK_THREAD_COUNT = 2;

        /// <summary> <see cref="Config.WorkThreadIntervalMS"/> 의 기본값. </summary>
        public const Int32 WORK_THREAD_INTERVAL_MS = 33;

        /// <summary> <see cref="Config.CompletePutIntervalMS"/> 의 기본값. </summary>
        public const Int32 COMPLETE_LOG_INTERVAL_MS = 200;

        /// <summary> <see cref="Config.MaxSerialErrorCount"/> 의 기본값. </summary>
        public const Int32 MAX_SERIAL_ERROR_COUNT = 100;

        /// <summary> <see cref="Config.MaxLogQueueSize"/> 의 기본값. </summary>
        public const Int32 MAX_LOG_QUEUE_SIZE = 5000;

        /// <summary> <see cref="Config.MaxRecordByte"/> 의 기본값. </summary>
        public const Int32 MAX_RECORD_BYTE = MAX_KINESIS_RECORD_BYTE;

        /// <summary> <see cref="Config.MaxBatchSize"/> 의 기본값. </summary>
        public const Int32 MAX_BATCH_SIZE = MAX_KINESIS_BATCH_SIZE;

        /// <summary> <see cref="Config.MaxBatchWaitTimeMS"/> 의 기본값. </summary>
        public const Int32 MAX_BATCH_WAIT_TIME_MS = 10000;

        /// <summary> <see cref="Config.MaxRetrySendCount"/> 의 기본값. </summary>
        public const Int32 MAX_RETRY_SEND_COUNT = 10;

        /// <summary> <see cref="Config.CompressLogThresholdByte"/> 의 기본값. </summary>
        public const Int32 COMPRESS_LOG_THRESHOLD_BYTE = 1024;

        /// <summary> <see cref="Config.CompressLogThresholdByte"/> 의 기본값. </summary>
        public const Int32 USE_IGNORE_LOG_TYPES = 1;

        /// <summary> <see cref="Config.UseThroughputControl"/> 의 기본값. </summary>
        public const Int32 USE_THROUGHPUT_CONTROL = 1;

        /// <summary> <see cref="Config.Watcher.IntervalMS"/> 의 기본값. </summary>
        public const Int32 WATCHER_INTERVAL_MS = 10000;
        
        /// <summary> <see cref="Config.Watcher.ReportElapsedMeanMS"/> 의 기본값. </summary>
        public const Int32 REPORT_ELAPSED_MEAN_MS = 30;

        /// <summary> <see cref="Config.Watcher.ReportLogCount"/> 의 기본값. </summary>
        public const Int32 REPORT_LOG_COUNT = 500;

        /// <summary> <see cref="Config.Watcher.ReportQueueMeanSize"/> 의 기본값. </summary>
        public const Int32 REPORT_QUEUE_MEAN_SIZE = 300;

        /// <summary> <see cref="Config.Watcher.ReportQueueStdDev"/> 의 기본값. </summary>
        public const Int32 REPORT_QUEUE_STD_DEV = 100;

        /// <summary> <see cref="Config.SlackConfig.TryOrderingReport"/> 의 기본값. </summary>
        public const Int32 SLACK_REPORTER_TRY_ORDERING_REPORT = 1;

        /// <summary> <see cref="Config.ReportLevel"/> 의 기본값. </summary>
        public const ReportLevelType REPORT_LEVEL = ReportLevelType.Warn;

        /// <summary> <see cref="Config.ReporterType"/> 의 기본값. </summary>
        public const ReporterType REPORTER_TYPE = ReporterType.None;

        /// <summary> <see cref="Config.SlackConfig.UTCAddHour"/> 의 기본값. </summary>
        public const Int32 SLACK_UTC_ADD_HOUR = 9;

        #endregion
    }
}
