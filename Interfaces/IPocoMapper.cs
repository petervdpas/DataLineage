using System;
using System.Collections.Generic;

namespace Interfaces
{
    public interface IPocoMapper
    {
        TResult Map<TSource, TResult>(
            IEnumerable<TSource> sources,
            Func<IEnumerable<TSource>, TResult> mappingFunction,
            IDataLineageTracker? lineageTracker = null
        );
    }
}