using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace DeckMiner.Gui
{
    public partial class NewConfigDialog : Window
    {
        public string MemberName { get; private set; }
        public string SavePath { get; private set; }
        private string _configDirectory;

        public NewConfigDialog()
        {
            InitializeComponent();

            // Get config directory
            _configDirectory = GetConfigDirectory();

            // Set default save path
            UpdateSavePath();
        }

        private void MemberNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateSavePath();
        }

        private void UpdateSavePath()
        {
            string memberName = MemberNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(memberName))
            {
                SavePathTextBox.Text = Path.Combine(_configDirectory, "member-new.yaml");
            }
            else
            {
                // 移除不合法的檔案名稱字元
                string safeName = string.Join("", memberName.Split(Path.GetInvalidFileNameChars()));
                SavePathTextBox.Text = Path.Combine(_configDirectory, $"member-{safeName}.yaml");
            }
        }

        private string GetConfigDirectory()
        {
            string baseDir = AppContext.BaseDirectory;

            string[] possiblePaths = new[]
            {
                Path.Combine(baseDir, "config"),
                Path.Combine(baseDir, "..", "..", "..", "..", "config")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (Directory.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch
                {
                    // Ignore invalid paths
                }
            }

            return baseDir;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "選擇配置檔儲存位置",
                Filter = "YAML Files (*.yaml)|*.yaml|All Files (*.*)|*.*",
                DefaultExt = ".yaml",
                InitialDirectory = GetConfigDirectory()
            };

            if (!string.IsNullOrEmpty(SavePathTextBox.Text))
            {
                try
                {
                    dialog.FileName = Path.GetFileName(SavePathTextBox.Text);
                    string dir = Path.GetDirectoryName(SavePathTextBox.Text);
                    if (Directory.Exists(dir))
                    {
                        dialog.InitialDirectory = dir;
                    }
                }
                catch
                {
                    // Use default if current path is invalid
                }
            }

            if (dialog.ShowDialog() == true)
            {
                SavePathTextBox.Text = dialog.FileName;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate member name
            if (string.IsNullOrWhiteSpace(MemberNameTextBox.Text))
            {
                MessageBox.Show(
                    "請輸入成員名稱",
                    "驗證錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                MemberNameTextBox.Focus();
                return;
            }

            // Validate save path
            if (string.IsNullOrWhiteSpace(SavePathTextBox.Text))
            {
                MessageBox.Show(
                    "請選擇配置檔儲存位置",
                    "驗證錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Check if file already exists
            if (File.Exists(SavePathTextBox.Text))
            {
                var result = MessageBox.Show(
                    $"檔案已存在：\n{SavePathTextBox.Text}\n\n是否覆蓋？",
                    "確認覆蓋",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Ensure directory exists
            try
            {
                string directory = Path.GetDirectoryName(SavePathTextBox.Text);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"無法建立目錄：\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            MemberName = MemberNameTextBox.Text.Trim();
            SavePath = SavePathTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
