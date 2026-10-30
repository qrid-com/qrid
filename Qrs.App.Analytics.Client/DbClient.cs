using Qrid.Common.Analytics;
using Qrid.Common.Client.Api;
using System.Data.SQLite;

namespace Qrs.App.Analytics.Client
{
    public class DbClient : CommonContext
    {
        private string _dbPath;
        private string _connectionString;

        public SQLiteConnection Connection { get; set; }

        public DbClient(string dbPath)
        {
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath};Version=3;";
            Connection = new SQLiteConnection(_connectionString);
        }

        public void Init()
        {
            EnsureDatabaseExists();
        }

        public void ConnectionOpenIfClosed()
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
                Connection.Open();
        }

        public void ConnectionClose()
        {
            if (Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }

        public void EnsureDatabaseExists()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                ConnectionOpenIfClosed();
                try
                {
                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS session_log (
                            InstanceId TEXT NOT NULL,
                            SessionId INTEGER NOT NULL,
                            App TEXT,
                            Ip TEXT,
                            Created DATETIME,
                            PRIMARY KEY (InstanceId, SessionId)
                        );
                    ";

                    using var command = new SQLiteCommand(createTableQuery, Connection);
                    command.ExecuteNonQuery();

                    ConnectionClose();        
                }
                catch (Exception ex)  
                {
                    LogDebug(ex.ToString());
                    // do not want to live there uninitialized db file
                    File.Delete(_dbPath);
                    throw ex;
                }

            }
        }

        public int Insert(SessionLogItem logItem)
        {
            

            string insertQuery = @"
                INSERT INTO session_log (InstanceId, SessionId, App, Ip, Created)
                VALUES (@InstanceId, @SessionId, @App, @Ip, @Created);
            ";

            using var command = new SQLiteCommand(insertQuery, Connection);
            command.Parameters.AddWithValue("@InstanceId", logItem.InstanceId);
            command.Parameters.AddWithValue("@SessionId", logItem.SessionId);
            command.Parameters.AddWithValue("@App", logItem.App);
            command.Parameters.AddWithValue("@Ip", logItem.Ip);
            command.Parameters.AddWithValue("@Created", logItem.Created);

            return command.ExecuteNonQuery();
        }

        public bool Exists(string instanceId, int sessionId)
        {
            

            string existsQuery = @"
                SELECT 1 FROM session_log 
                WHERE InstanceId = @InstanceId AND SessionId = @SessionId;
            ";

            using var command = new SQLiteCommand(existsQuery, Connection);
            command.Parameters.AddWithValue("@InstanceId", instanceId);
            command.Parameters.AddWithValue("@SessionId", sessionId);

            var result = command.ExecuteScalar();
            return result != null;  // DbNull ???
        }



    }
}
