using System.Collections.Generic;
using DataLineage.Tracking.Interfaces;
using Models.Source;
using Models.Target;

namespace Mappers
{
    public class PocoMapper
    {
        private readonly IEntityMapper _mapper;
        private readonly IDataLineageTracker _lineageTracker;

        public PocoMapper(IEntityMapper mapper, IDataLineageTracker lineageTracker)
        {
            _mapper = mapper;
            _lineageTracker = lineageTracker;
        }

        public PocoA Map(PocoX pocoX, PocoY pocoY)
        {
            return _mapper.Map(new List<object> { pocoX, pocoY }, sources =>
            {
                string sourceName = "Progress";
                string targetName = "FCDM";

                // 🔹 Track lineage for each mapped property
                _lineageTracker.Track(
                    sourceName, nameof(PocoX), nameof(PocoX.Id), true, "Identifier",
                    "Concatenation with PocoY.Code",
                    targetName, nameof(PocoA), nameof(PocoA.Bk), true, "BusinessKey");

                _lineageTracker.Track(
                    sourceName, nameof(PocoX), nameof(PocoX.Name), true, "The Code for PocoX",
                    "Concatenation with PocoY.Code",
                    targetName, nameof(PocoA), nameof(PocoA.NamedCode), true, "A NameCode");

                _lineageTracker.Track(
                    sourceName, nameof(PocoY), nameof(PocoY.PocoYDate), true, "A Date",
                    "Direct mapping",
                    targetName, nameof(PocoA), nameof(PocoA.Date), true, "A Date");

                return new PocoA
                {
                    Bk = $"{pocoX.Id}_{pocoY.Code}",
                    NamedCode = $"{pocoX.Name}-{pocoY.Code}",
                    Date = pocoY.PocoYDate
                };
            }, _lineageTracker);
        }
    }
}