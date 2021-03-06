using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common;
using Wyam.Common.Caching;
using Wyam.Common.Configuration;
using Wyam.Common.Execution;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Tracing;
using Wyam.Common.Util;

namespace Wyam.Core.Caching
{
    internal class ExecutionCacheManager
    {
        private readonly ConcurrentDictionary<IModule, ExecutionCache> _executionCaches
            = new ConcurrentDictionary<IModule, ExecutionCache>();

        // Creates one if it doesn't exist
        public IExecutionCache Get(IModule module, IReadOnlySettings settings)
        {
            return settings.Bool(Keys.UseCache)
                ? _executionCaches.GetOrAdd(module, new ExecutionCache())
                : (IExecutionCache)new NoCache();
        }

        public void ResetEntryHits()
        {
            foreach (ExecutionCache cache in _executionCaches.Values)
            {
                cache.ResetEntryHits();
            }
        }

        public void ClearUnhitEntries()
        {
            foreach (KeyValuePair<IModule, ExecutionCache> item in _executionCaches)
            {
                int count = item.Value.ClearUnhitEntries().Count;
                Trace.Verbose("Removed {0} stale cache entries for module {1}", count, item.Key.GetType().Name);
            }
        }
    }
}
