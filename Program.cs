using Models.Source;
using Models.Target;
using Mapping;
using System;
using Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Lineage;
using System.Linq;

class Program
{
    static void Main()
    {
        // Set up Dependency Injection (DI)
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IPocoMapper, PocoMapper>()
            .AddSingleton<IDataLineageTracker, DataLineageTracker>()
            .BuildServiceProvider();

        // Resolve dependencies
        var mapper = serviceProvider.GetRequiredService<IPocoMapper>();
        var lineageTracker = serviceProvider.GetRequiredService<IDataLineageTracker>();


        // Create sample instances of PocoX and PocoY
        PocoX pocoX = new PocoX
        {
            Id = 1,
            Name = "SampleName",
            PocoYDate = new DateOnly(2023, 5, 15)
        };

        PocoY pocoY = new PocoY
        {
            Id = 2,
            Code = "XYZ123",
            IsActive = true,
            PocoYDate = new DateOnly(2024, 2, 25)
        };

        // Define a mapping function with detailed lineage tracking
        Func<IEnumerable<object>, PocoA> mapToPocoA = (sources) =>
        {
            var x = sources.OfType<PocoX>().FirstOrDefault();
            var y = sources.OfType<PocoY>().FirstOrDefault();

            if (x == null || y == null) throw new Exception("Missing source objects");

            string sourceName = "Progress";
            string targetName = "FCDM";

            // Track lineage for each mapped property
            lineageTracker.Track(sourceName, nameof(PocoX), nameof(PocoX.Id), "Concatenation with PocoY.Code",
                targetName, nameof(PocoA), nameof(PocoA.Bk));

            lineageTracker.Track(sourceName, nameof(PocoX), nameof(PocoX.Name), "Concatenation with PocoY.Code",
                targetName, nameof(PocoA), nameof(PocoA.NamedCode));

            lineageTracker.Track(sourceName, nameof(PocoY), nameof(PocoY.PocoYDate), "Direct mapping",
                targetName, nameof(PocoA), nameof(PocoA.Date));

            return new PocoA
            {
                Bk = $"{x.Id}_{y.Code}",
                NamedCode = $"{x.Name}-{y.Code}",
                Date = y.PocoYDate
            };
        };

        // Provide the list of source POCOs
        var sources = new List<object> { pocoX, pocoY };

        // Map to PocoA
        PocoA pocoA = mapper.Map(sources, mapToPocoA);

        // Display the result
        Console.WriteLine("Mapped PocoA:");
        Console.WriteLine($"Bk: {pocoA.Bk}");
        Console.WriteLine($"NamedCode: {pocoA.NamedCode}");
        Console.WriteLine($"Date: {pocoA.Date}");

        // Retrieve and display lineage
        Console.WriteLine("\nData Lineage:");
        foreach (var entry in lineageTracker.GetLineage())
        {
            Console.WriteLine(entry);
        }
    }
}
