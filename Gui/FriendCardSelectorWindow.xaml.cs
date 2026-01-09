using DeckMiner.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeckMiner.Gui
{
    public partial class FriendCardSelectorWindow : Window
    {
        private readonly ObservableCollection<CardDisplayItem> _allCards = new();
        private readonly ObservableCollection<CardDisplayItem> _filteredCards = new();
        private readonly HashSet<int> _selectedCardIds = new();
        private readonly ObservableCollection<string> _selectedCardsDisplay = new();

        public List<int> SelectedCardIds => _selectedCardIds.ToList();

        public FriendCardSelectorWindow(List<int> initialSelection)
        {
            InitializeComponent();

            // 載入初始選擇
            if (initialSelection != null)
            {
                foreach (var id in initialSelection)
                {
                    _selectedCardIds.Add(id);
                }
            }

            LoadAllCards();
            CardsDataGrid.ItemsSource = _filteredCards;
            SelectedCardsListBox.ItemsSource = _selectedCardsDisplay;
            UpdateSelectedCardsDisplay();
        }

        private void LoadAllCards()
        {
            try
            {
                var cardDb = Services.DataManager.Instance.GetCardDatabase();

                foreach (var card in cardDb.OrderBy(c => c.Key))
                {
                    if (int.TryParse(card.Key, out int cardId))
                    {
                        var displayItem = new CardDisplayItem
                        {
                            CardId = cardId,
                            Rarity = card.Value.Rarity,
                            RarityString = GameConstants.GetRarityString(card.Value.Rarity),
                            CharacterName = GameConstants.GetCharacterName(card.Value.CharactersId),
                            CardName = card.Value.Name
                        };

                        _allCards.Add(displayItem);
                        _filteredCards.Add(displayItem);
                    }
                }

                UpdateResultCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入卡片資料失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCards();
        }

        private void FilterCards()
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();

            _filteredCards.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                foreach (var card in _allCards)
                {
                    _filteredCards.Add(card);
                }
            }
            else
            {
                foreach (var card in _allCards)
                {
                    if (card.CardId.ToString().Contains(searchText) ||
                        card.CharacterName.ToLower().Contains(searchText) ||
                        card.CardName.ToLower().Contains(searchText) ||
                        card.RarityString.ToLower().Contains(searchText))
                    {
                        _filteredCards.Add(card);
                    }
                }
            }

            UpdateResultCount();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
        }

        private void CardsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CardsDataGrid.SelectedItem is CardDisplayItem card)
            {
                if (_selectedCardIds.Contains(card.CardId))
                {
                    _selectedCardIds.Remove(card.CardId);
                }
                else
                {
                    _selectedCardIds.Add(card.CardId);
                }

                UpdateSelectedCardsDisplay();
            }
        }

        private void SelectedCardsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCardsListBox.SelectedItem is string selectedItem)
            {
                // 從顯示字串中提取卡片 ID（格式：「123456 [UR] 角色名 卡片名」）
                var parts = selectedItem.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    _selectedCardIds.Remove(cardId);
                    UpdateSelectedCardsDisplay();
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCardIds.Count == 0)
            {
                MessageBox.Show("已選清單為空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"確定要清空所有已選卡片（共 {_selectedCardIds.Count} 張）？",
                "確認清空",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _selectedCardIds.Clear();
                UpdateSelectedCardsDisplay();
            }
        }

        private void UpdateResultCount()
        {
            ResultCountText.Text = $"顯示 {_filteredCards.Count} 張卡片";
        }

        private void UpdateSelectedCardsDisplay()
        {
            SelectedCountText.Text = $" | 已選擇 {_selectedCardIds.Count} 張";

            // 更新已選卡片列表顯示
            _selectedCardsDisplay.Clear();
            var cardDb = Services.DataManager.Instance.GetCardDatabase();

            foreach (var cardId in _selectedCardIds.OrderBy(id => id))
            {
                if (cardDb.TryGetValue(cardId.ToString(), out var card))
                {
                    var rarityName = GameConstants.GetRarityString(card.Rarity);
                    var charName = GameConstants.GetCharacterName(card.CharactersId);
                    _selectedCardsDisplay.Add($"{cardId} [{rarityName}] {charName} {card.Name}");
                }
                else
                {
                    _selectedCardsDisplay.Add(cardId.ToString());
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CardDisplayItem
    {
        public int CardId { get; set; }
        public int Rarity { get; set; }
        public string RarityString { get; set; }
        public string CharacterName { get; set; }
        public string CardName { get; set; }
    }
}
