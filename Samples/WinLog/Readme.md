# The WinLog Sample

&copy; Microsoft. All rights reserved.

WinLog is a simple Console application that demonstrates how to use the WinLog NuGet package to read from various event or data sources.

## About the WinLog NuGet Package

The WinLog NuGet package makes it simple to read event logs into C# objects. It fixes the previous problems in the .Net framework API for reading event logs that was incompatible with the IEnumerable and LINQ concepts.

## Exploring the Sample Application

The sample application reads from three different sources:

- Event (*.evtx) file (supplied with the sample)
- The local Security event log
- A pre-recorded Performance Counter series from a *.csv file (supplied with the sample)

The sample uses three specific methods from the WinLog NuGet package:

### Reading the *.evtx log file

In Windows, the logs are saved in proprietary format with extension .EVTX (Event Log XML). Here is how you can use the WinLog LogReader class `ReadEvtxFile` method to read the log:

``` csharp
var log = LogReader.ReadEvtxFile(@"..\..\LogSample.evtx");
foreach (var e in log.Take(10))
{
    Console.WriteLine("{0} {1} {2}", e.TimeCreated, e.Provider, e.EventId);
}
```

This reads the first 10 events from the log file included with the sample.

### Reading the Windows Event Logs

The Windows Event Logs such as Application, Security, etc. are also EVTX files on disk. The difference is that new events are being written as they occur.  

The code below shows how to use the WinLog LogReader class `ReadWindowsLog` method to read all the currently recorded events in the Security log:

``` csharp
var securityLog = LogReader.ReadWindowsLog("Security", null);

EventRecord last = null;
foreach (var e in securityLog)
{
    last = e;
    var ev = LogReader.ParseEvent(e.ToXml());
    Console.WriteLine("{0} {1} {2}", ev.TimeCreated, ev.Provider, ev.EventId);
}
```

### Reading pre-recorded Performance Counter series from a *.csv file

The code below shows how to use the WinLog `CsvCounterReader` class `ReadCountersFile` method to read pre-recorded Performance Counter series from a *.csv file:

``` csharp
var log = LogReader.ReadWindowsLog("Security");

foreach (LogRecord r in log.Take(10))
{
    Console.WriteLine("{0} {1} {2}", r.TimeCreated, r.Provider, r.EventId);
};
```

### Reading recent events in the Windows Event Logs

Although not demonstrated in this sample application, you can also read recent, or specific, event from the Windows Event Logs.

The Windows Event Logs quite large and are being written all the time, by adding new events as they occur. You can use a System.Diagnostics.Eventing.Reader  `EventBookmark`, to continue to read from the last event you read before:

``` csharp
var newEvents = LogReader.ReadWindowsLog("Security", last.Bookmark);

foreach (var e in newEvents)
{
    last = e;
    var ev = LogReader.ParseEvent(e.ToXml());
    Console.WriteLine("{0} {1} {2}", ev.TimeCreated, ev.Provider, ev.EventId);
}
```

## See Also

[CDOC Samples](../CDOC.Samples.Readme.md)