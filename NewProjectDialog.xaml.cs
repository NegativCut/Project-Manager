using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProjectManager.Data;
using ProjectManager.Models;

namespace ProjectManager
{
    public partial class NewProjectDialog : Window
    {
        private DatabaseManager db;
        private List<AppInfo> apps;
        public Project NewProject { get; private set; }

        public NewProjectDialog(DatabaseManager database, List<AppInfo> availableApps)
        {
            InitializeComponent();
            db = database;
            apps = availableApps;

            // Set next project number
            txtProjectNumber.Text = db.GetNextProjectNumber();

            // Load apps
            foreach (var app in apps)
            {
                var checkbox = new CheckBox
                {
                    Content = app.AppName,
                    Tag = app.Id,
                    Margin = new Thickness(0, 3, 0, 3)
                };
                pnlApps.Children.Add(checkbox);
            }

            txtProjectName.Focus();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProjectName.Text))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedAppIds = new List<int>();
            foreach (CheckBox cb in pnlApps.Children)
            {
                if (cb.IsChecked == true)
                {
                    selectedAppIds.Add((int)cb.Tag);
                }
            }

            if (selectedAppIds.Count == 0)
            {
                MessageBox.Show("Please select at least one application.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewProject = new Project
            {
                ProjectNumber = txtProjectNumber.Text,
                ProjectName = txtProjectName.Text,
                RevisionNumber = 1,
                DateCreated = DateTime.Now,
                Status = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString(),
                AppIds = selectedAppIds
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
