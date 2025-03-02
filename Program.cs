using Models.Source;
using Models.Target;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using DataLineage.Tracking;
using DataLineage.Tracking.Interfaces;
using DataLayer;

class Program
{
    static void Main()
    {
        // Load configuration
        var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .Build();

        string connectionString = config["Database:ConnectionString"] ?? "Data Source=:memory:;";

        Console.WriteLine($"🔍 Checking database path: {connectionString}");

        // Set up Dependency Injection (DI)
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IDbLayer>(sp => new DbLayer(connectionString))
            .AddDataLineageTracking()
            .BuildServiceProvider();

        // Resolve services
        var dbLayer = serviceProvider.GetRequiredService<IDbLayer>();
        var lineageTracker = serviceProvider.GetRequiredService<IDataLineageTracker>();
        var mapper = serviceProvider.GetRequiredService<IEntityMapper>();

        // ✅ Ensure database schema is created **before** inserting records
        dbLayer.InitializeDatabase(typeof(PocoX), typeof(PocoY));

        // Check if tables exist before inserting
        if (!dbLayer.DoesTableExist("PocoX") || !dbLayer.DoesTableExist("PocoY"))
        {
            Console.WriteLine("Error: Tables were not created!");
            return;
        }

        // 🔹 Insert multiple records for PocoX and PocoY
        var pocoXRecords = new List<PocoX>
        {
            new() { Id = 1, Name = "X1", PocoYDate = new DateOnly(2023, 1, 10) },
            new() { Id = 2, Name = "X2", PocoYDate = new DateOnly(2023, 2, 15) },
            new() { Id = 3, Name = "X3", PocoYDate = new DateOnly(2023, 3, 20) }
        };

        var pocoYRecords = new List<PocoY>
        {
            new() { Id = 1, PocoXId = 1, Code = "Y1", IsActive = true, PocoYDate = new DateOnly(2024, 4, 5) },
            new() { Id = 2, PocoXId = 1, Code = "Y2", IsActive = false, PocoYDate = new DateOnly(2024, 5, 10) },
            new() { Id = 3, PocoXId = 2, Code = "Y3", IsActive = true, PocoYDate = new DateOnly(2024, 6, 15) }
        };

        // Insert into database
        foreach (var x in pocoXRecords) dbLayer.Insert(x);
        foreach (var y in pocoYRecords) dbLayer.Insert(y);


        // 🔹 Retrieve all records from the database
        var allPocoX = dbLayer.GetAll<PocoX>();
        var allPocoY = dbLayer.GetAll<PocoY>();

        // 🔹 Perform LINQ join between PocoX and PocoY in memory
        var joinedRecords = from x in allPocoX
                            join y in allPocoY on x.Id equals y.PocoXId
                            select new
                            {
                                PocoX = x,
                                PocoY = y
                            };

        var mappedPocoARecords = new List<PocoA>();

        foreach (var record in joinedRecords)
        {
            Func<IEnumerable<object>, PocoA> mapToPocoA = (sources) =>
            {
                var pocoX = record.PocoX;
                var pocoY = record.PocoY;

                string sourceName = "Progress";
                string targetName = "FCDM";

                // Track lineage for each mapped property
                lineageTracker.Track(sourceName, nameof(PocoX), nameof(PocoX.Id), true, "Identifier",
                    "Concatenation with PocoY.Code",
                    targetName, nameof(PocoA), nameof(PocoA.Bk), true, "BusinessKey");

                lineageTracker.Track(sourceName, nameof(PocoX), nameof(PocoX.Name), true, "The Code for PocoX",
                    "Concatenation with PocoY.Code",
                    targetName, nameof(PocoA), nameof(PocoA.NamedCode), true, "A NameCode");

                lineageTracker.Track(sourceName, nameof(PocoY), nameof(PocoY.PocoYDate), true, "A Date",
                    "Direct mapping",
                    targetName, nameof(PocoA), nameof(PocoA.Date), true, "A Date");

                return new PocoA
                {
                    Bk = $"{pocoX.Id}_{pocoY.Code}",
                    NamedCode = $"{pocoX.Name}-{pocoY.Code}",
                    Date = pocoY.PocoYDate
                };
            };

            var sources = new List<object> { record.PocoX, record.PocoY };
            var pocoA = mapper.Map(sources, mapToPocoA, lineageTracker);
            mappedPocoARecords.Add(pocoA);
        }

        // 🔹 Display mapped records
        Console.WriteLine("\nMapped PocoA Records:");
        foreach (var pocoA in mappedPocoARecords)
        {
            Console.WriteLine($"Bk: {pocoA.Bk}, NamedCode: {pocoA.NamedCode}, Date: {pocoA.Date}");
        }

        // 🔹 Retrieve and display lineage
        Console.WriteLine("\nData Lineage:");
        foreach (var entry in lineageTracker.GetLineage())
        {
            Console.WriteLine(entry);
        }
    }
}
