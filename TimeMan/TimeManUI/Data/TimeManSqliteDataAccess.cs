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

namespace TimeManUI.Data
{

  // ============================================================================================================================
  public class SchemaDefinition
  {
    private object ResolveLock = new object();
    private Dictionary<string, TableDef> _TableDefs = new Dictionary<string, TableDef>(StringComparer.OrdinalIgnoreCase);
    public ReadOnlyCollection<TableDef> TableDefs { get { return new ReadOnlyCollection<TableDef>(_TableDefs.Values.ToList()); } }


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

    public ReadOnlyCollection<DependentTable> DependentTables { get { return new ReadOnlyCollection<DependentTable>(_DependentTables); } }
    private List<DependentTable> _DependentTables = new List<DependentTable>();

    private List<ColumnDef> _Columns = new List<ColumnDef>();
    public ReadOnlyCollection<ColumnDef> Columns { get { return new ReadOnlyCollection<ColumnDef>(_Columns); } }

    // --------------------------------------------------------------------------------------------------------------------------
    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public TableDef(Type type_, string name_, SchemaDefinition schema)
    {
      DataType = type_;
      Name = name_;

      foreach (var p in ReflectionTools.GetProperties(DataType))
      {
        if (!p.CanWrite) { continue; }

        var dependent = ReflectionTools.GetAttribute<Relationship>(p);
        if (dependent != null)
        {
          Type useType = p.PropertyType;
          if (ReflectionTools.HasInterface<IList>(useType))
          {
            useType = useType.GetGenericArguments()[0];
          }

          // Get the dependent table...
          var def = schema.ResolveTableDef(p.Name, useType);
          _DependentTables.Add(new DependentTable()
          {
            Def = def,
          });


          _Columns.Add(new ColumnDef(def.Name + "_ID",
                                     schema.Flavor.TypeResolver.GetDataTypeName(typeof(int)),
                                     false,
                                     def.Name,
                                     nameof(IHasPrimary.ID)));

        }
        else
        {
          // This is a normal column.
          // NOTE: Non-related lists can't be represented.... should we make it so that lists are always included?
          _Columns.Add(new ColumnDef(p.Name,
                                     schema.Flavor.TypeResolver.GetDataTypeName(p.PropertyType),
                                     p.Name == nameof(IHasPrimary.ID),
                                     null,
                                     null));
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
    string GetDataTypeName(Type t);
  }

  // ============================================================================================================================
  public class SqliteDataTypeResolver : IDataTypeResolver
  {
    public string GetDataTypeName(Type t)
    {
      string res = "";

      bool isNull = false;
      if (ReflectionTools.IsNullable(t))
      {
        isNull = true;
        t = t.GetGenericArguments()[0];
      }
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


  //public class ColumnDef
  //{
  //  public ColumnDef(string name, Type dataType, bool isPrimary, string dataType, string? relatedTableName, string? relatedTableColumn)
  //  {
  //    Name = name;
  //    DataType = dataType;
  //    RelatedTableName = relatedTableName;
  //    RelatedTableColumn = relatedTableColumn;

  //  }

  //  public bool IsPrimary { get; private set; }
  //  public string Name { get; private set; }
  //  public string DataType { get; private set; }
  //  public string? RelatedTableName { get; private set; }
  //  public string? RelatedTableColumn { get; private set; }

  //}

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
  /// <summary>
  /// This is a filesystem based data driver for time man.
  /// </summary>
  public class TimeManSqliteDataAccess : ITimeManDataAccess
  {
    public string DataDirectory { get; private set; }
    private string DBFilePath { get; set; }
    private string ConnectionString;

    private string? _CurrentUserID = null;
    public string? CurrentUserID { get; private set; }

    private DataTableFile<TimeManSession> Sessions;

    //// private object 
    //  private ConcurrentDictionary<string, object> UserDataLocks = new ConcurrentDictionary<string, object>();


    // --------------------------------------------------------------------------------------------------------------------------
    public TimeManSqliteDataAccess(string dataDir, string dbFileName)
    {
      DataDirectory = dataDir;
      DBFilePath = Path.Combine(DataDirectory, $"{dbFileName}.sqlite");
      ConnectionString = $"Data Source={DBFilePath};Mode=ReadWriteCreate";

      Sessions = new DataTableFile<TimeManSession>(DataDirectory);
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
      // schema.AddTable<TimeManSchema>();

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
      string query = $"SELECT * from ActiveSessions where UserID = @userID";

      var conn = new SqliteConnection(ConnectionString);
      conn.Open();

      var qr = conn.Query<TimeManSession>(query, new { userID = userID });

      conn.Close();

      TimeManSession? res = qr.FirstOrDefault();
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
    public void SaveSession(TimeManSession session)
    {
      if (session == null)
      {
        throw new ArgumentNullException(nameof(session));
      }

      throw new NotImplementedException();

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

        Sessions.AddItem(res);
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
      TimeManSession? res = Sessions.GetItem(sessionID);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public IEnumerable<TimeManSession> GetSessions()
    {
      List<TimeManSession> res = Sessions.GetItems();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public IEnumerable<TimeManSession> GetSessions(Predicate<TimeManSession> filter)
    {
      var res = Sessions.GetItems().Where(x => filter(x));
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void CancelCurrentSession()
    {
      string userID = ValidateUser();

      // throw new NotImplementedException();
      //// Destroy the session file.
      //SafeWrite(userID, () =>
      //{
      //  string path = GetCurrentSessionPath(userID);
      //  FileTools.DeleteExistingFile(path);
      //});
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
}
