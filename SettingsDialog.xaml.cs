using System.Windows;
using ProjectManager.Data;

namespace ProjectManager
{
    public partial class SettingsDialog : Window
    {
        private DatabaseManager db;

        public SettingsDialog(DatabaseManager database)
        {
            InitializeComponent();
            db = database;

            txtEngineeringRoot.Text = db.GetSetting("EngineeringRoot", @"D:\Engineering");
            txtDatasheetsPath.Text = db.GetSetting("DatasheetsPath", "");
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Engineering Root Directory",
                SelectedPath = txtEngineeringRoot.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtEngineeringRoot.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseDatasheets_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Datasheets Directory",
                SelectedPath = txtDatasheetsPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtDatasheetsPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEngineeringRoot.Text))
            {
                MessageBox.Show("Please select a valid Engineering Root directory.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            db.SaveSetting("EngineeringRoot", txtEngineeringRoot.Text);
            db.SaveSetting("DatasheetsPath", txtDatasheetsPath.Text);
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
