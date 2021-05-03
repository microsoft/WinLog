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
    using System.Diagnostics;
    using System.Diagnostics.Eventing.Reader;
    using System.Linq;
    using System.Xml.Linq;
    using Newtonsoft.Json;
    using WinLog.Helpers;

    /// <summary>
    ///     The Event Record Conversion class
    /// </summary>
    public class EventRecordConversion : IDisposable
    {
        private readonly ProviderStringCache providerStringCache = new ProviderStringCache();

        public void Dispose()
        {
            // No actions to dispose of, yet...  Implemented to support the dispose pattern
        }

        /// <summary>
        ///     Converts a Windows EventRecord into a JsonLogRecord, used to insert to Kusto
        /// </summary>
        /// <param name="eventRecord">the EventRecord object</param>
        /// <returns></returns>
        public LogRecordCdoc ToLogRecordCdoc(EventRecord eventRecord)
        {
            if (eventRecord == null)
            {
                throw new ArgumentNullException(nameof(eventRecord));
            }

            string level;
            string task;
            string opCode;
            string keywords;
            providerStringCache.Lookup(eventRecord, out level, out task, out opCode, out keywords);

            LogRecord logRecord = ToLogRecord(
                eventRecord.ToXml(),
                eventRecord.Bookmark,
                level,
                task,
                opCode,
                eventRecord.ProcessId ?? 0,
                eventRecord.ThreadId ?? 0,
                keywords);

            return new LogRecordCdoc
            {
                EventRecordId = logRecord.EventRecordId,
                TimeCreated = logRecord.TimeCreated,
                Computer = logRecord.Computer,
                ProcessId = logRecord.ProcessId,
                ThreadId = logRecord.ThreadId,
                Provider = logRecord.Provider,
                EventId = logRecord.EventId,
                Level = logRecord.Level,
                Version = logRecord.Version,
                Channel = logRecord.Channel,
                Task = logRecord.Task,
                Opcode = logRecord.Opcode,
                Security = logRecord.Security,
                EventData = logRecord.EventData,
                LogFileLineage = logRecord.LogFileLineage
            };
        }

        /// <summary>
        ///     Converts a Windows EventRecord into a JsonLogRecordEx, containing an Extended field for use, used to insert to
        ///     Kusto
        /// </summary>
        /// <param name="eventXml"></param>
        /// <param name="eventBookmark"></param>
        /// <returns></returns>
        public LogRecordEx ToLogRecordEx(string eventXml,
            EventBookmark eventBookmark = null)
        {
            LogRecord logRecordCdoc = ToLogRecord(eventXml, eventBookmark);

            return new LogRecordEx
            {
                EventRecordId = logRecordCdoc.EventRecordId,
                TimeCreated = logRecordCdoc.TimeCreated,
                Computer = logRecordCdoc.Computer,
                ProcessId = logRecordCdoc.ProcessId,
                ThreadId = logRecordCdoc.ThreadId,
                Provider = logRecordCdoc.Provider,
                EventId = logRecordCdoc.EventId,
                Level = logRecordCdoc.Level,
                Version = logRecordCdoc.Version,
                Channel = logRecordCdoc.Channel,
                Task = logRecordCdoc.Task,
                Opcode = logRecordCdoc.Opcode,
                Security = logRecordCdoc.Security,
                EventData = logRecordCdoc.EventData,
                LogFileLineage = logRecordCdoc.LogFileLineage
            };
        }

        /// <summary>
        ///     Converts a Windows EventRecord into a JsonLogRecord, used to insert to Kusto
        /// </summary>
        /// <param name="eventRecord">the EventRecord object</param>
        /// <returns></returns>
        public IDictionary<string, object> ToLogRecordRaw(EventRecord eventRecord)
        {
            if (eventRecord == null)
            {
                throw new ArgumentNullException(nameof(eventRecord));
            }

            string level;
            string task;
            string opCode;
            string keywords;
            providerStringCache.Lookup(eventRecord, out level, out task, out opCode, out keywords);

            LogRecord logRecordCdoc = ToLogRecord(
                eventRecord.ToXml(),
                eventRecord.Bookmark,
                level,
                task,
                opCode,
                eventRecord.ProcessId ?? 0,
                eventRecord.ThreadId ?? 0,
                keywords,
                true);

            return GetLogRecordRawObject(logRecordCdoc);
        }

        /// <summary>
        ///     Converts a Windows EventRecord into a JsonLogRecordEx, containing an Extended field for use, used to insert to
        ///     Kusto
        /// </summary>
        /// <param name="eventXml"></param>
        /// <param name="eventBookmark"></param>
        /// <returns></returns>
        public IDictionary<string, object> ToLogRecordRaw(string eventXml,
            EventBookmark eventBookmark = null)
        {
            LogRecord logRecord = ToLogRecord(eventXml, eventBookmark, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, true);

            return GetLogRecordRawObject(logRecord);
        }

        /// <summary>
        ///     Common method return a LogRecordRaw object from a LogRecordCDOC object, essentially without a LogFileLineage field
        ///     while using the exact same methodology for parsing as all other parsing.
        /// </summary>
        /// <param name="logRecord"></param>
        /// <returns></returns>
        private IDictionary<string, object> GetLogRecordRawObject(LogRecord logRecord)
        {
            var instance = new LogRecordRaw
            {
                EventRecordId = logRecord.EventRecordId,
                TimeCreated = logRecord.TimeCreated,
                Computer = logRecord.Computer,
                ProcessId = logRecord.ProcessId,
                ThreadId = logRecord.ThreadId,
                Provider = logRecord.Provider,
                EventId = logRecord.EventId,
                Level = logRecord.Level,
                Version = logRecord.Version,
                Channel = logRecord.Channel,
                Task = logRecord.Task,
                Opcode = logRecord.Opcode,
                EventData = logRecord.EventData,
                Security = logRecord.Security,
                Keywords = logRecord.Keywords,
                Correlation = logRecord.Correlation
            };

            return instance.ToDictionary(instance);
        }

        /// <summary>
        ///     Creates a JsonLogRecord object
        /// </summary>
        /// <param name="eventXml"></param>
        /// <param name="eventBookmark"></param>
        /// <param name="level"></param>
        /// <param name="task"></param>
        /// <param name="opCode"></param>
        /// <param name="processId"></param>
        /// <param name="threadId"></param>
        /// <param name="returnEventDataDictionary"></param>
        /// <returns></returns>
        public LogRecord ToLogRecord(
            string eventXml,
            EventBookmark eventBookmark,
            string level = "",
            string task = "",
            string opCode = "",
            int processId = 0,
            int threadId = 0,
            string keywords = "",
        bool returnEventDataDictionary = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventXml))
                {
                    throw new ArgumentNullException(nameof(eventXml));
                }

                var sanitizedXmlString = XmlVerification.VerifyAndRepairXml(eventXml);

                var xe = XElement.Parse(sanitizedXmlString);
                var eventData = xe.Element(ElementNames.EventData);
                var userData = xe.Element(ElementNames.UserData);

                var header = xe.Element(ElementNames.System);
                var recordId = long.Parse(header.Element(ElementNames.EventRecordId).Value);

                var systemPropertiesDictionary = CommonXmlFunctions.ConvertSystemPropertiesToDictionary(xe);

                var namedProperties = new Dictionary<string, string>();
                var dataWithoutNames = new List<string>();

                // Convert the EventData to named properties
                if (userData != null)
                {
                    namedProperties = CommonXmlFunctions.ParseUserData(userData).ToDictionary(x => x.Key, x => x.Value.ToString());
                }

                if (eventData != null)
                {
                    var eventDataProperties = CommonXmlFunctions.ParseEventData(eventData);
                    namedProperties = eventDataProperties.ToDictionary(x => x.Key, x => x.Value.ToString());
                }

                string json;
                if (dataWithoutNames.Count > 0)
                {
                    if (namedProperties.Count > 0)
                    {
                        throw new Exception("Event that has both unnamed and named data?");
                    }

                    json = JsonConvert.SerializeObject(dataWithoutNames, Formatting.Indented);
                }
                else
                {
                    json = JsonConvert.SerializeObject(namedProperties, Formatting.Indented);
                }

                var collectorTimestamp = DateTime.UtcNow;
                var logFileLineage = new LogFileLineage
                {
                    LogFileId = 0,
                    UploadMachine = Environment.MachineName,
                    Seq = 1,
                    CollectorTimeStamp = collectorTimestamp,
                    CollectorUnixTimeStamp = collectorTimestamp.GetUnixTime()
                };

                string[] executionProcessThread;
                if (systemPropertiesDictionary.ContainsKey("Execution"))
                {
                    executionProcessThread = systemPropertiesDictionary["Execution"].ToString()
                        .Split(new[]
                        {
                            ':'
                        }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    executionProcessThread = new string[]
                    {
                        "0",
                        "0"
                    };
                }

                var serializedLogFileLineage = JsonConvert.SerializeObject(logFileLineage);

                return new LogRecord()
                {
                    EventRecordId = Convert.ToInt64(systemPropertiesDictionary["EventRecordID"]),
                    TimeCreated = Convert.ToDateTime(systemPropertiesDictionary["TimeCreated"]),
                    Computer = systemPropertiesDictionary["Computer"].ToString(),
                    ProcessId = processId.Equals(0) ? Convert.ToInt32(executionProcessThread[0]) : processId,
                    ThreadId = processId.Equals(0) ? Convert.ToInt32(executionProcessThread[1]) : threadId,
                    Provider = systemPropertiesDictionary["Provider"].ToString(),
                    EventId = Convert.ToInt32(systemPropertiesDictionary["EventID"]),
                    Level = level.Equals(string.Empty) ? systemPropertiesDictionary["Level"].ToString() : level,
                    Version = CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Version"),
                    Channel = systemPropertiesDictionary["Channel"].ToString(),
                    Security = CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Security"),
                    Keywords = keywords.Equals(string.Empty) ? CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Keywords") : keywords,
                    Correlation = !string.IsNullOrWhiteSpace(CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Correlation")) ? CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Correlation") : string.Empty,
                    Task = task.Equals(string.Empty) ? systemPropertiesDictionary["Task"].ToString() : task,
                    Opcode = opCode.Equals(string.Empty) ? CommonXmlFunctions.GetSafeExpandoObjectValue(systemPropertiesDictionary, "Opcode") : opCode,
                    EventData = returnEventDataDictionary ? (dynamic)namedProperties : json,
                    LogFileLineage = serializedLogFileLineage
                };
            }
            catch (Exception ex)
            {
                Trace.TraceError($"WinLog.EventRecordConversion.ToJsonLogRecord() threw an exception: {ex}");
                return null;
            }
        }
    }
}