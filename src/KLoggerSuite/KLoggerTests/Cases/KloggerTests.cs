using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using KLogger;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public abstract class KLoggerTests
    {
        protected const String CONFIG_PATH = @"..\\..\\KLoggerConfigTest.json";

        protected ConcurrentDictionary<Int64, Object> _sequenceToLogObject = new ConcurrentDictionary<Int64, Object>();
        protected ManualResetEventSlim _completeTestEvent = new ManualResetEventSlim(false);

        protected KLoggerAPI _kLoggerAPI;
        protected Config _kLoggerConfig;
        
        protected abstract CompletePutNotifyType CompletePutNotifyType { get; }

        protected virtual void OnCompletePut(IEnumerable<CompletePut> completePuts)
        {
            foreach (CompletePut completePut in completePuts)
            {
                Console.WriteLine($"{nameof(OnCompletePut)}({completePut.CompletePutType}): {completePut.Logs.Length}");

                if (completePut.CompletePutType != CompletePutType.Success)
                {
                    Console.WriteLine("성공하지 못한 로그");
                    Assert.Fail("성공하지 못한 로그");
                }

                foreach (ILog log in completePut.Logs)
                {
                    if (_sequenceToLogObject.TryRemove(log.Sequence, out Object originLogObject) == false)
                    {
                        Console.WriteLine("보낸 목록에서 찾을 수 없는 로그");
                        Assert.Fail("보낸 목록에서 찾을 수 없는 로그");
                    }

                    Object logObject = log.LogObject;
                    if (log.LogObject is StringLogObject logLogObject)
                    {
                        logObject = logLogObject.raw;
                    }

                    if (logObject != originLogObject)
                    {
                        Console.WriteLine("보냈던 로그와 다른 로그");
                        Assert.Fail("보냈던 로그와 다른 로그");
                    }
                }
            }

            if (_sequenceToLogObject.IsEmpty)
            {
                _completeTestEvent.Set();
            }
        }

        protected virtual Config OverwriteConfig()
        {
            _kLoggerConfig = Config.Create(CONFIG_PATH);
            _kLoggerConfig.ReporterType = ReporterType.Console;
            //_kLoggerConfig.ReporterType = ReporterType.Slack;
            return _kLoggerConfig;
        }

        protected void AddLogIfFailAssert(Object logObject, String logType = null)
        {
            Int64 sequence = _kLoggerAPI.Push(logType ?? "test", logObject);
            if (sequence < 0)
            {
                Console.WriteLine("푸시 실패");
            }

            Assert.IsTrue(0 < sequence);
            
            Boolean addResult = _sequenceToLogObject.TryAdd(sequence, logObject);
            if (addResult == false)
            {
                Console.WriteLine("로그 추가 실패");
            }

            Assert.IsTrue(addResult);
        }

        protected void WaitForCompleteTest(Int32 maxWaitMS, Int32 successLogCount, Int32 failLogCount)
        {
            Boolean isTimeout = _completeTestEvent.Wait(maxWaitMS);
            Assert.IsTrue(isTimeout);
            Assert.IsTrue(_kLoggerAPI.SuccessLogCount == successLogCount);
            Assert.IsTrue(_kLoggerAPI.FailLogCount == failLogCount);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _kLoggerAPI = new KLoggerAPI(OverwriteConfig(), OnCompletePut, CompletePutNotifyType);
            _kLoggerAPI.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _kLoggerAPI.Stop();
        }
    }
}
