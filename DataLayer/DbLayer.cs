using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;

namespace DataLayer
{
    public class DbLayer : IDbLayer
    {
        private readonly string _connectionString;

        public DbLayer(string connectionString)
        {
            _connectionString = connectionString;

            // Register Dapper custom type handler for DateOnly
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        }

        /// <summary>
        /// Creates tables dynamically based on the provided entity types.
        /// </summary>
        public void InitializeDatabase(params Type[] entityTypes)
        {
            Console.WriteLine($"📂 Using SQLite database: {_connectionString}");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open(); // Ensure database file is created

            foreach (var type in entityTypes)
            {
                string tableName = type.Name.ToLower();  // Force lowercase table names
                var properties = type.GetProperties();

                string createTableQuery = $"CREATE TABLE IF NOT EXISTS {tableName} (Id INTEGER PRIMARY KEY, ";
                createTableQuery += string.Join(", ", properties.Where(p => p.Name != "Id")
                    .Select(p => $"{p.Name} {GetSqliteType(p.PropertyType)}"));
                createTableQuery += ");";

                connection.Execute(createTableQuery);
                Console.WriteLine($"✅ Table '{tableName}' initialized.");
            }
        }


        /// <summary>
        /// Checks if a table exists in the SQLite database.
        /// </summary>
        public bool DoesTableExist(string tableName)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            Console.WriteLine($"🔍 Checking if table '{tableName}' exists...");

            string query = "SELECT LOWER(name) FROM sqlite_master WHERE type='table' AND LOWER(name) = LOWER(@TableName);";
            var result = connection.ExecuteScalar<string>(query, new { TableName = tableName });

            bool exists = result != null;
            Console.WriteLine(exists ? $"✅ Table '{tableName}' found." : $"❌ Table '{tableName}' NOT found!");

            return exists;
        }

        /// <summary>
        /// Inserts a generic entity into the corresponding table.
        /// </summary>
        public void Insert<T>(T entity) where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            string tableName = typeof(T).Name;

            var properties = typeof(T).GetProperties();

            // Convert DateOnly properties to string before inserting
            var parameters = new DynamicParameters();
            foreach (var prop in properties)
            {
                object? value = prop.GetValue(entity);
                if (value is DateOnly dateOnlyValue)
                {
                    value = dateOnlyValue.ToString("yyyy-MM-dd");  // Convert DateOnly to string
                }
                parameters.Add(prop.Name, value);
            }

            var columnNames = string.Join(", ", properties.Select(p => p.Name));
            var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));

            string insertQuery = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames});";
            connection.Execute(insertQuery, parameters);
        }


        /// <summary>
        /// Retrieves an entity by ID.
        /// </summary>
        public T? GetById<T>(int id) where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            string tableName = typeof(T).Name;

            return connection.QueryFirstOrDefault<T>($"SELECT * FROM {tableName} WHERE Id = @Id;", new { Id = id });
        }

        /// <summary>
        /// Retrieves all records of an entity type.
        /// </summary>
        public List<T> GetAll<T>() where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            string tableName = typeof(T).Name;

            return connection.Query<T>($"SELECT * FROM {tableName}").AsList();
        }


        /// <summary>
        /// Updates an existing entity dynamically.
        /// </summary>
        public void Update<T>(T entity) where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            string tableName = typeof(T).Name;

            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

            string updateQuery = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id;";
            connection.Execute(updateQuery, entity);
        }

        /// <summary>
        /// Deletes an entity by ID.
        /// </summary>
        public void Delete<T>(int id) where T : class
        {
            using var connection = new SqliteConnection(_connectionString);
            string tableName = typeof(T).Name;
            connection.Execute($"DELETE FROM {tableName} WHERE Id = @Id;", new { Id = id });
        }

        /// <summary>
        /// Maps C# types to SQLite types.
        /// </summary>
        private static string GetSqliteType(Type type)
        {
            if (type == typeof(int)) return "INTEGER";
            if (type == typeof(string)) return "TEXT";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "TEXT";
            if (type == typeof(DateOnly)) return "TEXT";
            return "TEXT"; // Default fallback
        }

    }
}
