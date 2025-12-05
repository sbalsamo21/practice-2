using MySqlConnector;

namespace MyApp.Namespace.DataAccess
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}

