using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ProjectManager.Models
{
    public class Project : INotifyPropertyChanged
    {
        private int _id;
        private string _projectNumber;
        private string _projectName;
        private int _revisionNumber;
        private DateTime _dateCreated;
        private string _status;
        private string _issues;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string ProjectNumber
        {
            get => _projectNumber;
            set { _projectNumber = value; OnPropertyChanged(nameof(ProjectNumber)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(nameof(ProjectName)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public int RevisionNumber
        {
            get => _revisionNumber;
            set { _revisionNumber = value; OnPropertyChanged(nameof(RevisionNumber)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public DateTime DateCreated
        {
            get => _dateCreated;
            set { _dateCreated = value; OnPropertyChanged(nameof(DateCreated)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public string Issues
        {
            get => _issues;
            set { _issues = value; OnPropertyChanged(nameof(Issues)); }
        }

        public string DisplayName => $"{ProjectNumber}_{ProjectName}_Rev{RevisionNumber}";
        
        public string FolderName => DisplayName;

        public List<int> AppIds { get; set; } = new List<int>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppInfo
    {
        public int Id { get; set; }
        public string AppName { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ProjectApp
    {
        public int ProjectId { get; set; }
        public int AppId { get; set; }
    }

    public class TodoTask : INotifyPropertyChanged
    {
        private int _id;
        private int _projectId;
        private string _appName;
        private string _taskDescription;
        private bool _isCompleted;
        private string _priority;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int ProjectId
        {
            get => _projectId;
            set { _projectId = value; OnPropertyChanged(nameof(ProjectId)); }
        }

        public string AppName
        {
            get => _appName;
            set { _appName = value; OnPropertyChanged(nameof(AppName)); }
        }

        public string TaskDescription
        {
            get => _taskDescription;
            set { _taskDescription = value; OnPropertyChanged(nameof(TaskDescription)); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); }
        }

        public string Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(nameof(Priority)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProjectDatasheet
    {
        public int ProjectId { get; set; }
        public string DatasheetPath { get; set; }
    }

    public class DatasheetInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string FileSize { get; set; }
        public DateTime DateModified { get; set; }
    }

    public class FileTodoItem
    {
        public int LineNumber { get; set; }
        public bool IsCompleted { get; set; }
        public string Status => IsCompleted ? "[x]" : "[ ]";
        public string Timestamp { get; set; }
        public string Description { get; set; }
        public string RawLine { get; set; }
    }
}
