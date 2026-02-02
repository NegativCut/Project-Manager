using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ProjectManager.Models;

namespace ProjectManager.Data
{
    public class DatabaseManager
    {
        private string connectionString;
        private string dbPath;

        public DatabaseManager()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProjectManager");
            
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);

            dbPath = Path.Combine(appDataPath, "projects.db");
            connectionString = $"Data Source={dbPath};Version=3;";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                // Projects table
                string createProjects = @"
                    CREATE TABLE IF NOT EXISTS Projects (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjectNumber TEXT NOT NULL,
                        ProjectName TEXT NOT NULL,
                        RevisionNumber INTEGER NOT NULL,
                        DateCreated TEXT NOT NULL,
                        Status TEXT,
                        Issues TEXT
                    )";
                ExecuteNonQuery(conn, createProjects);

                // Apps table
                string createApps = @"
                    CREATE TABLE IF NOT EXISTS Apps (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AppName TEXT NOT NULL UNIQUE,
                        IsDefault INTEGER NOT NULL DEFAULT 0
                    )";
                ExecuteNonQuery(conn, createApps);

                // ProjectApps junction table
                string createProjectApps = @"
                    CREATE TABLE IF NOT EXISTS ProjectApps (
                        ProjectId INTEGER NOT NULL,
                        AppId INTEGER NOT NULL,
                        PRIMARY KEY (ProjectId, AppId),
                        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
                        FOREIGN KEY (AppId) REFERENCES Apps(Id) ON DELETE CASCADE
                    )";
                ExecuteNonQuery(conn, createProjectApps);

                // Tasks table
                string createTasks = @"
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjectId INTEGER NOT NULL,
                        AppName TEXT,
                        TaskDescription TEXT NOT NULL,
                        IsCompleted INTEGER NOT NULL DEFAULT 0,
                        Priority TEXT,
                        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
                    )";
                ExecuteNonQuery(conn, createTasks);

                // ProjectDatasheets junction table
                string createProjectDatasheets = @"
                    CREATE TABLE IF NOT EXISTS ProjectDatasheets (
                        ProjectId INTEGER NOT NULL,
                        DatasheetPath TEXT NOT NULL,
                        PRIMARY KEY (ProjectId, DatasheetPath),
                        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
                    )";
                ExecuteNonQuery(conn, createProjectDatasheets);

                // Settings table
                string createSettings = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    )";
                ExecuteNonQuery(conn, createSettings);

                // Insert default apps if none exist
                InitializeDefaultApps(conn);
            }
        }

        private void InitializeDefaultApps(SQLiteConnection conn)
        {
            string checkApps = "SELECT COUNT(*) FROM Apps";
            long count = (long)ExecuteScalar(conn, checkApps);

            if (count == 0)
            {
                var defaultApps = new[] { "Altium", "Solidworks", "VisualStudio", "Documents" };
                foreach (var app in defaultApps)
                {
                    string insert = "INSERT INTO Apps (AppName, IsDefault) VALUES (@name, 1)";
                    using (var cmd = new SQLiteCommand(insert, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", app);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void ExecuteNonQuery(SQLiteConnection conn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(SQLiteConnection conn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                return cmd.ExecuteScalar();
            }
        }

        // Project operations
        public int CreateProject(Project project)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Projects (ProjectNumber, ProjectName, RevisionNumber, DateCreated, Status, Issues)
                              VALUES (@number, @name, @rev, @date, @status, @issues)";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@number", project.ProjectNumber);
                    cmd.Parameters.AddWithValue("@name", project.ProjectName);
                    cmd.Parameters.AddWithValue("@rev", project.RevisionNumber);
                    cmd.Parameters.AddWithValue("@date", project.DateCreated.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@status", project.Status ?? "Planning");
                    cmd.Parameters.AddWithValue("@issues", project.Issues ?? "");
                    cmd.ExecuteNonQuery();
                }

                return (int)conn.LastInsertRowId;
            }
        }

        public void UpdateProject(Project project)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"UPDATE Projects SET ProjectName = @name, Status = @status, Issues = @issues
                              WHERE Id = @id";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", project.ProjectName);
                    cmd.Parameters.AddWithValue("@status", project.Status);
                    cmd.Parameters.AddWithValue("@issues", project.Issues ?? "");
                    cmd.Parameters.AddWithValue("@id", project.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Project> GetAllProjects()
        {
            var projects = new List<Project>();
            
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Projects ORDER BY ProjectNumber DESC, RevisionNumber DESC";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var project = new Project
                        {
                            Id = reader.GetInt32(0),
                            ProjectNumber = reader.GetString(1),
                            ProjectName = reader.GetString(2),
                            RevisionNumber = reader.GetInt32(3),
                            DateCreated = DateTime.Parse(reader.GetString(4)),
                            Status = reader.IsDBNull(5) ? "Planning" : reader.GetString(5),
                            Issues = reader.IsDBNull(6) ? "" : reader.GetString(6)
                        };
                        projects.Add(project);
                    }
                }

                // Load app associations
                foreach (var project in projects)
                {
                    project.AppIds = GetProjectAppIds(conn, project.Id);
                }
            }

            return projects;
        }

        public string GetNextProjectNumber()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT MAX(CAST(ProjectNumber AS INTEGER)) FROM Projects";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result == DBNull.Value || result == null)
                        return "0001";
                    
                    int nextNum = Convert.ToInt32(result) + 1;
                    return nextNum.ToString("D4");
                }
            }
        }

        // App operations
        public List<AppInfo> GetAllApps()
        {
            var apps = new List<AppInfo>();
            
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Apps ORDER BY AppName";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        apps.Add(new AppInfo
                        {
                            Id = reader.GetInt32(0),
                            AppName = reader.GetString(1),
                            IsDefault = reader.GetInt32(2) == 1
                        });
                    }
                }
            }

            return apps;
        }

        public void AddApp(string appName)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Apps (AppName, IsDefault) VALUES (@name, 0)";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", appName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteApp(int appId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Apps WHERE Id = @id AND IsDefault = 0";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", appId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ProjectApp operations
        public void SetProjectApps(int projectId, List<int> appIds)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // Delete existing
                    string deleteSql = "DELETE FROM ProjectApps WHERE ProjectId = @projectId";
                    using (var cmd = new SQLiteCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@projectId", projectId);
                        cmd.ExecuteNonQuery();
                    }

                    // Insert new
                    foreach (var appId in appIds)
                    {
                        string insertSql = "INSERT INTO ProjectApps (ProjectId, AppId) VALUES (@projectId, @appId)";
                        using (var cmd = new SQLiteCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@projectId", projectId);
                            cmd.Parameters.AddWithValue("@appId", appId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        private List<int> GetProjectAppIds(SQLiteConnection conn, int projectId)
        {
            var appIds = new List<int>();
            string sql = "SELECT AppId FROM ProjectApps WHERE ProjectId = @projectId";
            
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@projectId", projectId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        appIds.Add(reader.GetInt32(0));
                    }
                }
            }

            return appIds;
        }

        // Task operations
        public void AddTask(TodoTask task)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO Tasks (ProjectId, AppName, TaskDescription, IsCompleted, Priority)
                              VALUES (@projectId, @appName, @desc, @completed, @priority)";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", task.ProjectId);
                    cmd.Parameters.AddWithValue("@appName", task.AppName ?? "");
                    cmd.Parameters.AddWithValue("@desc", task.TaskDescription);
                    cmd.Parameters.AddWithValue("@completed", task.IsCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@priority", task.Priority ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTask(TodoTask task)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = @"UPDATE Tasks SET AppName = @appName, TaskDescription = @desc, 
                              IsCompleted = @completed, Priority = @priority WHERE Id = @id";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@appName", task.AppName ?? "");
                    cmd.Parameters.AddWithValue("@desc", task.TaskDescription);
                    cmd.Parameters.AddWithValue("@completed", task.IsCompleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@priority", task.Priority ?? "");
                    cmd.Parameters.AddWithValue("@id", task.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTask(int taskId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Tasks WHERE Id = @id";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteProject(int projectId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete related ProjectApps
                        using (var cmd = new SQLiteCommand("DELETE FROM ProjectApps WHERE ProjectId = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", projectId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete related Tasks
                        using (var cmd = new SQLiteCommand("DELETE FROM Tasks WHERE ProjectId = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", projectId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete related Datasheets
                        using (var cmd = new SQLiteCommand("DELETE FROM ProjectDatasheets WHERE ProjectId = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", projectId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete the project
                        using (var cmd = new SQLiteCommand("DELETE FROM Projects WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", projectId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<TodoTask> GetTasksForProject(int projectId)
        {
            var tasks = new List<TodoTask>();
            
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Tasks WHERE ProjectId = @projectId ORDER BY IsCompleted, AppName";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TodoTask
                            {
                                Id = reader.GetInt32(0),
                                ProjectId = reader.GetInt32(1),
                                AppName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TaskDescription = reader.GetString(3),
                                IsCompleted = reader.GetInt32(4) == 1,
                                Priority = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return tasks;
        }

        // Datasheet operations
        public void AddDatasheetToProject(int projectId, string datasheetPath)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT OR IGNORE INTO ProjectDatasheets (ProjectId, DatasheetPath) VALUES (@projectId, @path)";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@path", datasheetPath);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RemoveDatasheetFromProject(int projectId, string datasheetPath)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM ProjectDatasheets WHERE ProjectId = @projectId AND DatasheetPath = @path";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    cmd.Parameters.AddWithValue("@path", datasheetPath);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<string> GetDatasheetsForProject(int projectId)
        {
            var datasheets = new List<string>();
            
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT DatasheetPath FROM ProjectDatasheets WHERE ProjectId = @projectId";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            datasheets.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return datasheets;
        }

        // Settings operations
        public void SaveSetting(string key, string value)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @value)";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT Value FROM Settings WHERE Key = @key";
                
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    var result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : defaultValue;
                }
            }
        }
    }
}
