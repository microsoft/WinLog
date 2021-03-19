// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

namespace WinLog
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using Newtonsoft.Json;
    using WinLog.Helpers;

    /// <summary>
    /// Class that defines the event metadata.
    /// </summary>
    public class EventIdMetrics
    {
        public int EventId { get; set; }

        public string EventProvider { get; set; }

        public int EventCount { get; set; }

        public string EventXml { get; set; }

        public string EventChannel { get; set; }
    }

    /// <summary>
    /// Class that defines the various metrics associated with event uploading.
    /// </summary>
    public class EventLogUploadResult
    {
        /// <summary>
        /// The number of event records that were uploaded.
        /// </summary>
        public int EventCount { get; set; }

        /// <summary>
        /// The number of events that matched the upload filter criteria. 
        /// </summary>
        public int FilteredEventCount { get; set; }

        /// <summary>
        /// The number of seconds that it took the program to read all of the events from the event source.
        /// </summary>
        public double TimeToRead { get; set; }

        /// <summary>
        /// The number of seconds that it took to complete uploading the events to the destination.
        /// </summary>
        public double TimeToUpload { get; set; }

        /// <summary>
        /// Whether the upload completed successfully. 
        /// </summary>
        public bool UploadSuccessful { get; set; }

        /// <summary>
        /// The returned error message, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The list of EventIdMetrics uploaded.
        /// </summary>
        public Dictionary<string, EventIdMetrics> EventIdMetricsList { get; set; }

        /// <summary>
        /// The number of events without User Data.
        /// </summary>
        public int NullUserDataCount { get; set; }

        /// <summary>
        /// The number of events without extended Event Data.
        /// </summary>
        public int NullEventDataCount { get; set; }
    }

    /// <summary>
    /// Class that defines an event log record for the purposes of the WinLog namespace. 
    /// </summary>
    public class LogRecord
    {
        public string Provider;
        public int EventId;
        public string Version;
        public string Level;
        public string Task;
        public string Opcode;
        public string Keywords;
        public DateTime TimeCreated;
        public long EventRecordId;
        public Guid Correlation; //missing in LogRecordCdoc
        public int ProcessId;
        public int ThreadId;
        public string Channel;
        public string Computer;
        public string Security;
        public dynamic EventData;
        public dynamic LogFileLineage;

        public EventBookmark Bookmark { get; private set; }

        public LogRecord(EventBookmark bookmark)
        {
            Bookmark = bookmark;
        }

        public LogRecord()
        {
        }

        public LogRecord(dynamic record, EventBookmark bookmark)
        {
            Bookmark = bookmark;

            SetCommonAttributes(record);
        }

        internal LogRecord(dynamic record)
        {
            SetCommonAttributes(record);
        }

        private void SetCommonAttributes(dynamic record)
        {
            IDictionary<string, object> dictionaryRecord = record;

            Provider = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Provider");
            EventId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "EventId"));
            TimeCreated = Convert.ToDateTime(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "TimeCreated"));
            Computer = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Computer");
            EventRecordId = Convert.ToInt64(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "EventRecordId"));

            if (dictionaryRecord.ContainsKey("EventData"))
            {
                EventData = JsonConvert.SerializeObject(dictionaryRecord["EventData"], Formatting.Indented);
            }

            // Newly added properties
            Version = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Version");
            Level = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Level");
            Task = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Task");
            Opcode = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Opcode");
            Security = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Security");
            Channel = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Channel");

            Keywords = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Keywords");

            Guid resultCorrelation;
            if (Guid.TryParse(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Correlation"), out resultCorrelation))
            {
                Correlation = resultCorrelation;
            }

            // Variant System properties (not on all Windows Events)
            string processId = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ProcessID");
            if (!string.IsNullOrEmpty(processId))
            {
                ProcessId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ProcessID"));
            }

            string threadId = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ThreadID");
            if (!string.IsNullOrEmpty(threadId))
            {
                ThreadId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ThreadID"));
            }
        }
    }

    /// <summary>
    /// Class that defines an event log record for the purposes of the Microsoft Cyber Defense Operations Center (CDOC). 
    /// </summary>
    public class LogRecordCdoc
    {
        public string Provider;
        public int EventId;
        public string Version;
        public string Level;
        public string Task;
        public string Opcode;
        public DateTime TimeCreated;
        public long EventRecordId;
        public int ProcessId;
        public int ThreadId;
        public string Channel;
        public string Computer;
        public string Security;
        public dynamic EventData;
        public dynamic LogFileLineage;

        public EventBookmark Bookmark { get; private set; }

        public LogRecordCdoc(EventBookmark bookmark)
        {
            Bookmark = bookmark;
        }

        public LogRecordCdoc()
        {
        }

        public LogRecordCdoc(dynamic record, EventBookmark bookmark)
        {
            Bookmark = bookmark;

            SetCommonAttributes(record);
        }

        internal LogRecordCdoc(dynamic record)
        {
            SetCommonAttributes(record);
        }

        private void SetCommonAttributes(dynamic record)
        {
            IDictionary<string, object> dictionaryRecord = record;

            Provider = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Provider");
            EventId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "EventId"));
            TimeCreated = Convert.ToDateTime(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "TimeCreated"));
            Computer = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Computer");
            EventRecordId = Convert.ToInt64(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "EventRecordId"));

            EventData = JsonConvert.SerializeObject(record.EventData, Formatting.Indented);

            // Newly added properties
            Version = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Version");
            Level = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Level");
            Task = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Task");
            Opcode = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Opcode");
            Security = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Security");
            Channel = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "Channel");

            // Variant System properties (not on all Windows Events)
            string processId = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ProcessID");
            if (!string.IsNullOrEmpty(processId))
            {
                ProcessId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ProcessID"));
            }

            string threadId = CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ThreadID");
            if (!string.IsNullOrEmpty(threadId))
            {
                ThreadId = Convert.ToInt32(CommonXmlFunctions.GetSafeExpandoObjectValue(dictionaryRecord, "ThreadID"));
            }
        }
    }

    /// <summary>
    /// Class that defines an event log record for the purposes of the WinLog namespace. 
    /// Includes extended properties, such as Bookmarks and embedded LogRecordEx instances.
    /// </summary>
    public class LogRecordEx
    {
        public string Provider;
        public int EventId;
        public string Version;
        public string Level;
        public string Task;
        public string Opcode;
        public DateTime TimeCreated;
        public long EventRecordId;
        public int ProcessId;
        public int ThreadId;
        public string Channel;
        public string Computer;
        public string Security;
        public dynamic EventData;
        public dynamic EventContext;
        public dynamic LogFileLineage;

        public LogRecordEx(EventBookmark bookmark)
        {
            Bookmark = bookmark;
        }

        public LogRecordEx()
        {
        }

        public EventBookmark Bookmark { get; private set; }
    }

    /// <summary>
    /// Class for enabling JSON parsing of events.
    /// </summary>
    public class JsonParseFilter
    {
        public JsonParseFilter()
        {
        }

        public JsonParseFilter(string eventId, string dataName, string contains)
        {
            EventId = eventId;
            DataName = dataName;
            Contains = contains;
        }

        public string EventId { get; set; }

        public string DataName { get; set; }

        public string Contains { get; set; }
    }

    /// <summary>
    /// Class defining metadata about where and when a log file was collected.
    /// </summary>
    public class LogFileLineage
    {
        public string Collector { get; set; }

        public string UploadMachine { get; set; }

        public long LogFileId { get; set; }

        public long Seq { get; set; }

        public DateTime CollectorTimeStamp { get; set; }

        public string CollectorUnixTimeStamp { get; set; }
    }
}