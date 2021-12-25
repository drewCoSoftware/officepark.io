using System.Text.Json.Serialization;

namespace TimeManUI.Data
{

  // ============================================================================================================================
  public interface ITimeManDataAccess
  {
    void SetCurrentUser(string? userID);
    string? CurrentUserID { get; }

    /// <exception cref="InvalidOperationException">This should throw an exception if the current user ID is null or otherwise invalid.</exception>
    string ValidateUser();

    TimeManSession? GetCurrentSession();
    TimeManSession? StartSession(DateTimeOffset timestamp);
    TimeManSession? EndSession(DateTimeOffset timestamp);

    void AddTimeMark(TimeMark mark);
    void AddTimeMark(TimeMark mark, TimeManSession session);

    ///// <summary>
    ///// Add a time mark to the session.
    ///// </summary>
    //TimeMark AddTimeMark(TimeManSession session);


    //WorkCategory AddCategory(WorkCategory workCategory);

    /// <summary>
    /// Cancel any currently active session!
    /// </summary>
    void CancelCurrentSession();

    /// <summary>
    /// Get the session by its ID.
    /// </summary>
    /// <returns>
    /// The session with the matching ID, or null if it doesn't exist!
    /// </returns>
    TimeManSession? GetSession(int sessionID);

    IEnumerable<TimeManSession> GetSessions();
    IEnumerable<TimeManSession> GetSessions(Predicate<TimeManSession> filter);

    void SaveCurrentSession(TimeManSession session);
  }


  // ============================================================================================================================
  public interface IHasPrimary
  {
    int ID { get; set; }
  }

  // ============================================================================================================================
  /// <summary>
  /// Used to indicate that the member in question has a relationship to some other data type.
  /// This is like a foreign key in a database.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  public class Relationship : Attribute
  {
  }

  // ============================================================================================================================
  [AttributeUsage(AttributeTargets.Property)]
  public class PrimaryKey : Attribute
  {
  }

  //// ============================================================================================================================
  //[AttributeUsage(AttributeTargets.Property)]
  //public class PrimaryKey : Attribute
  //{
  //}

  // ============================================================================================================================
  public class TimeManSchema
  {
    public List<TimeManSession> Sessions { get; set; } = new List<TimeManSession>();
    public List<TimeManSession> ActiveSessions { get; set; } = new List<TimeManSession>();
  }

  // ============================================================================================================================
  public class TimeManSession : IHasPrimary
  {
    public int ID { get; set; } = 0;
    public string UserID { get; set; } = "DEFAULT";
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }

    [Relationship]
    public List<TimeMark> TimeMarks { get; set; } = new List<TimeMark>();

    [JsonIgnore]
    public bool IsActive { get { return StartTime != null && EndTime == null; } }

    [JsonIgnore]
    public bool HasStarted { get { return StartTime != null; } }

    [JsonIgnore]
    public bool HasEnded { get { return HasStarted && EndTime != null; } }
  }

  // ============================================================================================================================
  /// <summary>
  /// Used to mark certain segments of time in a TimeMan session.
  /// This is used to help describe what the user is actually spending their time on, work, meetings, paperwork, etc.
  /// </summary>
  public class TimeMark : IHasPrimary
  {
    public int ID { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Notes { get; set; }

    [Relationship]
    public WorkCategory Category { get; set; }

    public bool IsBillable { get; set; }
  }



  // ============================================================================================================================
  public class WorkCategory : IHasPrimary
  {
    public int ID { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Indicates that this category of work is billable by default.
    /// </summary>
    public bool IsBillable { get; set; }
  }



}
