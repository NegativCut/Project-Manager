using System.Linq;
using System.Windows;
using ProjectManager.Data;
using ProjectManager.Models;

namespace ProjectManager
{
    public partial class ManageAppsDialog : Window
    {
        private DatabaseManager db;

        public ManageAppsDialog(DatabaseManager database)
        {
            InitializeComponent();
            db = database;
            LoadApps();
        }

        private void LoadApps()
        {
            var apps = db.GetAllApps();
            lstApps.ItemsSource = apps;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string appName = txtNewApp.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(appName))
            {
                MessageBox.Show("Please enter an application name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                db.AddApp(appName);
                txtNewApp.Clear();
                LoadApps();
                MessageBox.Show($"Added '{appName}' successfully.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("This application name already exists.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstApps.SelectedItem is AppInfo app)
            {
                if (app.IsDefault)
                {
                    MessageBox.Show("Cannot delete default applications.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete '{app.AppName}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    db.DeleteApp(app.Id);
                    LoadApps();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
