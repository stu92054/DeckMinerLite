using DeckMiner.Config;
using DeckMiner.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeckMiner.Gui
{
    public partial class FanLevelsWindow : Window
    {
        private readonly MemberConfig _config;
        private readonly Dictionary<int, TextBox> _fanLevelTextBoxes = new();

        public FanLevelsWindow(MemberConfig config)
        {
            InitializeComponent();
            _config = config;

            // 確保 FanLevels 已初始化
            if (_config.FanLevels == null)
            {
                _config.FanLevels = new Dictionary<int, int>();
            }

            // 初始化所有角色的粉絲等級（預設為 10）
            foreach (var characterId in GameConstants.CharacterNames.Keys)
            {
                if (!_config.FanLevels.ContainsKey(characterId))
                {
                    _config.FanLevels[characterId] = 10;
                }
            }

            BuildFanLevelControls();
        }

        private void BuildFanLevelControls()
        {
            FanLevelsPanel.Children.Clear();
            _fanLevelTextBoxes.Clear();

            foreach (var kvp in GameConstants.CharacterNames.OrderBy(x => x.Key))
            {
                int characterId = kvp.Key;
                string characterName = kvp.Value;

                // 創建每個角色的粉絲等級輸入行
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // 角色 ID
                var idText = new TextBlock
                {
                    Text = characterId.ToString(),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(idText, 0);
                grid.Children.Add(idText);

                // 角色名稱
                var nameText = new TextBlock
                {
                    Text = characterName,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(nameText, 1);
                grid.Children.Add(nameText);

                // 粉絲等級輸入框
                int currentLevel = _config.FanLevels.ContainsKey(characterId) ? _config.FanLevels[characterId] : 10;
                var levelTextBox = new TextBox
                {
                    Text = currentLevel.ToString(),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(levelTextBox, 2);
                grid.Children.Add(levelTextBox);

                _fanLevelTextBoxes[characterId] = levelTextBox;
                FanLevelsPanel.Children.Add(grid);
            }
        }

        private void SetAllTo10Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var textBox in _fanLevelTextBoxes.Values)
            {
                textBox.Text = "10";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 驗證並更新所有粉絲等級
                foreach (var kvp in _fanLevelTextBoxes)
                {
                    int characterId = kvp.Key;
                    string text = kvp.Value.Text.Trim();

                    if (string.IsNullOrEmpty(text))
                    {
                        MessageBox.Show(
                            $"角色 {characterId} ({GameConstants.GetCharacterName(characterId)}) 的粉絲等級不能為空",
                            "驗證錯誤",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        kvp.Value.Focus();
                        return;
                    }

                    if (!int.TryParse(text, out int level))
                    {
                        MessageBox.Show(
                            $"角色 {characterId} ({GameConstants.GetCharacterName(characterId)}) 的粉絲等級必須是整數",
                            "驗證錯誤",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        kvp.Value.Focus();
                        return;
                    }

                    if (level < 0 || level > 10)
                    {
                        MessageBox.Show(
                            $"角色 {characterId} ({GameConstants.GetCharacterName(characterId)}) 的粉絲等級必須在 0-10 之間",
                            "驗證錯誤",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        kvp.Value.Focus();
                        return;
                    }

                    _config.FanLevels[characterId] = level;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"更新粉絲等級時發生錯誤：\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
