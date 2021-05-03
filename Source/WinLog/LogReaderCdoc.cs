// /********************************************************
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
    using System.Dynamic;
    using System.Xml.Linq;
    using WinLog.Helpers;
    using WinLog.LogHelpers;

    /// <summary>
    ///     Creates enumerables, single event parsing, reading AP logs.
    ///     Used specifically for the Microsoft Cyber Defense Operations Center (CDOC).
    /// </summary>
    public class LogReaderCdoc
    {
        /// <summary>
        ///     Reads an EVTX file and returns an enumerable list of Winlog.LogRecord instances.
        /// </summary>
        /// <param name="fileName">string - the EVTX file name.</param>
        /// <returns>IEnumerable&lt;LogRecord&gt; - an enumerable list of WinLog.LogRecord instances.</returns>
        public static IEnumerable<LogRecord> ReadEvtxFile(string fileName)
        {
            long eventCount = 0; // for debugging

            using (var reader = new EventLogReader(fileName, PathType.FilePath))
            {
                for (;;)
                {
                    var record = reader.ReadEvent();
                    if (record == null)
                    {
                        yield break;
                    }

                    eventCount++;
                    dynamic evt = ParseEvent(record);
                    yield return new LogRecord(evt);
                }
            }
        }

        /// <summary>
        ///     Reads a Windows Event Log and returns an enumerable list of Winlog.LogRecord instances.
        /// </summary>
        /// <param name="fileName">string - name of the Windows Event Log.</param>
        /// <returns>IEnumerable&lt;LogRecord&gt; - an enumerable list of WinLog.LogRecord instances.</returns>
        public static IEnumerable<LogRecord> ReadWindowsLog(string logName)
        {
            var log = EvtxEnumerable.ReadWindowsLog(logName, null);
            foreach (var e in log)
            {
                var evt = LogReader.ParseEvent(e);
                yield return new LogRecord(evt);
            }
        }

        /// <summary>
        ///     Reads a Windows Event Log and returns an enumerable list of Winlog.LogRecord instances.
        ///     Starts reading the event log at the specified event bookmark.
        /// </summary>
        /// <param name="fileName">string - name of the Windows Event Log.</param>
        /// <returns>IEnumerable&lt;LogRecord&gt; - an enumerable list of WinLog.LogRecord instances.</returns>
        public static IEnumerable<LogRecord> ReadWindowsLog(string logName, EventBookmark bookmark)
        {
            var log = EvtxEnumerable.ReadWindowsLog(logName, bookmark);
            foreach (var e in log)
            {
                var evt = LogReader.ParseEvent(e);
                yield return new LogRecord(evt, e.Bookmark);
            }
        }

        /// <summary>
        ///     Parses a single event into dynamic object type, from the EventRecord instance of the Windows event.
        /// </summary>
        /// <param name="eventRecord">EventRecord - the EventRecord object.</param>
        /// <returns>dynamic - the event record as a dynamic object.</returns>
        public static dynamic ParseEvent(EventRecord eventRecord)
        {
            try
            {
                dynamic evt = ParseEvent(eventRecord.ToXml());

                evt.Bookmark = eventRecord.Bookmark;

                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Parses a single event into dynamic object type, from the XML of the Windows event.
        /// </summary>
        /// <param name="eventXml">string - the XML string of an EventRecord object.</param>
        /// <returns>dynamic - the event record as a dynamic object.</returns>
        public static dynamic ParseEvent(string eventXml)
        {
            try
            {
                var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);
                var xe = XElement.Parse(sanitizedXmlString);

                var eventDataProperties = new ExpandoObject();

                var systemData = xe.Element(ElementNames.System);
                dynamic evt = CommonXmlFunctions.ParseSystemData(systemData);

                var eventData = xe.Element(ElementNames.EventData);
                var userData = xe.Element(ElementNames.UserData);

                // Convert the EventData to named properties
                if (eventData != null)
                {
                    eventDataProperties = CommonXmlFunctions.ParseEventData(eventData);
                    evt.EventData = eventDataProperties;
                }

                // Convert the EventData to named properties
                if (userData != null)
                {
                    evt.EventData = CommonXmlFunctions.ParseUserData(userData);
                }

                return evt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}