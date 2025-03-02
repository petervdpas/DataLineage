using System;
using System.Data;
using Dapper;

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToString("yyyy-MM-dd");  // Store as string in SQLite
        parameter.DbType = System.Data.DbType.String;
    }

    public override DateOnly Parse(object value)
    {
        return DateOnly.Parse((string)value);  // Convert back from string when retrieving
    }
}
