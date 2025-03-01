using System.Collections.Generic;
using Interfaces;

namespace Lineage
{
    public class DataLineageTracker : IDataLineageTracker
    {
        private readonly List<LineageEntry> _lineageEntries = new();

        public void Track(
            string sourceName, string sourceEntity, string sourceField,
            string transformationRule,
            string targetName, string targetEntity, string targetField)
        {
            _lineageEntries.Add(new LineageEntry(
                sourceName, sourceEntity, sourceField,
                transformationRule,
                targetName, targetEntity, targetField
            ));
        }

        public List<LineageEntry> GetLineage()
        {
            return [.. _lineageEntries];
        }
    }
}