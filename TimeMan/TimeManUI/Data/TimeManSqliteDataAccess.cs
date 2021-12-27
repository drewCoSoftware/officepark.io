using drewCo.Tools;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using drewCo.Curations;
using TimeMan;
using static Dapper.SqlMapper;
using System.Data;

namespace TimeManUI.Data
{

  // ============================================================================================================================
  public class SchemaDefinition
  {
    private object ResolveLock = new object();
    private Dictionary<string, TableDef> _TableDefs = new Dictionary<string, TableDef>(StringComparer.OrdinalIgnoreCase);
    public ReadOnlyCollection<TableDef> TableDefs { get { return new ReadOnlyCollection<TableDef>(_TableDefs.Values.ToList()); } }


    // --------------------------------------------------------------------------------------------------------------------------
    public TableDef? GetTableDef(string tableName)
    {
      if (_TableDefs.TryGetValue(tableName, out TableDef? tableDef))
      {
        return tableDef;
      }
      else
      {
        return null;
      }
    }


    // --------------------------------------------------------------------------------------------------------------------------
    public ISqlFlavor Flavor { get; private set; }
    public SchemaDefinition(ISqlFlavor flavor_)
    {
      Flavor = flavor_;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Create a new schema defintion from the given type.  Each of the properties in <paramref name="schemaType"/>
    /// will be used to create a new table in the schema.
    /// </summary>
    public SchemaDefinition(ISqlFlavor flavor_, Type schemaType)
      : this(flavor_)
    {
      // We will add a table for each of the properties defined in 'schemaType'
      var props = ReflectionTools.GetProperties(schemaType);
      foreach (var prop in props)
      {
        if (!prop.CanWrite) { continue; }
        var useType = prop.PropertyType;
        if (ReflectionTools.HasInterface<IList>(useType))
        {
          useType = useType.GetGenericArguments()[0];
        }

        // NOTE: Other attributes could be analyzed to change table names, etc.
        ResolveTableDef(prop.Name, useType);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public SchemaDefinition AddTable<T>()
    {
      string name = typeof(T).Name;
      return AddTable<T>(name);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public SchemaDefinition AddTable<T>(string tableName)
    {
      ResolveTableDef(tableName, typeof(T));
      return this;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    internal bool HasTableDef(string tableName, Type propertyType)
    {
      if (_TableDefs.TryGetValue(tableName, out TableDef def))
      {
        return def.DataType == propertyType;
      }
      else
      {
        return false;
      }
    }


    // --------------------------------------------------------------------------------------------------------------------------
    internal TableDef ResolveTableDef(string tableName, Type propertyType)
    {
      lock (ResolveLock)
      {
        if (_TableDefs.TryGetValue(tableName, out TableDef def))
        {
          if (def.DataType != propertyType)
          {
            throw new InvalidOperationException($"There is already a table named '{tableName}' with the data type '{def.DataType}'");
          }
          return def;
        }
        else
        {
          Type useType = propertyType;
          bool isList = ReflectionTools.HasInterface<IList>(useType);
          if (isList)
          {
            useType = useType.GetGenericArguments()[0];
          }
          var res = new TableDef(useType, tableName, this);
          _TableDefs.Add(tableName, res);
          res.PopulateMembers();

          return res;
        }
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Returns the SQL that is required to represent this schema in a database.
    /// </summary>
    /// <remarks>
    /// For the moment, this only supports sqlite syntax.  More options (postgres) will be added later.
    /// </remarks>
    public string GetCreateSQL()
    {
      var sb = new StringBuilder(0x800);

      // Sort all tables by dependency.
      List<TableDef> defs = SortDependencies(_TableDefs);


      // For each of the defs, we have to build our queries.
      foreach (var d in defs)
      {
        sb.AppendLine(ComputeCreateTableQuery(d));
      }

      return sb.ToString();



      // throw new NotImplementedException();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private string ComputeCreateTableQuery(TableDef tableDef)
    {
      var sb = new StringBuilder(0x400);
      sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableDef.Name} (");

      var colDefs = new List<string>();
      var fkDefs = new List<string>();

      foreach (var col in tableDef.Columns)
      {
        string useName = FormatName(col.Name);

        string def = $"{useName} {col.DataType}";
        if (col.IsPrimary)
        {
          def += " PRIMARY KEY";
        }

        colDefs.Add(def);

        if (col.RelatedTableName != null)
        {
          string fk = $"FOREIGN KEY({useName}) REFERENCES {col.RelatedTableName}({col.RelatedTableColumn})";
          fkDefs.Add(fk);
        }

      }
      sb.AppendLine(string.Join(", " + Environment.NewLine, colDefs) + (fkDefs.Count > 0 ? "," : ""));

      foreach (var fk in fkDefs)
      {
        sb.AppendLine(fk);
      }


      sb.AppendLine(");");

      string res = sb.ToString();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// NOTE: This should happen when we are building out our defs.
    /// </summary>
    private string FormatName(string name)
    {
      return name.ToLower();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private List<TableDef> SortDependencies(Dictionary<string, TableDef> tableDefs)
    {
      var res = new List<TableDef>(tableDefs.Values.ToList());

      // LOL, this probably won't work!
      // It would be nice if it was just a matter of counting.  This will suffice for now.
      res.Sort((l, r) => l.DependentTables.Count.CompareTo(r.DependentTables.Count));

      return res;
    }
  }

  // ============================================================================================================================
  public class TableDef
  {
    public Type DataType { get; private set; }
    public string Name { get; private set; }
    public SchemaDefinition Schema { get; private set; }

    public ReadOnlyCollection<DependentTable> DependentTables { get { return new ReadOnlyCollection<DependentTable>(_DependentTables); } }
    private List<DependentTable> _DependentTables = new List<DependentTable>();

    private List<ColumnDef> _Columns = new List<ColumnDef>();
    public ReadOnlyCollection<ColumnDef> Columns { get { return new ReadOnlyCollection<ColumnDef>(_Columns); } }


    // --------------------------------------------------------------------------------------------------------------------------
    public TableDef(Type type_, string name_, SchemaDefinition schema_)
    {
      DataType = type_;
      Name = name_;
      Schema = schema_;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    internal void PopulateMembers()
    {
      foreach (var p in ReflectionTools.GetProperties(DataType))
      {
        if (!p.CanWrite) { continue; }

        var dependent = ReflectionTools.GetAttribute<Relationship>(p);
        if (dependent != null)
        {
          Type useType = p.PropertyType;
          bool isList = ReflectionTools.HasInterface<IList>(useType);
          if (isList)
          {
            useType = useType.GetGenericArguments()[0];
          }

          // Get the related table...
          var relatedDef = Schema.ResolveTableDef(p.Name, useType);


          // This is where we decide if we want a reference to a single item, or a list of them.
          string colName = $"{this.Name}_ID";
          string fkTableName = this.Name;
          var fkTableDef = this;

          var fkType = ReflectionTools.IsNullable(p.PropertyType) ? typeof(int?) : typeof(int);

          // NOTE: This is some incomplete work for many-many relationships, which we don't
          // actually support at this time.  Keep this block around for a while....
          //if (isList)
          //{
          //  // Resolve the mapping table....
          //  // what to do about the actual data type....  If we don't have a type defined,
          //  // should we just generate one?
          //  //object mappingTable = new { ParentID = (int)0, ChildID = (int)0 };
          //  //Type mappingTableType = mappingTable.GetType();

          //  fkTableName = $"{Name}_to_{relatedDef.Name}";
          //  Type mappingTableType = ResolveMappingTableType(DataType, useType);

          //  fkTableDef = Schema.ResolveTableDef(fkTableName, mappingTableType);
          //}
          ////else
          ////{
          ////  int x = 10;
          ////}
          ///

          relatedDef._DependentTables.Add(new DependentTable()
          {
            Def = fkTableDef,
          });

          relatedDef._Columns.Add(new ColumnDef(colName,
                                     Schema.Flavor.TypeResolver.GetDataTypeName(fkType, false),
                                     false,
                                     fkTableName,
                                     nameof(IHasPrimary.ID)));

        }
        else
        {
          // This is a normal column.
          // NOTE: Non-related lists can't be represented.... should we make it so that lists are always included?
          _Columns.Add(new ColumnDef(p.Name,
                                     Schema.Flavor.TypeResolver.GetDataTypeName(p.PropertyType, false),
                                     p.Name == nameof(IHasPrimary.ID),
                                     null,
                                     null));
        }
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private Type ResolveMappingTableType(Type parentType, Type childType)
    {
      // HACK: We won't always want a new instance of this....
      TypeGenerator gen = new TypeGenerator();
      Type res = gen.ResolveMappingTableType(parentType, childType);
      return res;
    }

  }

  // ============================================================================================================================
  // NOTE: This might want to go live with reflection tools?
  public class TypeGenerator
  {
    private object CacheLock = new object();
    private MultiDictionary<Type, Type, Type> _MappingTypesCache = new MultiDictionary<Type, Type, Type>();

    private DynamicTypeManager TypeMan = new DynamicTypeManager("TimeMan_DynamicTypes");

    public Type ResolveMappingTableType(Type parentType, Type childType)
    {
      lock (CacheLock)
      {
        // TODO: Update this call to 'TryGetValue'
        if (_MappingTypesCache.ContainsKey(parentType, childType))
        {
          return _MappingTypesCache[parentType, childType];
        }
        else
        {
          // We will now generate the new type definition....
          TypeDef tDef = new TypeDef()
          {
            Name = $"{parentType.Name}_To_{childType.Name}"
          };
          tDef.Properties.Add(new TypeDef.PropertyDef()
          {
            Name = parentType.Name,
            Type = parentType.Name,
            Attributes = new List<TypeDef.AttributeDef>()
            {
              new TypeDef.AttributeDef(typeof(Relationship))
            }
          });
          tDef.Properties.Add(new TypeDef.PropertyDef()
          {
            Name = childType.Name,
            Type = childType.Name,
            Attributes = new List<TypeDef.AttributeDef>()
            {
              new TypeDef.AttributeDef(typeof(Relationship))
            }
          });

          Type res = TypeMan.CreateDynamicType(tDef);
          _MappingTypesCache.Add(parentType, childType, res);
          return res;
        }
      }
    }


  }

  // ============================================================================================================================
  public interface ISqlFlavor
  {
    IDataTypeResolver TypeResolver { get; }
  }

  // ============================================================================================================================
  public interface IDataTypeResolver
  {
    string GetDataTypeName(Type t, bool forceNull);
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

  // ============================================================================================================================
  public class SqliteFlavor : ISqlFlavor
  {
    private readonly SqliteDataTypeResolver _TypeResolver = new SqliteDataTypeResolver();
    public IDataTypeResolver TypeResolver { get { return _TypeResolver; } }
  }

  // ============================================================================================================================
  public record ColumnDef(string Name, string DataType, bool IsPrimary, string? RelatedTableName, string? RelatedTableColumn);


  // ============================================================================================================================
  /// <summary>
  /// Describes a table that another is dependent upon.
  /// This is your typical Foreign Key relationship in an RDBMS system.
  /// </summary>
  public class DependentTable
  {
    public TableDef Def { get; set; }

  }

  // ============================================================================================================================
  public class DateTimeOffsetHandler : TypeHandler<DateTimeOffset>
  {
    // --------------------------------------------------------------------------------------------------------------------------
    public override DateTimeOffset Parse(object value)
    {
      if (value == null) { return DateTimeOffset.MinValue; }
      if (DateTimeOffset.TryParse(value as string, out DateTimeOffset res))
      {
        return res;
      }
      throw new InvalidOperationException($"Input value: '{value as string}' is not a valid DateTimeOffset type!");
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
      parameter.Value = value;
    }
  }


  // ============================================================================================================================
  /// <summary>
  /// This is a filesystem based data driver for time man.
  /// </summary>
  public class TimeManSqliteDataAccess : ITimeManDataAccess
  {

    // This is the ISO8601 format mentioned in:
    // https://www.sqlite.org/datatype3.html
    public const string SQLITE_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fffffff";

    public string DataDirectory { get; private set; }
    private string DBFilePath { get; set; }
    private string ConnectionString;

    private string? _CurrentUserID = null;
    public string? CurrentUserID { get; private set; }

    //  private DataTableFile<TimeManSession> Sessions;

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSqliteDataAccess(string dataDir, string dbFileName)
    {
      DataDirectory = dataDir;
      DBFilePath = Path.Combine(DataDirectory, $"{dbFileName}.sqlite");
      ConnectionString = $"Data Source={DBFilePath};Mode=ReadWriteCreate";

      //   Sessions = new DataTableFile<TimeManSession>(DataDirectory);


      SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
      SqlMapper.AddTypeHandler<DateTimeOffset>(new DateTimeOffsetHandler());
    }


    // --------------------------------------------------------------------------------------------------------------------------
    public void SetCurrentUser(string? userID)
    {
      _CurrentUserID = userID;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public string ValidateUser()
    {
      if (string.IsNullOrWhiteSpace(_CurrentUserID)) { throw new InvalidOperationException("The current user ID is null!"); }
      return _CurrentUserID;
    }




    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// This makes sure that we have a database, and the schema is correct.
    /// </summary>
    public void SetupDatabase()
    {
      // Look at the current schema, and make sure that it is up to date....
      bool hasCorrectSchema = ValidateSchema();
      if (!hasCorrectSchema)
      {
        CreateSchema();
      }


    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void CreateSchema()
    {
      var schema = new SchemaDefinition(new SqliteFlavor(), typeof(TimeManSchema));

      string query = schema.GetCreateSQL();

      var conn = new SqliteConnection(ConnectionString);
      conn.Open();
      using (var tx = conn.BeginTransaction())
      {
        conn.Execute(query);
        tx.Commit();
      }
      conn.Close();


    }



    // --------------------------------------------------------------------------------------------------------------------------
    private bool ValidateSchema()
    {
      // NOTE: This is simple.  In the future we could come up with a more robust verison of this.
      bool res = HasTable(nameof(TimeManSchema.Sessions));
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private bool HasTable(string tableName)
    {
      // Helpful:
      // https://www.sqlite.org/schematab.html

      // NOTE: Later we can find a way to validate schema versions or whatever....
      var conn = new SqliteConnection(ConnectionString);
      conn.Open();
      string query = $"select * from sqlite_schema where type = 'table' AND tbl_name=@tableName";

      var qr = conn.Query(query, new { tableName = tableName });
      bool res = qr.Count() > 0;
      conn.Close();

      return res;

    }




    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? GetCurrentSession()
    {
      string userID = ValidateUser();

      // NOTE: These queries will have to be sensitive to the names that are generated during schema creation.
      string query = $"SELECT * from Sessions where UserID = @userID AND EndTime IS NULL";

      TimeManSession? res = RunQuery<TimeManSession>(query, new { userID = userID }).FirstOrDefault();

      if (res == null || res.HasEnded)
      {
        res = null;
      }

      return res;

      ////string dataPath = GetCurrentSessionPath(userID);
      ////if (File.Exists(dataPath))
      ////{
      ////  var data = File.ReadAllText(dataPath);
      ////  var sesh = JsonSerializer.Deserialize<TimeManSession>(data);

      ////  if (sesh.HasEnded) { return null; }


      //return sesh;
      //}
      //return null;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Runs a database transaction, automatically rolling it back if there is an exception.
    /// </summary>
    private void Transaction(Action<SqliteConnection> txWork)
    {
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();

        using (var tx = conn.BeginTransaction())
        {
          try
          {
            txWork(conn);
            tx.Commit();
          }
          catch (Exception ex)
          {
            // TODO: A better logging mechanism!
            Console.WriteLine($"An exception was encountered when trying to execute the transaction!");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Transaction will be rolled back!");

            tx.Rollback();
          }
        }
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IEnumerable<T> RunQuery<T>(string query, object qParams)
    {
      // NOTE: This connection object could be abstracted more so that we could handle
      // connection pooling, etc. as neeed.
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();
        return RunQuery<T>(conn, query, qParams);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private IEnumerable<T> RunQuery<T>(SqliteConnection conn, string query, object parameters)
    {
      var res = conn.Query<T>(query, parameters);
      return res;
    }


    // --------------------------------------------------------------------------------------------------------------------------
    private void RunExecute(string query, object qParams)
    {
      using (var conn = new SqliteConnection(ConnectionString))
      {
        conn.Open();
        RunExecute(conn, query, qParams);
      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private void RunExecute(SqliteConnection conn, string query, object qParams)
    {
      conn.Execute(query, qParams);
    }


    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Save or update the given session data.
    /// </summary>
    public void SaveSession(TimeManSession session)
    {
      if (session == null)
      {
        throw new ArgumentNullException(nameof(session));
      }
      var qParams = new
      {
        userID = session.UserID,
        startTime = session.StartTime?.ToString(SQLITE_DATETIME_FORMAT),
        endTime = session.EndTime?.ToString(SQLITE_DATETIME_FORMAT),
        sessionID = session.ID
      };

      if (session.ID == 0)
      {
        string query = "INSERT INTO Sessions (UserID, StartTime, EndTime) VALUES (@userID, @startTime, @endTime) RETURNING ID";
        int newID = RunQuery<int>(query, qParams).First();
        session.ID = newID;
      }
      else
      {
        // We are updating exiting data.
        string query = "UPDATE Sessions SET StartTime = @startTime, EndTime = @endTime WHERE ID = @sessionID";
        RunExecute(query, qParams);
      }



      // throw new NotImplementedException();

      //// We want to write this data to disk...
      //SafeWrite(session.UserID, () =>
      //{
      //  string path = GetCurrentSessionPath(session.UserID);
      //  string? dir = Path.GetDirectoryName(path);
      //  FileTools.CreateDirectory(dir);

      //  string data = JsonSerializer.Serialize(session);
      //  File.WriteAllText(path, data);
      //});
    }


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession StartSession(DateTimeOffset timestamp)
    {
      string userID = ValidateUser();

      TimeManSession? cur = GetCurrentSession();
      if (cur == null || cur.HasEnded)
      {
        TimeManSession sesh = new TimeManSession()
        {
          UserID = userID,
          StartTime = timestamp
        };
        SaveSession(sesh);
        return sesh;
      }

      if (cur.HasStarted)
      {
        EndSession(timestamp);
        return StartSession(timestamp);
      }

      return cur;
    }


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? EndSession(DateTimeOffset timestamp)
    {
      var res = GetCurrentSession();

      if (res != null && !res.HasEnded)
      {
        res.EndTime = timestamp;
        SaveSession(res);
      }

      return res;
    }



    //// --------------------------------------------------------------------------------------------------------------------------
    //private string GetUserDir(string userID)
    //{
    //  string res = Path.Combine(DataDirectory, userID);
    //  return res;
    //}

    //// --------------------------------------------------------------------------------------------------------------------------
    //private string GetSessionDir(string userID)
    //{
    //  string res = Path.Combine(GetUserDir(userID), "Sessions");
    //  return res;
    //}

    //// --------------------------------------------------------------------------------------------------------------------------
    //private string GetCurrentSessionPath(string userID)
    //{
    //  string res = Path.Combine(GetSessionDir(userID), "CurSession.json");
    //  return res;
    //}

    //// --------------------------------------------------------------------------------------------------------------------------
    //private string GetSessionHistoryDirectory(string userID)
    //{
    //  string res = Path.Combine(GetSessionDir(userID), "SessionHistory");
    //  return res;
    //}

    //// --------------------------------------------------------------------------------------------------------------------------
    //private void SafeWrite(string userID, Action action)
    //{
    //  object l = ResolveLock(userID);
    //  lock (l)
    //  {
    //    action();
    //  }
    //}

    //// --------------------------------------------------------------------------------------------------------------------------
    //private object ResolveLock(string userID)
    //{
    //  if (!UserDataLocks.TryGetValue(userID, out object res))
    //  {
    //    res = new object();
    //    UserDataLocks.TryAdd(userID, res);
    //  }

    //  return res;
    //}

    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSession? GetSession(int sessionID)
    {
      // NOTE: Some way to get all of the time-marks would be good too....
      string query = "SELECT * FROM Sessions WHERE ID = @id";
      var qParams = new { id = sessionID };

      var res = RunQuery<TimeManSession>(query, qParams).SingleOrDefault();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public IEnumerable<TimeManSession> GetSessions()
    {
      // NOTE: Some way to get all of the time-marks would be good too....
      string query = "SELECT * FROM Sessions";

      var res = RunQuery<TimeManSession>(query, null);
      return res;
    }

    //--------------------------------------------------------------------------------------------------------------------------
    //NOTE: These types of predicate filters won't work with dapper since we have to
    //create the queries by hand.Some kind of automation for this may be in order in the future....
    public IEnumerable<TimeManSession> GetSessions(string userID)
    {
      string query = "SELECT * FROM Sessions WHERE UserID = @userID";
      var qParams = new { userID = userID };

      var res = RunQuery<TimeManSession>(query, qParams);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void CancelCurrentSession()
    {
      var current = GetCurrentSession();
      if (current != null)
      {
        Transaction((conn) =>
        {
          var qParams = new { id = current.ID };

          // Cleanup any of the time marks!
          {
            string query = $"DELETE FROM TimeMarks WHERE Sessions_ID = @id";
            RunExecute(conn, query, qParams);
          }

          // Now delete the parent object.
          {
            string query = $"DELETE FROM Sessions WHERE ID = @id";
            RunExecute(conn, query, qParams);
          }
        });

      }
    }


    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Adds a time mark to the currently active session.
    /// </summary>
    public void AddTimeMark(TimeMark mark)
    {
      TimeManSession? session = GetCurrentSession();
      if (session == null)
      {
        throw new InvalidOperationException("You may not add a time mark if there isn't a currently active session!");
      }

      AddTimeMark(mark, session);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void AddTimeMark(TimeMark mark, TimeManSession session)
    {
      string userID = ValidateUser();
      if (session.HasEnded && mark.Timestamp > session.EndTime)
      {
        throw new InvalidOperationException("You can't add a time mark that exceeds the end time of the session!");
      }

      if (session.UserID != userID)
      {
        throw new InvalidOperationException($"The session user ({session.UserID}) doesn't match the current user ID ({userID})");
      }

      session.TimeMarks.Add(mark);
      SaveSession(session);
    }

  }


  //private class DbConnection : IDisposable
  //{
  //  private SqliteConnection Connection = null;
  //  public void Dispose()
  //  {
  //    Connection.dis
  //    throw new NotImplementedException();
  //  }
  //}
}
