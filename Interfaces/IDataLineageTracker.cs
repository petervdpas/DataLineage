using System.Collections.Generic;
using Lineage;

namespace Interfaces
{
    public interface IDataLineageTracker
    {
        void Track(string sourceName, string sourceEntity, string sourceField, string transformationRule, string targetName, string targetEntity, string targetField);
        List<LineageEntry> GetLineage();
    }
}