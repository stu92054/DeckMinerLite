using DeckMiner.Config;
using DeckMiner.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeckMiner.Gui
{
    public partial class OptimizerConfigWindow : Window
    {
        private readonly MemberConfig _config;
        private ObservableCollection<string> _forbiddenCardsDisplay = new();

        public OptimizerConfigWindow(MemberConfig config)
        {
            InitializeComponent();
            _config = config;

            // Ensure Optimizer is initialized
            if (_config.Optimizer == null)
            {
                _config.Optimizer = new OptimizerConfig();
            }

            LoadConfiguration();
            UpdateCardComboBox();
        }

        private void LoadConfiguration()
        {
            // Load Top N
            TopNTextBox.Text = _config.Optimizer.TopN.ToString();

            // Load Show Card Names
            ShowCardNamesCheckBox.IsChecked = _config.Optimizer.ShowCardNames;

            // Load Forbidden Cards
            UpdateForbiddenCardsDisplay();
            ForbiddenCardsListBox.ItemsSource = _forbiddenCardsDisplay;
        }

        private void UpdateCardComboBox()
        {
            var cardOptions = new ObservableCollection<ComboBoxItem>();
            cardOptions.Add(new ComboBoxItem { Content = "從卡池中選擇...", Tag = "" });

            var cardIds = _config.CardIds ?? new System.Collections.Generic.List<int>();

            foreach (var cardId in cardIds)
            {
                try
                {
                    var cardData = Services.DataManager.Instance.GetCardDatabase();
                    if (cardData.TryGetValue(cardId.ToString(), out var card))
                    {
                        var charName = GameConstants.GetCharacterName(card.CharactersId);
                        var rarityName = GameConstants.GetRarityString(card.Rarity);
                        var content = $"{cardId} - [{rarityName}] {charName} {card.Name}";
                        cardOptions.Add(new ComboBoxItem { Content = content, Tag = cardId.ToString() });
                    }
                    else
                    {
                        cardOptions.Add(new ComboBoxItem { Content = cardId.ToString(), Tag = cardId.ToString() });
                    }
                }
                catch
                {
                    cardOptions.Add(new ComboBoxItem { Content = cardId.ToString(), Tag = cardId.ToString() });
                }
            }

            ForbiddenCardsComboBox.ItemsSource = cardOptions;
            ForbiddenCardsComboBox.SelectedIndex = 0;
        }

        private void UpdateForbiddenCardsDisplay()
        {
            _forbiddenCardsDisplay.Clear();

            if (_config.Optimizer.ForbiddenCards == null || _config.Optimizer.ForbiddenCards.Count == 0)
            {
                return;
            }

            foreach (var cardId in _config.Optimizer.ForbiddenCards)
            {
                try
                {
                    var cardData = Services.DataManager.Instance.GetCardDatabase();
                    if (cardData.TryGetValue(cardId.ToString(), out var card))
                    {
                        var charName = GameConstants.GetCharacterName(card.CharactersId);
                        var rarityName = GameConstants.GetRarityString(card.Rarity);
                        _forbiddenCardsDisplay.Add($"{cardId} - [{rarityName}] {charName} {card.Name}");
                    }
                    else
                    {
                        _forbiddenCardsDisplay.Add(cardId.ToString());
                    }
                }
                catch
                {
                    _forbiddenCardsDisplay.Add(cardId.ToString());
                }
            }
        }

        private void AddForbiddenCard_Click(object sender, RoutedEventArgs e)
        {
            if (ForbiddenCardsComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string cardIdStr = selectedItem.Tag?.ToString();
                if (string.IsNullOrEmpty(cardIdStr))
                {
                    MessageBox.Show("請先選擇卡片", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!int.TryParse(cardIdStr, out int cardId))
                {
                    return;
                }

                // Ensure ForbiddenCards list is initialized
                if (_config.Optimizer.ForbiddenCards == null)
                {
                    _config.Optimizer.ForbiddenCards = new System.Collections.Generic.List<int>();
                }

                // Check if already exists
                if (_config.Optimizer.ForbiddenCards.Contains(cardId))
                {
                    MessageBox.Show("此卡片已在禁卡列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Add to config
                _config.Optimizer.ForbiddenCards.Add(cardId);

                // Update display
                UpdateForbiddenCardsDisplay();

                // Reset combo box
                ForbiddenCardsComboBox.SelectedIndex = 0;
            }
        }

        private void ClearForbiddenCards_Click(object sender, RoutedEventArgs e)
        {
            if (_config.Optimizer.ForbiddenCards == null || _config.Optimizer.ForbiddenCards.Count == 0)
            {
                return;
            }

            var result = MessageBox.Show(
                "確定要清空所有全局禁卡嗎？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _config.Optimizer.ForbiddenCards.Clear();
                UpdateForbiddenCardsDisplay();
            }
        }

        private void ForbiddenCardsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ForbiddenCardsListBox.SelectedItem is string selectedDisplay)
            {
                // Extract card ID from display string (format: "cardId - [rarity] name")
                var cardIdStr = selectedDisplay.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
                if (int.TryParse(cardIdStr, out int cardId))
                {
                    _config.Optimizer.ForbiddenCards.Remove(cardId);
                    UpdateForbiddenCardsDisplay();
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate Top N
            if (!int.TryParse(TopNTextBox.Text.Trim(), out int topN) || topN <= 0)
            {
                MessageBox.Show(
                    "保留前 N 名卡組必須是大於 0 的整數",
                    "驗證錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                TopNTextBox.Focus();
                return;
            }

            // Save configuration
            _config.Optimizer.TopN = topN;
            _config.Optimizer.ShowCardNames = ShowCardNamesCheckBox.IsChecked ?? true;

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
