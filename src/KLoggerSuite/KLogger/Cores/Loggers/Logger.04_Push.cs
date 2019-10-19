using System;
using KLogger.Cores.Components;
using KLogger.Libs;
using KLogger.Types;
using Newtonsoft.Json.Linq;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 Push.
    /// </summary>
    internal partial class Logger
    {
        private const Int32 INVALID_SEQUENCE = -1;

        internal Int64 Push(String type, Object log)
        {
            return PushImpl(type, log, true);
        }

        internal Int64 PushJsonString(String type, String jsonString)
        {
            if (IsValidJsonString(jsonString) == false)
            {
                return INVALID_SEQUENCE;
            }

            return PushImpl(type, jsonString, false);
        }

        // 락이 없는 함수로 푸시 말고는 쓰기 동작이 없어야 한다.
        // Stop이 불리고 나서도 로그가 푸시될 수 있는 것을 감안한다(이런 로그는 Stop에서 처리).
        private Int64 PushImpl(String type, Object log, Boolean doSerialize)
        {
            try
            {
                if (CheckAvailablePush(type, log) == false)
                {
                    return INVALID_SEQUENCE;
                }

                DateTime now = Now.NowDateTime();
                Int64 sequence = _sequenceGenerator.Generate();
                Boolean successPush = _logQueue.Push(new Log()
                                                     {
                                                         Sequence = sequence,
                                                         LogType = type,
                                                         LogObject = log,
                                                         TimeStamp = Now.TimestampSec(now),
                                                         TimeStampNS = Now.TimestampNS(now),
                                                         IsRawString = doSerialize
                                                     });

                if (successPush)
                {
                    Watcher.IncrementPushLogCount();
                    return sequence;
                }

                return INVALID_SEQUENCE;
            }
            catch (Exception exception)
            {
                ErrorCounter.RaiseError($"Fail Push: {exception.Message}");
                return INVALID_SEQUENCE;
            }
        }

        private Boolean CheckAvailablePush(String type, Object log)
        {
            if (type == null || log == null)
            {
                return false;
            }

            if (State != StateType.Start)
            {
                DebugLog.Log($"Fail Push. state: {State.ToString()}", "klogger:push");
                return false;
            }

            if (UseIgnoreLogType && IgnoreLogTypes.Contains(type.ToLower()))
            {
                DebugLog.Log($"Ignore LogType! ({type})", "klogger:push");
                return false;
            }

            Int32 logCountInQueue = _logQueue.Count;
            if (Config.UseThroughputControl == 1)
            {
                logCountInQueue += ThroughputController.LogCountInQueue;
            }

            if (Config.MaxLogQueueSize <= logCountInQueue)
            {
                DebugLog.Log($"Fail Push. {Config.MaxLogQueueSize.ToString()} <= {_logQueue.Count.ToString()}", "klogger:push");
                return false;
            }

            return true;
        }

        private Boolean IsValidJsonString(String jsonString)
        {
            try
            {
                JToken.Parse(jsonString);
                return true;
            }
            catch (Exception exception)
            {
                DebugLog.Log($"Fail IsValidJsonString: {exception.Message}");
                return false;
            }
        }
    }
}
