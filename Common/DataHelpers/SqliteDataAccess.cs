using System.Diagnostics;
using Dapper;
using drewCo.Tools;
using Microsoft.Data.Sqlite;

namespace officepark.io.Data;

// ==========================================================================
public interface IDataAccess
{
  IEnumerable<T> RunQuery<T>(string query, object qParams);
  int RunExecute(string query, object qParams);
  T? RunSingleQuery<T>(string query, object parameters);
}

// ========================================================================== 
public class SqliteDataAccess<TSchema> : IDataAccess
{
  // This is the ISO8601 format mentioned in:
  // https://www.sqlite.org/datatype3.html
  public const string SQLITE_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fffffff";

  public string DataDirectory { get; private set; }
  public string DBFilePath { get; private set; }
  public string ConnectionString { get; private set; }

  // --------------------------------------------------------------------------------------------------------------------------
  public SqliteDataAccess(string dataDir, string dbFileName)
  {
    DataDirectory = dataDir;
    DBFilePath = Path.Combine(DataDirectory, $"{dbFileName}.sqlite");
    ConnectionString = $"Data Source={DBFilePath};Mode=ReadWriteCreate";

    SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
    SqlMapper.AddTypeHandler<DateTimeOffset>(new DateTimeOffsetHandler());
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
    var schema = new SchemaDefinition(new SqliteFlavor(), typeof(TSchema));

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
    // Make sure that the file exists!
    var parts = ConnectionString.Split(";");
    foreach (var p in parts)
    {
      if (p.StartsWith("Data Source"))
      {
        string filePath = p.Split("=")[1].Trim();
        if (!File.Exists(filePath))
        {
          Debug.WriteLine($"The database file at: {filePath} does not exist!");
          return false;
        }
      }
    }

    var props = ReflectionTools.GetProperties<TSchema>();
    foreach (var p in props)
    {
      if (!HasTable(p.Name)) { return false; }
    }

    return true;
    // NOTE: This is simple.  In the future we could come up with a more robust verison of this.
    // bool res = HasTable(nameof(TimeManSchema.Sessions));
    // return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  private bool HasTable(string tableName)
  {
    // Helpful:
    // https://www.sqlite.org/schematab.html

    // NOTE: Later we can find a way to validate schema versions or whatever....
    var conn = new SqliteConnection(ConnectionString);
    conn.Open();
    string query = $"SELECT * from sqlite_schema where type = 'table' AND tbl_name=@tableName";

    var qr = conn.Query(query, new { tableName = tableName });
    bool res = qr.Count() > 0;
    conn.Close();

    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Runs a database transaction, automatically rolling it back if there is an exception.
  /// </summary>
  protected void Transaction(Action<SqliteConnection> txWork)
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
  public IEnumerable<T> RunQuery<T>(string query, object qParams)
  {
    // NOTE: This connection object could be abstracted more so that we could handle
    // connection pooling, etc. as neeed.
    using (var conn = new SqliteConnection(ConnectionString))
    {
      conn.Open();
      var res = RunQuery<T>(conn, query, qParams);
      return res;
    }

  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected IEnumerable<T> RunQuery<T>(SqliteConnection conn, string query, object parameters)
  {
    var res = conn.Query<T>(query, parameters);
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  /// <summary>
  /// Run a query where a single, or no result is expected.
  /// </summary>
  /// <remarks>
  /// If the query returns more than one result, and exception will be thrown.
  /// </remarks>
  public T? RunSingleQuery<T>(string query, object parameters)
  {
    IEnumerable<T> qr = RunQuery<T>(query, parameters);
    T? res = qr.SingleOrDefault();
    return res;
  }

  // --------------------------------------------------------------------------------------------------------------------------
  public int RunExecute(string query, object qParams)
  {
    using (var conn = new SqliteConnection(ConnectionString))
    {
      conn.Open();
      int res = RunExecute(conn, query, qParams);
      return res;
    }
  }

  // --------------------------------------------------------------------------------------------------------------------------
  protected int RunExecute(SqliteConnection conn, string query, object qParams)
  {
    int res = conn.Execute(query, qParams);
    return res;
  }

}