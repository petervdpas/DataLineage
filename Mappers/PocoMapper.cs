using System;
using System.Collections.Generic;
using System.Linq;
using DataLineage.Tracking.Mapping;
using Models.Source;
using Models.Target;

namespace Mappers
{
    /// <summary>
    /// A standard mapper that maps multiple source POCOs (PocoX, PocoY) to PocoA.
    /// Does not track data lineage.
    /// </summary>
    public class PocoMapper : BaseMapper<IEnumerable<object>, PocoA>
    {
        public PocoMapper() { } // No lineage tracker needed for base mapper

        /// <summary>
        /// Maps PocoX and PocoY into PocoA.
        /// </summary>
        public override PocoA Map(IEnumerable<IEnumerable<object>> sourceData) // ✅ Fix: match base class
        {
            var sources = sourceData.SelectMany(x => x).ToList(); // Flatten nested list
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
    }
}
