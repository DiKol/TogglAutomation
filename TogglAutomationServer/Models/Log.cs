using MySqlConnector;
using TogglAutomationServer.Toggl;
using UskokDB.MySql;
using UskokDB.MySql.Attributes;

namespace TogglAutomationServer.Models;

public class Log : MySqlTable<Log>
{
    [UskokDB.Attributes.NotMapped]
    public static Func<MySqlConnection> ConnectionFactory { get; set; } = null!;

    [MaxLength(5)]
    public string Extension { get; set; } = null!;
    public DateTime Date { get; set; }
    public LogType LogType { get; set; }
    public Project? Project { get; set; }
    public bool UsingTheApp { get; set; }

    public Task Insert() => InsertLog(this);

    public static async Task InsertLog(Log log)
    {
        await using var connection = ConnectionFactory();
        await InsertAsync(connection, log);
    }
    
    public Log() { }
    public Log(string extension, LogType type, Project? project = null)
    {
        Extension = extension;
        LogType = type;
        Project = project;
        UsingTheApp = true;
        Date = DateTime.Now;
    }
}

public enum LogType : int
{
    Connected = 0,
    Disconnected = 1,
    WorkOnProject = 2,
    StoppedTimer = 3,
    Choose = 4,
    CallIncoming = 5,
    CallEnded = 6,
    CallMissedOrDeclined = 7,
    CallStarted = 8,
    WorkBeforeCall = 9,
    WorkAfterCall = 10,
    DoNothing = 11
}
