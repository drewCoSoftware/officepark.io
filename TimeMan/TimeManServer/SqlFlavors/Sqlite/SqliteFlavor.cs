﻿using drewCo.Tools;

namespace TimeManServer.Data
{
  // ============================================================================================================================
  public class SqliteFlavor : ISqlFlavor
  {
    private readonly SqliteDataTypeResolver _TypeResolver = new SqliteDataTypeResolver();
    public IDataTypeResolver TypeResolver { get { return _TypeResolver; } }
  }


  // ============================================================================================================================
  public class SqliteDataTypeResolver : IDataTypeResolver
  {
    public string GetDataTypeName(Type t, bool forceNull)
    {
      string res = "";

      bool isNull = false;
      if (ReflectionTools.IsNullable(t))
      {
        isNull = true;
        t = t.GetGenericArguments()[0];
      }
      isNull = isNull | forceNull;

      if (t == typeof(Int32) || t == typeof(Int64))
      {
        res = "INTEGER";
      }
      else if (t == typeof(float) || t == typeof(double))
      {
        res = "REAL";
      }
      else if (t == typeof(string))
      {
        res = "TEXT";
      }
      else if (t == typeof(DateTimeOffset))
      {
        // Sqlite is too stupid to have the concept of date and time, so it punts and tries to make it something else.
        // TEXT as ISO8601 strings("YYYY-MM-DD HH:MM:SS.SSS")
        res = "TEXT";
      }
      else if (t == typeof(bool))
      {
        // lol, no boolean type either!
        res = "INTEGER";
      }
      else
      {
        throw new NotSupportedException($"The data type {t} is not supported!");
      }

      if (!isNull) { res += " NOT NULL"; }

      return res;
    }
  }


}