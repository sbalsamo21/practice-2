using MySqlConnector;

namespace MyApp.Namespace.ModelUtility
{
    public static class DbUtility
    {
        /// <summary>
        /// Executes a SQL statement that does not return data (INSERT, UPDATE, DELETE).
        /// Returns the number of rows affected.
        /// </summary>
        public static async Task<int> ExecuteNonQueryAsync(MySqlConnection connection, string query)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = new MySqlCommand(query, connection);
            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes a SQL query and returns the first column of the first row as an object.
        /// Useful for COUNT, SUM, MAX, MIN, etc.
        /// </summary>
        public static async Task<object?> ExecuteScalarAsync(MySqlConnection connection, string query)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = new MySqlCommand(query, connection);
            return await command.ExecuteScalarAsync();
        }

        /// <summary>
        /// Executes a SQL query and returns a MySqlDataReader for reading the results.
        /// The caller is responsible for disposing the reader and connection.
        /// </summary>
        public static async Task<MySqlDataReader> ExecuteReaderAsync(MySqlConnection connection, string query)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var command = new MySqlCommand(query, connection);
            return await command.ExecuteReaderAsync();
        }
    }
}

