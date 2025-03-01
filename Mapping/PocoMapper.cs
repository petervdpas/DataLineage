using System;
using System.Collections.Generic;
using Interfaces;

namespace Mapping
{
    public class PocoMapper : IPocoMapper
    {
        public TResult Map<TSource, TResult>(
            IEnumerable<TSource> sources,
            Func<IEnumerable<TSource>, TResult> mappingFunction,
            IDataLineageTracker? lineageTracker = null) // Optional tracker
        {
            // Perform mapping
            TResult result = mappingFunction.Invoke(sources);

            // If lineage tracking is provided, track lineage
            if (lineageTracker != null)
            {
                foreach (var source in sources)
                {
                    if (source != null)
                    {
                        lineageTracker.Track(
                            sourceName: $"{source.GetType().Name}_{Guid.NewGuid().ToString().Substring(0, 8)}", // Unique instance
                            sourceEntity: source.GetType().Name,
                            sourceField: "Unknown Field", // Generic level does not know exact field names
                            transformationRule: "Generic Mapping Rule", // Generic level does not know exact rules
                            targetName: $"{typeof(TResult).Name}_{Guid.NewGuid().ToString().Substring(0, 8)}", // Unique instance
                            targetEntity: typeof(TResult).Name,
                            targetField: "Unknown Field"
                        );
                    }
                }
            }

            return result;
        }
    }
}
