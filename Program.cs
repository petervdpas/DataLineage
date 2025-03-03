using Models.Source;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using DataLineage.Tracking;
using DataLineage.Tracking.Interfaces;
using DataLayer;
using Mappers;
using DataLineage.Tracking.Sinks;

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
            .AddDataLineageTracking(options =>
            {
                options.EnableLineageTracking = true;
                options.ThrowOnNullSources = true;
            },
            new FileLineageSink("lineage.json", append: false))
            .AddSingleton<PocoMapper>()
            .BuildServiceProvider();

        // Resolve services
        var dbLayer = serviceProvider.GetRequiredService<IDbLayer>();
        var lineageTracker = serviceProvider.GetRequiredService<IDataLineageTracker>();
        var pocoMapper = serviceProvider.GetRequiredService<PocoMapper>();

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

        var mappedRecords = joinedRecords
            .Select(record => pocoMapper.Map(record.PocoX, record.PocoY))
            .ToList();

        // 🔹 Display mapped records
        Console.WriteLine("\nMapped PocoA Records:");
        foreach (var pocoA in mappedRecords)
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
