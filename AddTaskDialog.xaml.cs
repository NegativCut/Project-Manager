using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ProjectManager
{
    public partial class AddTaskDialog : Window
    {
        public string SelectedApp { get; private set; }
        public string TaskDescription { get; private set; }
        public string Priority { get; private set; }

        public AddTaskDialog(List<string> appNames)
        {
            InitializeComponent();

            cmbApp.Items.Add("General");
            foreach (var app in appNames)
            {
                cmbApp.Items.Add(app);
            }
            cmbApp.SelectedIndex = 0;

            txtTask.Focus();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTask.Text))
            {
                MessageBox.Show("Please enter a task description.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedApp = cmbApp.SelectedItem.ToString();
            TaskDescription = txtTask.Text;
            Priority = ((ComboBoxItem)cmbPriority.SelectedItem).Content.ToString();

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
