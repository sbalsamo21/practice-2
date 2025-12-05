using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using TaskModel = api.Models.Task;
using MyApp.Namespace.DataAccess;
using MyApp.Namespace.ModelUtility;
using System.Data;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly Database _database;
        private readonly IConfiguration _configuration;

        public TaskController(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found");
            _database = new Database(connectionString);
        }

        // GET /task
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskModel>>> GetAllTasks()
        {
            using var connection = _database.GetConnection();
            
            try
            {
                const string query = "SELECT taskid, name, description, status, priority, created_at FROM task ORDER BY created_at DESC";
                var reader = await DbUtility.ExecuteReaderAsync(connection, query);

                var tasks = new List<TaskModel>();
                while (await reader.ReadAsync())
                {
                    tasks.Add(new TaskModel
                    {
                        TaskId = reader.GetInt32("taskid"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description"),
                        Status = reader.GetString("status"),
                        Priority = reader.IsDBNull("priority") ? string.Empty : reader.GetString("priority"),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }

                await reader.CloseAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving tasks", error = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // GET /task/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskModel>> GetTask(int id)
        {
            using var connection = _database.GetConnection();
            
            try
            {
                await connection.OpenAsync();
                var query = "SELECT taskid, name, description, status, priority, created_at FROM task WHERE taskid = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);
                
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                var task = new TaskModel
                {
                    TaskId = reader.GetInt32("taskid"),
                    Name = reader.GetString("name"),
                    Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description"),
                    Status = reader.GetString("status"),
                    Priority = reader.IsDBNull("priority") ? string.Empty : reader.GetString("priority"),
                    CreatedAt = reader.GetDateTime("created_at")
                };

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the task", error = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // POST /task
        [HttpPost]
        public async Task<ActionResult<TaskModel>> CreateTask([FromBody] TaskModel task)
        {
            if (task == null)
            {
                return BadRequest(new { message = "Task data is required" });
            }

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                return BadRequest(new { message = "Task name is required" });
            }

            using var connection = _database.GetConnection();
            
            try
            {
                await connection.OpenAsync();
                
                var insertQuery = @"INSERT INTO task (name, description, status, priority, created_at) 
                                   VALUES (@name, @description, @status, @priority, NOW())";
                
                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@name", task.Name ?? string.Empty);
                command.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
                command.Parameters.AddWithValue("@status", task.Status ?? string.Empty);
                command.Parameters.AddWithValue("@priority", task.Priority ?? string.Empty);
                
                await command.ExecuteNonQueryAsync();

                var newTaskId = command.LastInsertedId;
                
                var getTaskQuery = "SELECT taskid, name, description, status, priority, created_at FROM task WHERE taskid = @id";
                using var getCommand = new MySqlCommand(getTaskQuery, connection);
                getCommand.Parameters.AddWithValue("@id", newTaskId);
                
                using var reader = await getCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var createdTask = new TaskModel
                    {
                        TaskId = reader.GetInt32("taskid"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description"),
                        Status = reader.GetString("status"),
                        Priority = reader.IsDBNull("priority") ? string.Empty : reader.GetString("priority"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };

                    return CreatedAtAction(nameof(GetTask), new { id = createdTask.TaskId }, createdTask);
                }

                return StatusCode(500, new { message = "Failed to retrieve created task" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the task", error = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // PUT /task/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskModel>> UpdateTask(int id, [FromBody] TaskModel task)
        {
            if (task == null)
            {
                return BadRequest(new { message = "Task data is required" });
            }

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                return BadRequest(new { message = "Task name is required" });
            }

            using var connection = _database.GetConnection();
            
            try
            {
                await connection.OpenAsync();
                
                // Check if task exists
                var checkQuery = "SELECT COUNT(*) FROM task WHERE taskid = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);
                var exists = await checkCommand.ExecuteScalarAsync();

                if (exists == null || Convert.ToInt32(exists) == 0)
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                var updateQuery = @"UPDATE task 
                                   SET name = @name, 
                                       description = @description, 
                                       status = @status, 
                                       priority = @priority
                                   WHERE taskid = @id";
                
                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@id", id);
                updateCommand.Parameters.AddWithValue("@name", task.Name ?? string.Empty);
                updateCommand.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
                updateCommand.Parameters.AddWithValue("@status", task.Status ?? string.Empty);
                updateCommand.Parameters.AddWithValue("@priority", task.Priority ?? string.Empty);
                
                await updateCommand.ExecuteNonQueryAsync();

                var getTaskQuery = "SELECT taskid, name, description, status, priority, created_at FROM task WHERE taskid = @id";
                using var getCommand = new MySqlCommand(getTaskQuery, connection);
                getCommand.Parameters.AddWithValue("@id", id);
                
                using var reader = await getCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var updatedTask = new TaskModel
                    {
                        TaskId = reader.GetInt32("taskid"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull("description") ? string.Empty : reader.GetString("description"),
                        Status = reader.GetString("status"),
                        Priority = reader.IsDBNull("priority") ? string.Empty : reader.GetString("priority"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };

                    return Ok(updatedTask);
                }

                return NotFound(new { message = $"Task with id {id} not found after update" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the task", error = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // DELETE /task/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            using var connection = _database.GetConnection();
            
            try
            {
                await connection.OpenAsync();
                
                // Check if task exists
                var checkQuery = "SELECT COUNT(*) FROM task WHERE taskid = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);
                var exists = await checkCommand.ExecuteScalarAsync();

                if (exists == null || Convert.ToInt32(exists) == 0)
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                var deleteQuery = "DELETE FROM task WHERE taskid = @id";
                using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@id", id);
                
                await deleteCommand.ExecuteNonQueryAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the task", error = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
