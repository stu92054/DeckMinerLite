using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeckMiner.Config;
using DeckMiner.Data;
using DeckMiner.Gui.ViewModels;
using DeckMiner.Services;

namespace DeckMiner.Gui
{
    public partial class CardPoolWindow : Window
    {
        private readonly MemberConfig _config;
        private readonly List<CardViewModel> _allCards = new();
        private readonly ObservableCollection<CardViewModel> _availableCards = new();
        private readonly ObservableCollection<CardViewModel> _selectedCards = new();
        
        public CardPoolWindow(MemberConfig config)
        {
            InitializeComponent();
            _config = config;
            
            LoadData();
            
            AvailableCardsListBox.ItemsSource = _availableCards;
            SelectedCardsListBox.ItemsSource = _selectedCards;
            
            UpdateCount();
            InitializeFilters();
        }

        private void LoadData()
        {
            var cardDb = DataManager.Instance.GetCardDatabase();
            var currentSet = new HashSet<int>(_config.CardIds ?? new List<int>());

            foreach (var kvp in cardDb)
            {
                if (int.TryParse(kvp.Key, out int id))
                {
                    var card = kvp.Value;
                    var vm = new CardViewModel
                    {
                        Id = id,
                        Name = card.Name,
                        CharacterId = card.CharactersId,
                        Rarity = card.Rarity,
                        MemberName = GameConstants.GetCharacterName(card.CharactersId),
                        RarityString = GameConstants.GetRarityString(card.Rarity)
                    };

                    // Initialize levels
                    if (_config.CardLevels != null && _config.CardLevels.TryGetValue(id, out var levels) && levels.Count >= 3)
                    {
                        vm.Level = levels[0];
                        vm.CenterSkillLevel = levels[1];
                        vm.SkillLevel = levels[2];
                    }
                    else
                    {
                        // Default levels based on rarity (logic from YamlConfigManager)
                        vm.Level = GetDefaultLevel(card.Rarity);
                        vm.CenterSkillLevel = 14;
                        vm.SkillLevel = 14;
                    }

                    _allCards.Add(vm);

                    if (currentSet.Contains(id))
                    {
                        _selectedCards.Add(vm);
                    }
                }
            }
            
            ApplyFilter();
        }

        private int GetDefaultLevel(int rarity)
        {
            return rarity switch
            {
                3 => 80,   // R
                4 => 100,  // SR
                5 => 120,  // UR
                7 => 140,  // LR
                8 => 140,  // DR
                9 => 120,  // BR
                _ => 100
            };
        }

        private void InitializeFilters()
        {
            // Character Filter
            var characters = _allCards.Select(c => c.CharacterId).Distinct().OrderBy(c => c).ToList();
            CharacterFilterComboBox.Items.Add("All");
            foreach (var c in characters)
            {
                var name = GameConstants.GetCharacterName(c);
                CharacterFilterComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = c });
            }
            CharacterFilterComboBox.SelectedIndex = 0;

            // Rarity Filter
            var rarities = _allCards.Select(c => c.Rarity).Distinct().OrderBy(r => r).ToList();
            RarityFilterComboBox.Items.Clear();
            RarityFilterComboBox.Items.Add("All");
            foreach (var r in rarities)
            {
                var name = GameConstants.GetRarityString(r);
                RarityFilterComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = r });
            }
            RarityFilterComboBox.SelectedIndex = 0;
        }

        private void ApplyFilter()
        {
            _availableCards.Clear();
            
            string search = SearchTextBox.Text.ToLower();
            
            int? selectedCharId = null;
            if (CharacterFilterComboBox.SelectedItem is ComboBoxItem charItem)
                selectedCharId = charItem.Tag as int?;

            int? selectedRarity = null;
            if (RarityFilterComboBox.SelectedItem is ComboBoxItem rarityItem)
                selectedRarity = rarityItem.Tag as int?;

            var selectedIds = new HashSet<int>(_selectedCards.Select(c => c.Id));

            var filtered = _allCards.Where(c => 
                !selectedIds.Contains(c.Id) &&
                (string.IsNullOrEmpty(search) || c.SearchText.Contains(search)) &&
                (selectedCharId == null || c.CharacterId == selectedCharId) &&
                (selectedRarity == null || c.Rarity == selectedRarity)
            );

            foreach (var card in filtered)
            {
                _availableCards.Add(card);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var items = AvailableCardsListBox.SelectedItems.Cast<CardViewModel>().ToList();
            foreach (var item in items)
            {
                _selectedCards.Add(item);
                _availableCards.Remove(item);
            }
            UpdateCount();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var items = SelectedCardsListBox.SelectedItems.Cast<CardViewModel>().ToList();
            foreach (var item in items)
            {
                _selectedCards.Remove(item);
            }
            ApplyFilter(); // Re-populate available list correctly
            UpdateCount();
        }

        private void AddAllButton_Click(object sender, RoutedEventArgs e)
        {
            var items = _availableCards.ToList();
            foreach (var item in items)
            {
                _selectedCards.Add(item);
                _availableCards.Remove(item);
            }
            UpdateCount();
        }

        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedCards.Clear();
            ApplyFilter();
            UpdateCount();
        }

        private void UpdateCount()
        {
            CountText.Text = $"Selected: {_selectedCards.Count}";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Update Card IDs
            _config.CardIds = _selectedCards.Select(c => c.Id).OrderBy(id => id).ToList();

            // Update Card Levels
            if (_config.CardLevels == null) _config.CardLevels = new Dictionary<int, List<int>>();
            
            // Clear old levels for cards that are no longer selected? 
            // Maybe better to just update/add. Removing might lose data if user accidentally removes and re-adds.
            // But if we don't remove, the config grows indefinitely.
            // Let's keep it simple: Update levels for selected cards.
            
            foreach (var card in _selectedCards)
            {
                _config.CardLevels[card.Id] = new List<int> { card.Level, card.CenterSkillLevel, card.SkillLevel };
            }

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
