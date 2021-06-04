// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

using System.IO;
using System.Reflection;

namespace WinLogSample
{
    using System;
    using System.Linq;
    using WinLog;

    class Program
    {
        static void Main()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo dir_info = new DirectoryInfo(directoryName);

            while (true)
            {
                Console.WriteLine("Select (F) for existing log file,  (L) for the WinOS Security log, or (C) for counters");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.F:
                        var evtx = LogReader.ReadEvtxFile(Path.Combine(dir_info.FullName, @"LogSample.evtx")); // Read a log file from disk
                        foreach (LogRecord e in evtx.Take(10))
                        {
                            Console.WriteLine("{0} {1} {2}", e.TimeCreated, e.Provider, e.EventId);
                        }
                        break;

                    case ConsoleKey.L:
                        var log = LogReader.ReadWindowsLog("Security"); // Read one of the OS logs, that you see in EventVwr

                        foreach (LogRecord r in log.Take(10))
                        {
                            Console.WriteLine("{0} {1} {2}", r.TimeCreated, r.Provider, r.EventId);
                        };
                        break;

                    case ConsoleKey.C:
                        var samples = CsvCounterReader.ReadCountersFile(Path.Combine(dir_info.FullName, @"ExampleRecording.csv")); // Read recording from PerfMon
                        foreach (CounterSample sample in samples.Take(10))
                        {
                            Console.WriteLine("{0} {1} {2}", sample.Timestamp, sample.Instance, sample.Counters);
                        }
                        break;

                    default:
                        continue;
                }
            }
        }
    }
}