using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjectManager.Data;
using ProjectManager.Models;

namespace ProjectManager
{
    public partial class MainWindow : Window
    {
        private DatabaseManager db;
        private List<Project> allProjects;
        private Project currentProject;
        private string engineeringRoot;
        private string datasheetsPath;
        private List<AppInfo> availableApps;

        public MainWindow()
        {
            InitializeComponent();
            db = new DatabaseManager();
            
            // Load or prompt for engineering root
            engineeringRoot = db.GetSetting("EngineeringRoot");
            if (string.IsNullOrEmpty(engineeringRoot))
            {
                engineeringRoot = @"D:\Engineering";
                db.SaveSetting("EngineeringRoot", engineeringRoot);
            }

            // Load datasheets path (can be separate from engineering root)
            datasheetsPath = db.GetSetting("DatasheetsPath");
            if (string.IsNullOrEmpty(datasheetsPath))
            {
                datasheetsPath = Path.Combine(engineeringRoot, "DATASHEETS");
            }

            // Try to ensure directories exist, but don't crash if we can't
            try
            {
                if (!Directory.Exists(engineeringRoot))
                {
                    Directory.CreateDirectory(engineeringRoot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create engineering directory at '{engineeringRoot}'.\n\nPlease go to Settings and configure a valid path.\n\nError: {ex.Message}",
                    "Directory Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            LoadData();
        }

        private void LoadData()
        {
            availableApps = db.GetAllApps();
            allProjects = db.GetAllProjects();
            lstProjects.ItemsSource = allProjects;
            
            if (allProjects.Count > 0)
            {
                lstProjects.SelectedIndex = 0;
            }
        }

        private void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProjects.SelectedItem is Project project)
            {
                currentProject = project;
                LoadProjectDetails(project);
            }
        }

        private void LoadProjectDetails(Project project)
        {
            txtProjectNumber.Text = project.DisplayName;
            txtProjectName.Text = project.ProjectName;
            txtDateCreated.Text = project.DateCreated.ToString("yyyy-MM-dd HH:mm");
            cmbStatus.Text = project.Status;
            txtIssues.Text = project.Issues;

            LoadAppsUsed(project);
            LoadFiles(project);
            LoadDatasheets(project);
            LoadTasks(project);
        }

        private void LoadAppsUsed(Project project)
        {
            pnlAppsUsed.Children.Clear();
            
            foreach (var app in availableApps)
            {
                var checkbox = new CheckBox
                {
                    Content = app.AppName,
                    IsChecked = project.AppIds.Contains(app.Id),
                    Tag = app.Id,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                checkbox.Checked += AppCheckbox_Changed;
                checkbox.Unchecked += AppCheckbox_Changed;
                pnlAppsUsed.Children.Add(checkbox);
            }
        }

        private void AppCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            var selectedAppIds = new List<int>();
            foreach (CheckBox cb in pnlAppsUsed.Children)
            {
                if (cb.IsChecked == true)
                {
                    selectedAppIds.Add((int)cb.Tag);
                }
            }

            currentProject.AppIds = selectedAppIds;
            db.SetProjectApps(currentProject.Id, selectedAppIds);

            // Create/remove folders as needed
            UpdateProjectFolders(currentProject);
            LoadFiles(currentProject);
        }

        private void LoadFiles(Project project)
        {
            pnlFiles.Children.Clear();

            var projectApps = availableApps.Where(a => project.AppIds.Contains(a.Id)).ToList();

            foreach (var app in projectApps)
            {
                var groupBox = new GroupBox
                {
                    Header = app.AppName,
                    Margin = new Thickness(0, 5, 0, 10)
                };

                var panel = new StackPanel();
                
                string appPath = Path.Combine(engineeringRoot, app.AppName, project.FolderName);
                
                if (Directory.Exists(appPath))
                {
                    var btnOpen = new Button
                    {
                        Content = $"Open {app.AppName} Folder",
                        Margin = new Thickness(5),
                        Tag = appPath,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Width = 200
                    };
                    btnOpen.Click += (s, e) => OpenFolder((string)((Button)s).Tag);
                    panel.Children.Add(btnOpen);

                    var files = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                    
                    if (files.Length == 0)
                    {
                        var lblNoFiles = new TextBlock
                        {
                            Text = "No files in this folder",
                            Foreground = System.Windows.Media.Brushes.White,
                            Margin = new Thickness(5)
                        };
                        panel.Children.Add(lblNoFiles);
                    }
                    else
                    {
                        foreach (var file in files.Take(20)) // Limit display
                        {
                            var fileBtn = new Button
                            {
                                Content = Path.GetFileName(file),
                                Margin = new Thickness(5, 2, 5, 2),
                                Tag = file,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Background = System.Windows.Media.Brushes.Transparent,
                                BorderThickness = new Thickness(0),
                                Foreground = System.Windows.Media.Brushes.White,
                                Cursor = Cursors.Hand
                            };
                            fileBtn.Click += (s, e) => OpenFile((string)((Button)s).Tag);
                            panel.Children.Add(fileBtn);
                        }

                        if (files.Length > 20)
                        {
                            var lblMore = new TextBlock
                            {
                                Text = $"... and {files.Length - 20} more files",
                                Margin = new Thickness(5),
                                Foreground = System.Windows.Media.Brushes.White
                            };
                            panel.Children.Add(lblMore);
                        }
                    }
                }
                else
                {
                    var lblNotCreated = new TextBlock
                    {
                        Text = "Folder not created yet",
                        Foreground = System.Windows.Media.Brushes.White,
                        Margin = new Thickness(5)
                    };
                    panel.Children.Add(lblNotCreated);
                }

                groupBox.Content = panel;
                pnlFiles.Children.Add(groupBox);
            }
        }

        private void LoadDatasheets(Project project)
        {
            var datasheetPaths = db.GetDatasheetsForProject(project.Id);
            var datasheetInfos = new List<DatasheetInfo>();

            foreach (var path in datasheetPaths)
            {
                // Try as relative path first, then as absolute path
                string fullPath = Path.IsPathRooted(path) ? path : Path.Combine(datasheetsPath, path);

                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    datasheetInfos.Add(new DatasheetInfo
                    {
                        FileName = Path.GetFileName(path),
                        FullPath = fullPath,
                        FileSize = FormatFileSize(fileInfo.Length),
                        DateModified = fileInfo.LastWriteTime
                    });
                }
                else
                {
                    // Show entry even if file not found
                    datasheetInfos.Add(new DatasheetInfo
                    {
                        FileName = Path.GetFileName(path) + " (not found)",
                        FullPath = fullPath,
                        FileSize = "-",
                        DateModified = DateTime.MinValue
                    });
                }
            }

            dgDatasheets.ItemsSource = datasheetInfos;
        }

        private void LoadTasks(Project project)
        {
            var todos = new List<FileTodoItem>();

            try
            {
                string filePath = GetOverviewFilePath();
                if (filePath != null && File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    bool inTodoSection = false;
                    bool foundTodoHeader = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();

                        // Detect TODO section: look for line that says "TODO" (between === dividers)
                        if (line == "TODO")
                        {
                            foundTodoHeader = true;
                            continue;
                        }

                        // After finding TODO header, the next === line starts the content area
                        if (foundTodoHeader && line.StartsWith("==="))
                        {
                            inTodoSection = true;
                            foundTodoHeader = false;
                            continue;
                        }

                        // Detect end of TODO section (next section header with === or ---)
                        if (inTodoSection && (line.StartsWith("===") || line.StartsWith("---")))
                        {
                            break;
                        }

                        // Parse TODO items: [ ] [timestamp] description or [x] [timestamp] description
                        if (inTodoSection)
                        {
                            string trimmed = lines[i].TrimStart();
                            if (trimmed.StartsWith("[ ]") || trimmed.StartsWith("[x]") || trimmed.StartsWith("[X]"))
                            {
                                var todo = ParseTodoLine(lines[i], i);
                                if (todo != null)
                                {
                                    todos.Add(todo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error loading tasks: {ex.Message}";
            }

            dgTasks.ItemsSource = todos;
        }

        private FileTodoItem ParseTodoLine(string line, int lineNumber)
        {
            string trimmed = line.TrimStart();
            bool isCompleted = trimmed.StartsWith("[x]") || trimmed.StartsWith("[X]");

            // Remove the checkbox part
            string rest = trimmed.Substring(3).TrimStart();

            // Try to extract timestamp [yyyy-MM-dd HH:mm]
            string timestamp = "";
            string description = rest;

            if (rest.StartsWith("["))
            {
                int endBracket = rest.IndexOf(']');
                if (endBracket > 0)
                {
                    timestamp = rest.Substring(1, endBracket - 1);
                    description = rest.Substring(endBracket + 1).TrimStart();
                }
            }

            return new FileTodoItem
            {
                LineNumber = lineNumber,
                IsCompleted = isCompleted,
                Timestamp = timestamp,
                Description = description,
                RawLine = line
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void BtnNewProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NewProjectDialog(db, availableApps);
            if (dialog.ShowDialog() == true)
            {
                var project = dialog.NewProject;
                
                // Create project folders
                CreateProjectFolders(project);
                
                // Save to database
                int projectId = db.CreateProject(project);
                project.Id = projectId;
                
                // Save app associations
                db.SetProjectApps(projectId, project.AppIds);
                
                // Reload
                LoadData();
                
                // Select new project
                lstProjects.SelectedItem = allProjects.FirstOrDefault(p => p.Id == projectId);
                
                txtStatus.Text = $"Created project: {project.DisplayName}";
            }
        }

        private void CreateProjectFolders(Project project)
        {
            foreach (var appId in project.AppIds)
            {
                var app = availableApps.FirstOrDefault(a => a.Id == appId);
                if (app != null)
                {
                    string appFolder = Path.Combine(engineeringRoot, app.AppName);
                    if (!Directory.Exists(appFolder))
                        Directory.CreateDirectory(appFolder);

                    string projectFolder = Path.Combine(appFolder, project.FolderName);
                    if (!Directory.Exists(projectFolder))
                        Directory.CreateDirectory(projectFolder);
                }
            }
        }

        private void UpdateProjectFolders(Project project)
        {
            // Create folders for newly selected apps
            CreateProjectFolders(project);
        }

        private void BtnNewRevision_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new NewRevisionDialog(currentProject, availableApps, db);
            if (dialog.ShowDialog() == true)
            {
                var newRevision = dialog.NewRevision;
                
                // Create folders
                CreateProjectFolders(newRevision);
                
                // Copy selected files
                CopyRevisionFiles(currentProject, newRevision, dialog.SelectedAppIds);
                
                // Save to database
                int projectId = db.CreateProject(newRevision);
                newRevision.Id = projectId;
                db.SetProjectApps(projectId, newRevision.AppIds);
                
                // Reload
                LoadData();
                lstProjects.SelectedItem = allProjects.FirstOrDefault(p => p.Id == projectId);
                
                txtStatus.Text = $"Created revision: {newRevision.DisplayName}";
            }
        }

        private void CopyRevisionFiles(Project oldProject, Project newProject, List<int> appIdsToCopy)
        {
            foreach (var appId in appIdsToCopy)
            {
                var app = availableApps.FirstOrDefault(a => a.Id == appId);
                if (app != null)
                {
                    string oldPath = Path.Combine(engineeringRoot, app.AppName, oldProject.FolderName);
                    string newPath = Path.Combine(engineeringRoot, app.AppName, newProject.FolderName);

                    if (Directory.Exists(oldPath))
                    {
                        CopyDirectory(oldPath, newPath);
                    }
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetDirectoryName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private void BtnManageApps_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ManageAppsDialog(db);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                if (currentProject != null)
                {
                    LoadProjectDetails(currentProject);
                }
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(db);
            if (dialog.ShowDialog() == true)
            {
                engineeringRoot = db.GetSetting("EngineeringRoot");
                datasheetsPath = db.GetSetting("DatasheetsPath");
                if (string.IsNullOrEmpty(datasheetsPath))
                {
                    datasheetsPath = Path.Combine(engineeringRoot, "DATASHEETS");
                }
                LoadData();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            txtStatus.Text = "Refreshed";
        }

        private void BtnTodoFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("==============================================================================");
                sb.AppendLine("OUTSTANDING TODO ITEMS");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("==============================================================================");
                sb.AppendLine();

                // Get all projects ordered by project number descending (highest first)
                var projects = db.GetAllProjects();
                int totalOutstanding = 0;

                foreach (var project in projects)
                {
                    // Get the overview file path for this project
                    string documentsPath = Path.Combine(engineeringRoot, "Documents", project.FolderName);
                    string overviewPath = Path.Combine(documentsPath, $"{project.FolderName}_Overview.txt");

                    var outstandingTodos = new List<string>();

                    if (File.Exists(overviewPath))
                    {
                        string[] lines = File.ReadAllLines(overviewPath);
                        bool inTodoSection = false;
                        bool foundTodoHeader = false;

                        foreach (string line in lines)
                        {
                            string trimmed = line.Trim();

                            // Detect TODO section header
                            if (trimmed == "TODO")
                            {
                                foundTodoHeader = true;
                                continue;
                            }

                            // After finding TODO header, the next === line starts the content area
                            if (foundTodoHeader && trimmed.StartsWith("==="))
                            {
                                inTodoSection = true;
                                foundTodoHeader = false;
                                continue;
                            }

                            // Detect end of TODO section
                            if (inTodoSection && (trimmed.StartsWith("===") || trimmed.StartsWith("---")))
                            {
                                break;
                            }

                            // Parse only incomplete TODO items: [ ]
                            if (inTodoSection)
                            {
                                string lineTrimmed = line.TrimStart();
                                if (lineTrimmed.StartsWith("[ ]"))
                                {
                                    // Extract the todo text (remove [ ] prefix)
                                    string todoText = lineTrimmed.Substring(3).TrimStart();
                                    outstandingTodos.Add(todoText);
                                }
                            }
                        }
                    }

                    // Only add project to output if it has outstanding todos
                    if (outstandingTodos.Count > 0)
                    {
                        sb.AppendLine("------------------------------------------------------------------------------");
                        sb.AppendLine($"{project.DisplayName}");
                        sb.AppendLine("------------------------------------------------------------------------------");

                        foreach (var todo in outstandingTodos)
                        {
                            // Format: "  [ ] [timestamp] description"
                            // Wrap long descriptions so continuation lines align with description start
                            string prefix = "  [ ] ";
                            string fullLine = prefix + todo;

                            // Find where the description starts (after the timestamp)
                            // Timestamp format: [yyyy-MM-dd HH:mm] = 18 chars + space = 19 chars
                            int descStartPos = prefix.Length;
                            if (todo.StartsWith("["))
                            {
                                int closeBracket = todo.IndexOf(']');
                                if (closeBracket > 0)
                                {
                                    descStartPos = prefix.Length + closeBracket + 2; // +2 for ] and space
                                }
                            }

                            const int maxLineWidth = 78;
                            string indent = new string(' ', descStartPos);

                            if (fullLine.Length <= maxLineWidth)
                            {
                                sb.AppendLine(fullLine);
                            }
                            else
                            {
                                // Word wrap with aligned continuation
                                sb.AppendLine(WrapTodoLine(fullLine, maxLineWidth, indent));
                            }
                            totalOutstanding++;
                        }

                        sb.AppendLine();
                    }
                }

                if (totalOutstanding == 0)
                {
                    sb.AppendLine("No outstanding TODO items found.");
                }

                // Save to file in engineering root
                string todoFilePath = Path.Combine(engineeringRoot, "OutstandingTodos.txt");
                File.WriteAllText(todoFilePath, sb.ToString());

                // Open the file
                Process.Start(new ProcessStartInfo(todoFilePath) { UseShellExecute = true });
                txtStatus.Text = $"Todo file generated: {totalOutstanding} outstanding items";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not generate todo file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only handle if it's the main tabControl and we have a current project
            if (e.Source != tabControl || currentProject == null) return;

            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab?.Header?.ToString() == "To-Do List")
            {
                LoadTasks(currentProject);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allProjects == null) return;

            string searchText = txtSearch.Text.ToLower();
            
            if (string.IsNullOrEmpty(searchText) || searchText == "search projects...")
            {
                lstProjects.ItemsSource = allProjects;
            }
            else
            {
                var filtered = allProjects.Where(p => 
                    p.DisplayName.ToLower().Contains(searchText) ||
                    p.ProjectName.ToLower().Contains(searchText) ||
                    p.ProjectNumber.Contains(searchText)
                ).ToList();
                
                lstProjects.ItemsSource = filtered;
            }
        }

        private void TxtProjectName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                currentProject.ProjectName = txtProjectName.Text;
                db.UpdateProject(currentProject);
                LoadData();
            }
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentProject != null && cmbStatus.SelectedItem != null)
            {
                string newStatus = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString();
                string oldStatus = currentProject.Status;

                currentProject.Status = newStatus;
                db.UpdateProject(currentProject);

                // Record status change to overview file
                if (oldStatus != newStatus)
                {
                    RecordStatusChange(oldStatus, newStatus);
                }

                LoadData();
            }
        }

        private void RecordStatusChange(string oldStatus, string newStatus)
        {
            try
            {
                EnsureOverviewFileExists();
                string filePath = GetOverviewFilePath();
                if (filePath == null || !File.Exists(filePath)) return;

                string content = File.ReadAllText(filePath);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string statusEntry = $"[{timestamp}] Status changed: {oldStatus ?? "None"} -> {newStatus}";

                // Check if STATUS HISTORY section exists
                if (content.Contains("STATUS HISTORY"))
                {
                    // Find the STATUS HISTORY section and append after the divider line
                    int historyIndex = content.IndexOf("STATUS HISTORY");
                    int dividerIndex = content.IndexOf("------------------------------------------------------------------------------", historyIndex);
                    if (dividerIndex != -1)
                    {
                        int insertPos = dividerIndex + "------------------------------------------------------------------------------".Length;
                        content = content.Insert(insertPos, "\n" + statusEntry);
                    }
                }
                else
                {
                    // Add STATUS HISTORY section before the end
                    string historySection = $@"
------------------------------------------------------------------------------
STATUS HISTORY
------------------------------------------------------------------------------
{statusEntry}
";
                    content += historySection;
                }

                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Could not record status change: {ex.Message}";
            }
        }

        private void TxtIssues_LostFocus(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                currentProject.Issues = txtIssues.Text;
                db.UpdateProject(currentProject);
            }
        }

        private void BtnOpenProjectRoot_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(engineeringRoot);
        }

        private string GetOverviewFilePath()
        {
            if (currentProject == null) return null;

            // Put the overview file in the Documents folder for this project
            string documentsPath = Path.Combine(engineeringRoot, "Documents", currentProject.FolderName);
            return Path.Combine(documentsPath, $"{currentProject.FolderName}_Overview.txt");
        }

        private void EnsureOverviewFileExists()
        {
            string filePath = GetOverviewFilePath();
            if (filePath == null) return;

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(filePath))
            {
                // Create new overview file with template
                var content = $@"==============================================================================
PROJECT OVERVIEW
==============================================================================
Project Number: {currentProject.ProjectNumber}
Project Name:   {currentProject.ProjectName}
Revision:       {currentProject.RevisionNumber}
Created:        {currentProject.DateCreated:yyyy-MM-dd}
Status:         {currentProject.Status}

------------------------------------------------------------------------------
DESCRIPTION
------------------------------------------------------------------------------

{GenerateFilesSection()}
{GenerateDatasheetsSection()}
------------------------------------------------------------------------------
NOTES
------------------------------------------------------------------------------


==============================================================================
TODO
==============================================================================


------------------------------------------------------------------------------
STATUS HISTORY
------------------------------------------------------------------------------

";
                File.WriteAllText(filePath, content);
            }
        }

        private string GenerateFilesSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine("FILES BY APPLICATION");
            sb.AppendLine("--------------------");

            var projectApps = availableApps.Where(a => currentProject.AppIds.Contains(a.Id)).ToList();

            foreach (var app in projectApps)
            {
                sb.AppendLine($"\n{app.AppName}:");
                string appPath = Path.Combine(engineeringRoot, app.AppName, currentProject.FolderName);

                if (Directory.Exists(appPath))
                {
                    var files = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                    if (files.Length == 0)
                    {
                        sb.AppendLine("  (No files)");
                    }
                    else
                    {
                        foreach (var file in files)
                        {
                            sb.AppendLine($"  - {Path.GetFileName(file)}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("  (Folder not created)");
                }
            }

            return sb.ToString();
        }

        private string GenerateDatasheetsSection()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\nDATASHEETS");
            sb.AppendLine("----------");

            var datasheetPaths = db.GetDatasheetsForProject(currentProject.Id);

            if (datasheetPaths.Count == 0)
            {
                sb.AppendLine("  (No datasheets linked)");
            }
            else
            {
                foreach (var path in datasheetPaths)
                {
                    string fileName = Path.GetFileName(path);
                    sb.AppendLine($"  - {fileName}");
                }
            }

            return sb.ToString();
        }

        private void BtnOpenOverview_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            try
            {
                EnsureOverviewFileExists();
                string filePath = GetOverviewFilePath();
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    txtStatus.Text = "Opened overview file";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open overview file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrintOverview_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            try
            {
                EnsureOverviewFileExists();
                string filePath = GetOverviewFilePath();
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true,
                        Verb = "print"
                    });
                    txtStatus.Text = "Printing overview file";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not print overview file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegenerateOverview_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            try
            {
                string filePath = GetOverviewFilePath();
                string existingTodoContent = "";
                string existingStatusHistory = "";
                string existingNotes = "";
                string existingDescription = "";

                // Extract existing content if file exists
                if (filePath != null && File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);

                    // Extract TODO items
                    int todoStart = content.IndexOf("TODO");
                    if (todoStart != -1)
                    {
                        int todoContentStart = content.IndexOf("===", todoStart);
                        if (todoContentStart != -1)
                        {
                            todoContentStart = content.IndexOf("\n", todoContentStart) + 1;
                            int todoEnd = content.IndexOf("---", todoContentStart);
                            if (todoEnd == -1) todoEnd = content.IndexOf("===", todoContentStart);
                            if (todoEnd == -1) todoEnd = content.Length;
                            existingTodoContent = content.Substring(todoContentStart, todoEnd - todoContentStart).Trim();
                        }
                    }

                    // Extract STATUS HISTORY
                    int statusStart = content.IndexOf("STATUS HISTORY");
                    if (statusStart != -1)
                    {
                        int statusContentStart = content.IndexOf("---", statusStart);
                        if (statusContentStart != -1)
                        {
                            statusContentStart = content.IndexOf("\n", statusContentStart) + 1;
                            int statusEnd = content.IndexOf("===", statusContentStart);
                            if (statusEnd == -1) statusEnd = content.Length;
                            existingStatusHistory = content.Substring(statusContentStart, statusEnd - statusContentStart).Trim();
                        }
                    }

                    // Extract NOTES
                    int notesStart = content.IndexOf("NOTES");
                    if (notesStart != -1)
                    {
                        int notesContentStart = content.IndexOf("---", notesStart);
                        if (notesContentStart != -1)
                        {
                            notesContentStart = content.IndexOf("\n", notesContentStart) + 1;
                            int notesEnd = content.IndexOf("===", notesContentStart);
                            if (notesEnd == -1) notesEnd = content.IndexOf("---", notesContentStart);
                            if (notesEnd == -1) notesEnd = content.Length;
                            existingNotes = content.Substring(notesContentStart, notesEnd - notesContentStart).Trim();
                        }
                    }

                    File.Delete(filePath);
                }

                // Create new file with preserved content
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var newContent = $@"==============================================================================
PROJECT OVERVIEW
==============================================================================
Project Number: {currentProject.ProjectNumber}
Project Name:   {currentProject.ProjectName}
Revision:       {currentProject.RevisionNumber}
Created:        {currentProject.DateCreated:yyyy-MM-dd}
Status:         {currentProject.Status}

------------------------------------------------------------------------------
DESCRIPTION
------------------------------------------------------------------------------

{GenerateFilesSection()}
{GenerateDatasheetsSection()}
------------------------------------------------------------------------------
NOTES
------------------------------------------------------------------------------
{(string.IsNullOrEmpty(existingNotes) ? "" : existingNotes + "\n")}

==============================================================================
TODO
==============================================================================
{(string.IsNullOrEmpty(existingTodoContent) ? "" : existingTodoContent + "\n")}

------------------------------------------------------------------------------
STATUS HISTORY
------------------------------------------------------------------------------
{(string.IsNullOrEmpty(existingStatusHistory) ? "" : existingStatusHistory + "\n")}
";
                File.WriteAllText(filePath, newContent);
                txtStatus.Text = "Overview file regenerated (TODO items and history preserved)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not regenerate overview file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshOverview_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            try
            {
                EnsureOverviewFileExists();
                string filePath = GetOverviewFilePath();
                if (filePath == null || !File.Exists(filePath)) return;

                string content = File.ReadAllText(filePath);

                // Find DESCRIPTION section and NOTES section
                int descIndex = content.IndexOf("DESCRIPTION");
                int notesIndex = content.IndexOf("NOTES");

                if (descIndex == -1 || notesIndex == -1)
                {
                    MessageBox.Show("Could not find DESCRIPTION or NOTES section in overview file.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Find the divider after DESCRIPTION
                int descDividerEnd = content.IndexOf("------------------------------------------------------------------------------", descIndex);
                if (descDividerEnd != -1)
                {
                    descDividerEnd = content.IndexOf("\n", descDividerEnd) + 1;
                }

                // Find the divider before NOTES
                int notesDividerStart = content.LastIndexOf("------------------------------------------------------------------------------", notesIndex);

                if (descDividerEnd != -1 && notesDividerStart != -1 && notesDividerStart > descDividerEnd)
                {
                    // Replace content between DESCRIPTION divider and NOTES divider
                    string newContent = content.Substring(0, descDividerEnd) +
                                       "\n" + GenerateFilesSection() +
                                       GenerateDatasheetsSection() +
                                       content.Substring(notesDividerStart);

                    File.WriteAllText(filePath, newContent);
                    txtStatus.Text = "Overview file refreshed with current files and datasheets";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not refresh overview file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddTodoItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            // Input dialog for TODO item
            var inputDialog = new Window
            {
                Title = "Add TODO Item",
                Width = 450,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E1E1E"))
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            var label = new TextBlock
            {
                Text = "Enter TODO item:",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var textBox = new TextBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2D2D30")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F3F46")),
                Height = 25,
                Margin = new Thickness(0, 0, 0, 15)
            };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button
            {
                Content = "Add",
                Width = 80,
                Height = 28,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0E639C")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsDefault = true
            };
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 28,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F3F46")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsCancel = true
            };

            okButton.Click += (s, args) => { inputDialog.DialogResult = true; };
            cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(label);
            stack.Children.Add(textBox);
            stack.Children.Add(buttonPanel);
            inputDialog.Content = stack;

            textBox.Focus();

            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                try
                {
                    EnsureOverviewFileExists();
                    string filePath = GetOverviewFilePath();
                    string todoItem = textBox.Text.Trim();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                    // Read current content and normalize line endings
                    string content = File.ReadAllText(filePath);

                    // Find the TODO section (handle both \n and \r\n line endings)
                    string todoHeaderUnix = "==============================================================================\nTODO\n==============================================================================";
                    string todoHeaderWin = "==============================================================================\r\nTODO\r\n==============================================================================";
                    int todoIndex = content.IndexOf(todoHeaderWin);
                    string headerUsed = todoHeaderWin;
                    if (todoIndex < 0)
                    {
                        todoIndex = content.IndexOf(todoHeaderUnix);
                        headerUsed = todoHeaderUnix;
                    }

                    if (todoIndex >= 0)
                    {
                        int insertPos = todoIndex + headerUsed.Length;
                        string newTodo = $"\n[ ] [{timestamp}] {todoItem}";
                        content = content.Insert(insertPos, newTodo);
                        File.WriteAllText(filePath, content);
                        LoadTasks(currentProject);
                        txtStatus.Text = $"Added TODO: {todoItem}";
                    }
                    else
                    {
                        MessageBox.Show("Could not find TODO section in overview file.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not add TODO item:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRenameProject_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            // Simple input dialog using a child window
            var inputDialog = new Window
            {
                Title = "Rename Project",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E1E1E"))
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            var label = new TextBlock
            {
                Text = "Enter new project name:",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var textBox = new TextBox
            {
                Text = currentProject.ProjectName,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2D2D30")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE0E0E0")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F3F46")),
                Height = 25,
                Margin = new Thickness(0, 0, 0, 15)
            };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button
            {
                Content = "Rename",
                Width = 80,
                Height = 28,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0E639C")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 28,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F3F46")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            okButton.Click += (s, args) => { inputDialog.DialogResult = true; };
            cancelButton.Click += (s, args) => { inputDialog.DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(label);
            stack.Children.Add(textBox);
            stack.Children.Add(buttonPanel);
            inputDialog.Content = stack;

            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string oldName = currentProject.ProjectName;
                string newName = textBox.Text.Trim();

                if (oldName != newName)
                {
                    currentProject.ProjectName = newName;
                    db.UpdateProject(currentProject);
                    LoadData();
                    txtStatus.Text = $"Project renamed from '{oldName}' to '{newName}'";
                }
            }
        }

        private void BtnDeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            // First confirmation - ask if they want to delete
            var result = MessageBox.Show(
                $"Are you sure you want to delete project '{currentProject.DisplayName}'?\n\n" +
                "This action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Second question - ask if they want to delete folders too
            var deleteFolders = MessageBox.Show(
                "Do you also want to delete the project folders from disk?\n\n" +
                "YES = Delete database record AND all project folders\n" +
                "NO = Delete database record only (keep folders)",
                "Delete Folders?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            string projectName = currentProject.DisplayName;
            int deletedFolders = 0;

            if (deleteFolders == MessageBoxResult.Yes)
            {
                // Delete project folders for each app
                foreach (var appId in currentProject.AppIds)
                {
                    var app = availableApps.FirstOrDefault(a => a.Id == appId);
                    if (app != null)
                    {
                        string folderPath = Path.Combine(engineeringRoot, app.AppName, currentProject.FolderName);
                        if (Directory.Exists(folderPath))
                        {
                            try
                            {
                                Directory.Delete(folderPath, true);
                                deletedFolders++;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Could not delete folder '{folderPath}':\n{ex.Message}",
                                    "Delete Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }

            // Delete from database
            db.DeleteProject(currentProject.Id);
            currentProject = null;
            LoadData();
            ClearProjectDetails();

            if (deleteFolders == MessageBoxResult.Yes)
                txtStatus.Text = $"Project '{projectName}' deleted ({deletedFolders} folders removed)";
            else
                txtStatus.Text = $"Project '{projectName}' removed from database (folders kept)";
        }

        private void ClearProjectDetails()
        {
            txtProjectNumber.Text = "";
            txtProjectName.Text = "";
            txtDateCreated.Text = "";
            cmbStatus.SelectedIndex = -1;
            txtIssues.Text = "";
            pnlAppsUsed.Children.Clear();
            pnlFiles.Children.Clear();
            dgDatasheets.ItemsSource = null;
            dgTasks.ItemsSource = null;
        }

        private void BtnRefreshFiles_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                LoadFiles(currentProject);
            }
        }

        private void BtnOpenAllFolders_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            foreach (var appId in currentProject.AppIds)
            {
                var app = availableApps.FirstOrDefault(a => a.Id == appId);
                if (app != null)
                {
                    string path = Path.Combine(engineeringRoot, app.AppName, currentProject.FolderName);
                    if (Directory.Exists(path))
                    {
                        OpenFolder(path);
                    }
                }
            }
        }

        private void BtnAddDatasheet_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Datasheets",
                InitialDirectory = datasheetsPath,
                Multiselect = true,
                Filter = "All Files (*.*)|*.*|PDF Files (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    string pathToStore;
                    // If file is inside datasheets folder, store relative path; otherwise store full path
                    if (file.StartsWith(datasheetsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        pathToStore = file.Substring(datasheetsPath.Length).TrimStart('\\', '/');
                    }
                    else
                    {
                        // Store full path for files outside the datasheets directory
                        pathToStore = file;
                    }

                    db.AddDatasheetToProject(currentProject.Id, pathToStore);
                }

                LoadDatasheets(currentProject);
                txtStatus.Text = "Datasheets linked";
            }
        }

        private void BtnRemoveDatasheet_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null || dgDatasheets.SelectedItem == null) return;

            var datasheet = (DatasheetInfo)dgDatasheets.SelectedItem;
            db.RemoveDatasheetFromProject(currentProject.Id, datasheet.FileName);
            LoadDatasheets(currentProject);
            txtStatus.Text = "Datasheet link removed";
        }

        private void BtnOpenDatasheetFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(datasheetsPath);
        }

        private void DgDatasheets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDatasheets.SelectedItem is DatasheetInfo datasheet)
            {
                OpenFile(datasheet.FullPath);
            }
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null) return;

            // Use the existing BtnAddTodoItem_Click logic
            BtnAddTodoItem_Click(sender, e);
            LoadTasks(currentProject);
        }

        private void BtnToggleTask_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null || dgTasks.SelectedItem == null) return;

            var todo = dgTasks.SelectedItem as FileTodoItem;
            if (todo == null) return;

            try
            {
                string filePath = GetOverviewFilePath();
                if (filePath != null && File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    if (todo.LineNumber < lines.Length)
                    {
                        string line = lines[todo.LineNumber];

                        // Toggle the checkbox
                        if (line.Contains("[ ]"))
                        {
                            lines[todo.LineNumber] = line.Replace("[ ]", "[x]");
                            txtStatus.Text = "Task marked as done";
                        }
                        else if (line.Contains("[x]") || line.Contains("[X]"))
                        {
                            lines[todo.LineNumber] = line.Replace("[x]", "[ ]").Replace("[X]", "[ ]");
                            txtStatus.Text = "Task marked as not done";
                        }

                        File.WriteAllLines(filePath, lines);
                        LoadTasks(currentProject);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not toggle task:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject == null || dgTasks.SelectedItem == null) return;

            var todo = dgTasks.SelectedItem as FileTodoItem;
            if (todo == null) return;

            var result = MessageBox.Show($"Delete task: {todo.Description}?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                string filePath = GetOverviewFilePath();
                if (filePath != null && File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath).ToList();

                    if (todo.LineNumber < lines.Count)
                    {
                        lines.RemoveAt(todo.LineNumber);
                        File.WriteAllLines(filePath, lines);
                        LoadTasks(currentProject);
                        txtStatus.Text = "Task deleted";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete task:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMoveTaskUp_Click(object sender, RoutedEventArgs e)
        {
            MoveTask(-1);
        }

        private void BtnMoveTaskDown_Click(object sender, RoutedEventArgs e)
        {
            MoveTask(1);
        }

        private void MoveTask(int direction)
        {
            if (currentProject == null || dgTasks.SelectedItem == null) return;

            var todo = dgTasks.SelectedItem as FileTodoItem;
            if (todo == null) return;

            int selectedIndex = dgTasks.SelectedIndex;

            try
            {
                string filePath = GetOverviewFilePath();
                if (filePath == null || !File.Exists(filePath)) return;

                var lines = File.ReadAllLines(filePath).ToList();
                var todos = dgTasks.ItemsSource as List<FileTodoItem>;
                if (todos == null) return;

                int targetIndex = selectedIndex + direction;

                // Check bounds
                if (targetIndex < 0 || targetIndex >= todos.Count) return;

                var targetTodo = todos[targetIndex];

                // Swap the lines in the file
                int lineA = todo.LineNumber;
                int lineB = targetTodo.LineNumber;

                if (lineA >= 0 && lineA < lines.Count && lineB >= 0 && lineB < lines.Count)
                {
                    string temp = lines[lineA];
                    lines[lineA] = lines[lineB];
                    lines[lineB] = temp;

                    File.WriteAllLines(filePath, lines);
                    LoadTasks(currentProject);

                    // Reselect the moved item
                    if (targetIndex >= 0 && targetIndex < dgTasks.Items.Count)
                    {
                        dgTasks.SelectedIndex = targetIndex;
                    }

                    txtStatus.Text = direction < 0 ? "Task moved up" : "Task moved down";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not move task:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshTasks_Click(object sender, RoutedEventArgs e)
        {
            if (currentProject != null)
            {
                LoadTasks(currentProject);
                txtStatus.Text = "Tasks refreshed";
            }
        }

        private void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path);
            }
        }

        private void OpenFile(string path)
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }

        private string WrapTodoLine(string line, int maxWidth, string continuationIndent)
        {
            var result = new StringBuilder();
            int currentPos = 0;
            bool firstLine = true;

            while (currentPos < line.Length)
            {
                int availableWidth = firstLine ? maxWidth : maxWidth - continuationIndent.Length;
                string currentLine = firstLine ? "" : continuationIndent;
                string remaining = line.Substring(currentPos);

                if (remaining.Length <= availableWidth)
                {
                    result.AppendLine(currentLine + remaining);
                    break;
                }

                // Find last space within available width
                int breakPos = remaining.LastIndexOf(' ', Math.Min(availableWidth, remaining.Length - 1));
                if (breakPos <= 0)
                {
                    // No space found, force break at max width
                    breakPos = availableWidth;
                }

                result.AppendLine(currentLine + remaining.Substring(0, breakPos).TrimEnd());
                currentPos += breakPos;

                // Skip the space we broke at
                while (currentPos < line.Length && line[currentPos] == ' ')
                    currentPos++;

                firstLine = false;
            }

            return result.ToString().TrimEnd('\r', '\n');
        }
    }
}
