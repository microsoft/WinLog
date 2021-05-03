﻿// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *   Licensed under the MIT license.                     *
// *                                                       *
// ********************************************************/

namespace WinLog
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Threading;
    using WinLog.LogHelpers;

    /// <summary>
    /// Class that defines WinLog.LogRecords as an IObservable and provides functionality to monitor a Windows Event Log for new events.
    /// </summary>
    public class EventLogObservable : IObservable<LogRecord>, IDisposable
    {
        private readonly Subject<LogRecord> _subject = new Subject<LogRecord>();

        /// <summary>
        /// Watches the specified Windows Event Log for new events.
        /// </summary>
        /// <param name="logname">string - the name / path of the Windows Event Log to monitor for new events.</param>
        public void MonitorLog(string logname)
        {
            var securityLog = EvtxEnumerable.ReadWindowsLog(logname, null);

            // Read all the events that existed when the program was started
            EventRecord last = null;
            foreach (var e in securityLog)
            {
                last = e;
            }

            // Read only new security events each second
            while (true)
            {
                Thread.Sleep(1000);

                var newEvents = EvtxEnumerable.ReadWindowsLog(logname, last.Bookmark);

                foreach (var e in newEvents)
                {
                    last = e;
                    var ev = LogReader.ParseEvent(e.ToXml());
                    _subject.OnNext(ev);
                }
            }
        }

        /// <summary>
        /// Notifies the publisher that an observer is ready to receive notification.
        /// </summary>
        /// <param name="observer">IObserver - the observer that will receive notifications.</param>
        /// <returns></returns>
        public IDisposable Subscribe(IObserver<LogRecord> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Dispose()
        {
            _subject.Dispose();
        }
    }

    /// <summary>
    ///     Mock up of the Subject in Rx
    ///     Note the operators in Rx don't include subjects nowadays.
    ///     Using subject in each operator is how Rx worked in v1. In v2.0 these were removed for better performance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Subject<T> : IObserver<T>, IObservable<T>, IDisposable
    {
        private List<Subscription> _subscriptions = new List<Subscription>();

        public void OnNext(T value)
        {
            foreach (var s in _subscriptions)
            {
                s.Subscriber.OnNext(value);
            }
        }

        public void OnCompleted()
        {
            foreach (var s in _subscriptions)
            {
                s.Subscriber.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            foreach (var s in _subscriptions)
            {
                s.Subscriber.OnError(error);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var s = new Subscription()
            {
                Parent = this,
                Subscriber = observer
            };
            _subscriptions.Add(s);
            return s;
        }

        public void Unsubscribe(Subscription s)
        {
            _subscriptions.Remove(s);
        }

        public void Dispose()
        {
            var list = _subscriptions;
            _subscriptions = new List<Subscription>();

            foreach (var sub in list)
            {
                sub.Dispose();
            }
        }

        public class Subscription : IDisposable
        {
            public IObserver<T> Subscriber;
            public Subject<T> Parent;

            public void Dispose()
            {
                Parent.Unsubscribe(this);
            }
        }
    }
}