using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLineage.Tracking.Interfaces;
using DataLineage.Tracking.Mapping;
using Models.Source;
using Models.Target;

public class EntityMapper : GenericEntityMapper<PocoA>
{
    public EntityMapper(IDataLineageTracker lineageTracker) : base(lineageTracker) { }

    public override PocoA Map(List<object> sources)
    {
        var pocoX = sources.OfType<PocoX>().FirstOrDefault();
        var pocoY = sources.OfType<PocoY>().FirstOrDefault();

        if (pocoX == null || pocoY == null)
        {
            throw new ArgumentException("Both PocoX and PocoY must be provided.");
        }

        return new PocoA
        {
            Bk = $"{pocoX.Id}_{pocoY.Code}",
            NamedCode = $"{pocoX.Name}-{pocoY.Code}",
            Date = pocoY.PocoYDate
        };
    }

    public override async Task Track(List<object>? sources, PocoA? result)
    {
        var pocoX = sources!.OfType<PocoX>().FirstOrDefault();
        var pocoY = sources!.OfType<PocoY>().FirstOrDefault();

        if (pocoX == null || pocoY == null)
        {
            throw new ArgumentException("Both PocoX and PocoY must be provided for lineage tracking.");
        }

        // 🔹 Track lineage dynamically
        var mappings = new List<(
            string SourceEntity, 
            string SourceField, 
            string TransformationRule, 
            string TargetEntity, 
            string TargetField,
            bool Validated,
            string? ModelReferenceUrl,
            int? Classification)>
        {
            (nameof(PocoX), nameof(PocoX.Id), "Concatenation with PocoY.Code", nameof(PocoA), nameof(PocoA.Bk), true, "https:\\example.com", 111),
            (nameof(PocoX), nameof(PocoX.Name), "Concatenation with PocoY.Code", nameof(PocoA), nameof(PocoA.NamedCode), true, null, 211),
            (nameof(PocoY), nameof(PocoY.PocoYDate), "Direct mapping", nameof(PocoA), nameof(PocoA.Date), false, null, 123)
        };

        foreach (var mapping in mappings)
        {
            await _lineageTracker.TrackAsync(
                sourceSystem: null,
                sourceEntity: mapping.SourceEntity,
                sourceField: mapping.SourceField,
                transformationRule: mapping.TransformationRule,
                targetSystem: null,
                targetEntity: mapping.TargetEntity,
                targetField: mapping.TargetField,
                validated: mapping.Validated,
                tags: ["SIS", "DataLineage"],
                modelReferenceUrl: mapping.ModelReferenceUrl,
                classification: mapping.Classification
            );
        }

        await Task.CompletedTask;
    }
}
