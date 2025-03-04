using Models.Source;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using DataLayer;
using Mappers;
using System.Reflection;
using Models.Target;
using DataLineage.Tracking;
using DataLineage.Tracking.Sinks;
using DataLineage.Tracking.Interfaces;

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
            .AddDataLineageTracking(
                Assembly.GetExecutingAssembly(),
                options =>
                {
                    options.EnableLineageTracking = true;
                    options.ThrowOnNullSources = true;
                },
                new FileLineageSink("lineage.json", append: true))

            // Register BaseMapper (without lineage tracking)
            .AddSingleton<IEntityMapper<IEnumerable<object>, PocoA>, PocoMapper>()

            // Register TrackableMapper (with lineage tracking)
            .AddSingleton<ITrackableMapper<IEnumerable<object>, PocoA>, TrackablePocoMapper>()

            .BuildServiceProvider();

        // Resolve services
        var dbLayer = serviceProvider.GetRequiredService<IDbLayer>();
        var lineageTracker = serviceProvider.GetRequiredService<IDataLineageTracker>();

        var baseMapper = serviceProvider.GetRequiredService<IEntityMapper<IEnumerable<object>, PocoA>>();
        var trackableMapper = serviceProvider.GetRequiredService<ITrackableMapper<IEnumerable<object>, PocoA>>();

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

        // 🔹 Use LINQ to join PocoX and PocoY in-memory
        var joinedRecords = from x in allPocoX
                            join y in allPocoY on x.Id equals y.PocoXId
                            select new { PocoX = x, PocoY = y };

        // 🔹 Test **BaseMapper** (without lineage tracking)
        var baseMappedRecords = joinedRecords
            .Select(record => baseMapper.Map([[record.PocoX, record.PocoY]]))
            .ToList();

        // 🔹 Test **TrackableMapper** (with lineage tracking)
        var trackedMappedRecords = joinedRecords
            .Select(record =>
            {
                var mapped = trackableMapper.Map([[record.PocoX, record.PocoY]]);
                trackableMapper.Track([[record.PocoX, record.PocoY]], mapped).Wait();
                return mapped;
            })
            .ToList();


        // 🔹 Display mapped records (BaseMapper)
        Console.WriteLine("\nMapped PocoA Records (BaseMapper - No Lineage):");
        foreach (var pocoA in baseMappedRecords)
        {
            Console.WriteLine($"Bk: {pocoA.Bk}, NamedCode: {pocoA.NamedCode}, Date: {pocoA.Date}");
        }

        // 🔹 Display mapped records (TrackableMapper)
        Console.WriteLine("\nMapped PocoA Records (TrackableMapper - With Lineage):");
        foreach (var pocoA in trackedMappedRecords)
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
