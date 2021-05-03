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
    using Newtonsoft.Json;
    using WinLog.Helpers;
    using WinLog.LogHelpers;

    /// <summary>
    ///     Creates IEnumerables, single event parsing and reading AP logs
    /// </summary>
    public class LogReader
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
        /// Reads the specified Windows Event Log and yields a stream of WinLog.LogRecord instances.
        /// </summary>
        /// <param name="logName">string - the name of the Windows Event Log  to read.</param>
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
        /// Reads the specified Windows Event Log and yields a stream of WinLog.LogRecord instances.
        /// Starts at the specified event bookmark.
        /// </summary>
        /// <param name="logName">string - the name of the Windows Event Log  to read.</param>
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
        ///     Parse a single event into a dynamic object type, from the XML of the Windows Event.
        /// </summary>
        /// <param name="eventRecord">EventRecord - the System.Diagnostics.Eventing.Reader.EventRecord object.</param>
        /// <returns>dynamic - the specified event as a dynamic object.</returns>
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
        ///     Parses a single event into dynamic object type, from the XML of the Windows Event.
        /// </summary>
        /// <param name="eventXml">string - the XML string of an EventRecord object</param>
        /// <returns>dynamic - the specified event as a dynamic object.</returns>
        public static dynamic ParseEvent(string eventXml)
        {
            try
            {
                var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);
                var xe = XElement.Parse(sanitizedXmlString);

                var systemData = xe.Element(ElementNames.System);
                dynamic evt = CommonXmlFunctions.ParseSystemData(systemData);

                var eventData = xe.Element(ElementNames.EventData);
                var userData = xe.Element(ElementNames.UserData);

                // Convert the EventData to named properties
                if (eventData != null)
                {
                    var eventDataProperties = CommonXmlFunctions.ParseEventData(eventData);
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

        /// <summary>
        ///     Parses a single event, from an EventRecord instance, and returns the event extended data as a dynamic object.
        /// </summary>
        /// <param name="eventRecord">EventRecord - the System.Diagnostics.Eventing.Reader.EventRecord object.</param>
        /// <returns>dynamic - the specified event's extended data as a dynamic object.</returns>
        public static dynamic RetrieveExtendedData(EventRecord eventRecord)
        {
            try
            {
                return RetrieveExtendedData(eventRecord.ToXml());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Parses a single event, from the XML string of an Event, and returns the event extended data as a string.
        /// </summary>
        /// <param name="eventXml">string - the XML string of an EventRecord object.</param>
        /// <returns>string - the event extended data as a string.</returns>
        public static string RetrieveExtendedData(string eventXml)
        {
            try
            {
                var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);
                var xe = XElement.Parse(sanitizedXmlString);
                var eventData = xe.Element(ElementNames.EventData);
                var userData = xe.Element(ElementNames.UserData);

                // Convert the EventData string
                if (eventData != null)
                {
                    return eventData.ToString();
                }

                // Return the UserData string
                if (userData != null)
                {
                    return userData.ToString();
                }

                // If the event has neither EventData or UserData, return null...  
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Parses a single event, from the XML string of an Event, and returns the event extended data as a dynamic object.
        /// </summary>
        /// <param name="eventXml">string - the XML string of an EventRecord object.</param>
        /// <returns>dynamic - the specified event's extended data as a dynamic object.</returns>
        public static dynamic ParseEventDataIntoDynamic(string eventXml)
        {
            dynamic evtData = new ExpandoObject();

            var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);
            var xe = XElement.Parse(sanitizedXmlString);

            var eventData = xe.Element(ElementNames.EventData);
            var userData = xe.Element(ElementNames.UserData);

            // Convert the EventData to named properties
            if (eventData != null)
            {
                var eventDataProperties = CommonXmlFunctions.ParseEventData(eventData);
                evtData = eventDataProperties;
            }

            // Convert the EventData to named properties
            if (userData != null)
            {
                evtData = CommonXmlFunctions.ParseUserData(userData);
            }

            return evtData;
        }

        /// <summary>
        ///     Parses the XML string of an Event into JSON, from the XML of the windows event
        /// </summary>
        /// <param name="eventXml">string - the XML string of an EventRecord object.</param>
        /// <returns>string - the string representation of a JSON object containing the EventData values.</returns>
        public static string ParseEventDataIntoJson(string eventXml)
        {
            dynamic evtData = new ExpandoObject();

            var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);
            var xe = XElement.Parse(sanitizedXmlString);

            var eventData = xe.Element(ElementNames.EventData);
            var userData = xe.Element(ElementNames.UserData);

            // Convert the EventData to named properties
            if (eventData != null)
            {
                var eventDataProperties = CommonXmlFunctions.ParseEventData(eventData);
                evtData = eventDataProperties;
            }

            // Convert the EventData to named properties
            if (userData != null)
            {
                evtData = CommonXmlFunctions.ParseUserData(userData);
            }

            string json = JsonConvert.SerializeObject(evtData, Formatting.Indented);

            return json;
        }
    }
}