using System;
using System.Collections.Generic;
using System.Linq;
using Wyam.Core.Documents;

namespace Wyam.Core.Meta
{
    /// <summary>
    /// This class contains a stack of all the metadata generated at a particular pipeline stage.
    /// Getting a value checks each of the stacks and returns the first hit.
    /// This class is immutable, create a new document to get a new one with additional values.
    /// </summary>
    internal class MetadataStack : Metadata
    {
        internal MetadataStack(Metadata initialMetadata, IEnumerable<KeyValuePair<string, object>> items = null)
            : this(initialMetadata.Stack.Reverse(), items)
        {
        }

        internal MetadataStack(IEnumerable<IReadOnlyDictionary<string, object>> initialMetadata, IEnumerable<KeyValuePair<string, object>> items = null)
            : base(new Stack<IReadOnlyDictionary<string, object>>(initialMetadata))
        {
            if (items != null)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, object> item in items)
                {
                    dictionary[item.Key] = item.Value;
                }
                Stack.Push(dictionary);
            }
        }

        // This clones the stack and pushes a new dictionary on to the cloned stack
        internal MetadataStack Clone(IEnumerable<KeyValuePair<string, object>> items)
        {
            return new MetadataStack(this, items);
        }
    }
}
