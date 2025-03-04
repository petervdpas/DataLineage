using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLineage.Tracking.Interfaces;
using DataLineage.Tracking.Mapping;
using Models.Source;
using Models.Target;

public class TrackablePocoMapper : TrackableBaseMapper<IEnumerable<object>, PocoA>
{
    public TrackablePocoMapper(IDataLineageTracker lineageTracker) : base(lineageTracker) { }

    public override PocoA Map(IEnumerable<IEnumerable<object>> sources)
    {
        var flatSources = sources.SelectMany(x => x); // Flatten the sources

        var pocoX = flatSources.OfType<PocoX>().FirstOrDefault();
        var pocoY = flatSources.OfType<PocoY>().FirstOrDefault();

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

    public override async Task Track(IEnumerable<IEnumerable<object>> sources, PocoA result)
    {
        var flatSources = sources.SelectMany(x => x); // Flatten the sources

        var pocoX = flatSources.OfType<PocoX>().FirstOrDefault();
        var pocoY = flatSources.OfType<PocoY>().FirstOrDefault();

        if (pocoX == null || pocoY == null)
        {
            throw new ArgumentException("Both PocoX and PocoY must be provided for lineage tracking.");
        }

            string sourceSystem = "Progress";
            string targetSystem = "FCDM";

            // 🔹 Track lineage dynamically
            var mappings = new List<(string SourceEntity, string SourceField, string TransformationRule, string TargetEntity, string TargetField)>
            {
                (nameof(PocoX), nameof(PocoX.Id), "Concatenation with PocoY.Code", nameof(PocoA), nameof(PocoA.Bk)),
                (nameof(PocoX), nameof(PocoX.Name), "Concatenation with PocoY.Code", nameof(PocoA), nameof(PocoA.NamedCode)),
                (nameof(PocoY), nameof(PocoY.PocoYDate), "Direct mapping", nameof(PocoA), nameof(PocoA.Date))
            };

            foreach (var mapping in mappings)
            {
                _lineageTracker.Track(
                    sourceSystem, mapping.SourceEntity, mapping.SourceField, true, "Tracked Field",
                    mapping.TransformationRule,
                    targetSystem, mapping.TargetEntity, mapping.TargetField, true, "Mapped Field");
            }

            await Task.CompletedTask;
    }
}
