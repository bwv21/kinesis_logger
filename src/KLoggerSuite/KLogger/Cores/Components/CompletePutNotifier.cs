using System;
using System.Collections.Generic;
using System.Threading;
using KLogger.Configs;
using KLogger.Cores.Exceptions;
using KLogger.Cores.Loggers;
using KLogger.Libs;
using KLogger.Types;

namespace KLogger.Cores.Components
{
    internal class CompletePutNotifier
    {
        private readonly Object _lock = new Object();

        private Config _config;
        private ErrorCounter _errorCounter;
        private CompletePutNotifyType _completePutNotifyType;
        private CompletePutDelegate _noticeCompletePut;

        private QueueMT<CompletePut> _completePuts;
        private NaiveLoopThread _thread;
        
        internal void Initialize(Logger logger, CompletePutDelegate noticeCompletePut)
        {
            if (noticeCompletePut == null || logger.CompletePutNotifyType == CompletePutNotifyType.None)
            {
                throw new LoggerException($"Fail {nameof(CompletePutNotifier)}::{nameof(Initialize)}");
            }

            _config = logger.Config;
            _errorCounter = logger.ErrorCounter;
            _completePutNotifyType = logger.CompletePutNotifyType;
            _noticeCompletePut = noticeCompletePut;
        }

        internal Boolean Push(ILog[] logs, CompletePutType completePutType)
        {
            lock (_lock)
            {
                if (_thread == null)
                {
                    return false;
                }
            }

            return _completePuts.Push(new CompletePut(logs, completePutType));
        }

        internal void Start()
        {
            lock (_lock)
            {
                if (_thread != null)
                {
                    throw new LoggerException($"Fail {nameof(CompletePutNotifier)}::{nameof(Start)} ${nameof(_thread)} is not null");
                }

                _completePuts = new QueueMT<CompletePut>();

                _thread = new NaiveLoopThread(HandleCompletePut, _config.CompletePutIntervalMS, e => _errorCounter.RaiseError(e), nameof(CompletePutNotifier));
                _thread.Start();
            }
        }

        internal void Stop()
        {
            lock (_lock)
            {
                if (_thread == null)
                {
                    throw new LoggerException($"Fail {nameof(CompletePutNotifier)}::{nameof(Stop)}");
                }

                _thread.Stop();
                _thread = null;
            }

            // Stop이후에 미묘한 타이밍에 Push된 것은 손실될 수 있음을 감안한다.
            while (_completePuts.IsEmpty() == false)
            {
                PopAndNotice();
            }
        }

        private void HandleCompletePut()
        {
            if (_completePuts.Count <= 0)
            {
                return;
            }

            lock (_lock)
            {
                if (_thread == null)
                {
                    return;
                }
            }

            PopAndNotice();
        }

        private void PopAndNotice()
        {
            var completePuts = new List<CompletePut>(_completePuts.Count);

            while (_completePuts.IsEmpty() == false)
            {
                CompletePut completePut = _completePuts.Pop();
                if (completePut.CompletePutType == CompletePutType.Success)
                {
                    if (_completePutNotifyType == CompletePutNotifyType.Both ||
                        _completePutNotifyType == CompletePutNotifyType.SuccessOnly)
                    {
                        completePuts.Add(completePut);
                    }
                }
                else
                {
                    if (_completePutNotifyType == CompletePutNotifyType.Both ||
                        _completePutNotifyType == CompletePutNotifyType.FailOnly)
                    {
                        completePuts.Add(completePut);
                    }
                }
            }

            if (0 < completePuts.Count)
            {
                // 알림을 받는 쪽과 이곳의 분리를 위해 스레드 풀을 이용한다.
                ThreadPool.QueueUserWorkItem(_ => _noticeCompletePut?.Invoke(completePuts));
            }
        }
    }
}
