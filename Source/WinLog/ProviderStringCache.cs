// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *   Licensed under the MIT license.                     *
// *                                                       *
// ********************************************************/

namespace WinLog
{
    using System.Collections.Concurrent;
    using System.Diagnostics.Eventing.Reader;

    internal class ProviderStringCache
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, DisplayNames>> _cache = new ConcurrentDictionary<string, ConcurrentDictionary<int, DisplayNames>>();

        /// <summary>
        /// Returns the level, task, opcode and keywords of the specified EventRecord object as strings.
        /// </summary>
        /// <param name="evt">EventRecord - the event to return data for.</param>
        /// <param name="level">(out) string - the event level.</param>
        /// <param name="task">(out) string - the string representation of the event Task identifier.</param>
        /// <param name="opcode">(out) string - the string representation of the event OpCode identifier.</param>
        /// <param name="keywords">(out) string - the keyword(s) for the event.</param>
        public void Lookup(EventRecord evt, out string level, out string task, out string opcode, out string keywords)
        {
            var providerId = evt.ProviderId.ToString();
            if (providerId == null)
            {
                providerId = evt.ProviderName;
            }

            ConcurrentDictionary<int, DisplayNames> events = null;
            if (!_cache.TryGetValue(providerId, out events))
            {
                events = new ConcurrentDictionary<int, DisplayNames>();
                _cache.TryAdd(providerId, events);
            }

            DisplayNames names = null;
            if (!events.TryGetValue(evt.Id, out names))
            {
                names = new DisplayNames();

                try
                {
                    names.Level = ValueOrEmpty(evt.LevelDisplayName);
                }
                catch
                {
                    names.Level = ValueOrEmpty(evt.Level);
                }

                try
                {
                    names.Task = ValueOrEmpty(evt.TaskDisplayName);
                }
                catch
                {
                    names.Task = ValueOrEmpty(evt.Task);
                }

                try
                {
                    names.Opcode = ValueOrEmpty(evt.OpcodeDisplayName);
                }
                catch
                {
                    names.Opcode = ValueOrEmpty(evt.Opcode);
                }

                try
                {
                    names.Keywords = ValueOrEmpty(evt.KeywordsDisplayNames);
                }
                catch
                {
                    names.Keywords = ValueOrEmpty(evt.Keywords.ToString());
                }

                events.TryAdd(evt.Id, names);
            }

            level = names.Level;
            task = names.Task;
            opcode = names.Opcode;
            keywords = names.Keywords;
        }

        private string ValueOrEmpty(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return value.ToString();
        }

        private class DisplayNames
        {
            public string Level;
            public string Opcode;
            public string Task;
            public string Keywords;
        }
    }
}