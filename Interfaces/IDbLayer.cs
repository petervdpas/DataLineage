using System.Collections.Generic;

namespace DataLayer
{
    public interface IDbLayer
    {
        void InitializeDatabase(params System.Type[] entityTypes);
        bool DoesTableExist(string tableName);
        void Insert<T>(T entity) where T : class;
        T? GetById<T>(int id) where T : class;
        List<T> GetAll<T>() where T : class;
        void Update<T>(T entity) where T : class;
        void Delete<T>(int id) where T : class;
    }
}
