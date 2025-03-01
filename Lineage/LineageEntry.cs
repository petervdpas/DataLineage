using System;

namespace Lineage
{
    public class LineageEntry
    {
        public string SourceName { get; set; } // Identifier of the source instance
        public string SourceEntity { get; set; } // Type of source entity
        public string SourceField { get; set; } // Field in the source entity
        public string TransformationRule { get; set; } // How data was transformed
        public string TargetName { get; set; } // Identifier of the target instance
        public string TargetEntity { get; set; } // Type of target entity
        public string TargetField { get; set; } // Field in the target entity

        public LineageEntry(
            string sourceName, string sourceEntity, string sourceField,
            string transformationRule,
            string targetName, string targetEntity, string targetField)
        {
            SourceName = sourceName;
            SourceEntity = sourceEntity;
            SourceField = sourceField;
            TransformationRule = transformationRule;
            TargetName = targetName;
            TargetEntity = targetEntity;
            TargetField = targetField;
        }

        public override string ToString()
        {
            return $"{SourceName}.{SourceEntity}.{SourceField} → ({TransformationRule}) → {TargetName}.{TargetEntity}.{TargetField}";
        }
    }
}
