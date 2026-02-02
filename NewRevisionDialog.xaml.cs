using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProjectManager.Data;
using ProjectManager.Models;

namespace ProjectManager
{
    public partial class NewRevisionDialog : Window
    {
        private Project baseProject;
        private List<AppInfo> apps;
        public Project NewRevision { get; private set; }
        public List<int> SelectedAppIds { get; private set; }

        public NewRevisionDialog(Project project, List<AppInfo> availableApps, DatabaseManager db)
        {
            InitializeComponent();
            baseProject = project;
            apps = availableApps;

            txtBaseProject.Text = project.DisplayName;
            txtNewRevision.Text = $"{project.ProjectNumber}_{project.ProjectName}_Rev{project.RevisionNumber + 1}";

            // Load apps that are currently used in the project
            var projectApps = apps.Where(a => project.AppIds.Contains(a.Id)).ToList();

            foreach (var app in projectApps)
            {
                var checkbox = new CheckBox
                {
                    Content = app.AppName,
                    Tag = app.Id,
                    IsChecked = true,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                pnlApps.Children.Add(checkbox);
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            SelectedAppIds = new List<int>();
            foreach (CheckBox cb in pnlApps.Children)
            {
                if (cb.IsChecked == true)
                {
                    SelectedAppIds.Add((int)cb.Tag);
                }
            }

            NewRevision = new Project
            {
                ProjectNumber = baseProject.ProjectNumber,
                ProjectName = baseProject.ProjectName,
                RevisionNumber = baseProject.RevisionNumber + 1,
                DateCreated = DateTime.Now,
                Status = "In Progress",
                AppIds = baseProject.AppIds // Keep same apps
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
