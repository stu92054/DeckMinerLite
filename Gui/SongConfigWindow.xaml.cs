using DeckMiner.Config;
using DeckMiner.Data;
using DeckMiner.Gui.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeckMiner.Gui
{
    public partial class SongConfigWindow : Window
    {
        private readonly MemberConfig _config;
        private readonly ObservableCollection<SongViewModel> _songs = new();
        private SongViewModel? _currentSong = null;
        private const int MAX_SONGS = 3;

        // ObservableCollections for card lists
        private readonly ObservableCollection<string> _mustCardsAllDisplay = new();
        private readonly ObservableCollection<string> _mustCardsAnyDisplay = new();
        private readonly ObservableCollection<string> _bannedCardsDisplay = new();

        public SongConfigWindow(MemberConfig config)
        {
            InitializeComponent();
            _config = config;

            // Bind card list displays
            MustCardsAllListBox.ItemsSource = _mustCardsAllDisplay;
            MustCardsAnyListBox.ItemsSource = _mustCardsAnyDisplay;
            BannedCardsListBox.ItemsSource = _bannedCardsDisplay;

            // 初始化卡片選擇器（只需執行一次）
            UpdateCardComboBoxes();

            LoadData();
            SongsListBox.ItemsSource = _songs;
            UpdateCount();
        }

        private void LoadData()
        {
            // 載入現有歌曲配置
            if (_config.Songs != null)
            {
                foreach (var song in _config.Songs)
                {
                    _songs.Add(SongViewModel.FromConfig(song));
                }
            }

            // 如果沒有歌曲，添加一首空白歌曲
            if (_songs.Count == 0)
            {
                _songs.Add(new SongViewModel());
            }

            // 選擇第一首歌曲
            if (_songs.Count > 0)
            {
                SongsListBox.SelectedIndex = 0;
            }
        }

        private void UpdateCount()
        {
            CountText.Text = $"({_songs.Count} / {MAX_SONGS} 首)";
        }

        private void SongsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SongsListBox.SelectedItem is SongViewModel song)
            {
                LoadSongToEditor(song);
                EditPanel.IsEnabled = true;
            }
            else
            {
                EditPanel.IsEnabled = false;
            }
        }

        private void LoadSongToEditor(SongViewModel song)
        {
            // 先儲存當前編輯的歌曲
            if (_currentSong != null)
            {
                SaveEditorToSong(_currentSong);
            }

            _currentSong = song;

            // 載入基本設定
            UpdateMusicComboBox();
            SelectMusicInComboBox(song.MusicId);
            MasteryLevelTextBox.Text = song.MasteryLevel.ToString();

            // 設定難度
            foreach (ComboBoxItem item in DifficultyComboBox.Items)
            {
                if (item.Tag?.ToString() == song.Difficulty)
                {
                    DifficultyComboBox.SelectedItem = item;
                    break;
                }
            }

            // 載入約束條件 - 顯示卡片名稱
            UpdateCardListDisplay(_mustCardsAllDisplay, song.MustCardsAll);
            UpdateCardListDisplay(_mustCardsAnyDisplay, song.MustCardsAny);
            UpdateCardListDisplay(_bannedCardsDisplay, song.BannedCards);

            // 載入必帶技能
            SkillScoreGainCheckBox.IsChecked = song.MustSkills.Contains(2);
            SkillVoltageBurstCheckBox.IsChecked = song.MustSkills.Contains(3);
            SkillDeckResetCheckBox.IsChecked = song.MustSkills.Contains(5);
            SkillScoreGainPlusCheckBox.IsChecked = song.MustSkills.Contains(7);
            SkillVoltagePlusCheckBox.IsChecked = song.MustSkills.Contains(8);

            // 載入進階設定
            SecondaryCenterTextBox.Text = string.Join(", ", song.SecondaryCenter);
            CenterOverrideTextBox.Text = song.CenterOverride?.ToString() ?? "";
            LeaderDesignationTextBox.Text = song.LeaderDesignation ?? "0";

            // 設定屬性覆蓋
            foreach (ComboBoxItem item in ColorOverrideComboBox.Items)
            {
                string itemTag = item.Tag?.ToString() ?? "";
                string songColor = song.ColorOverride?.ToString() ?? "";
                if (itemTag == songColor)
                {
                    ColorOverrideComboBox.SelectedItem = item;
                    break;
                }
            }

            // 載入朋友卡池 - 顯示卡片名稱
            FriendCardPoolTextBox.Text = FormatCardList(song.FriendCardPool);

            StatusText.Text = $"正在編輯: {song.DisplayName}";
        }

        private void SaveEditorToSong(SongViewModel song)
        {
            // 儲存基本設定 - 從歌曲下拉選單取得 MusicId
            if (MusicSelectionComboBox.SelectedItem is ComboBoxItem selectedItem &&
                !string.IsNullOrEmpty(selectedItem.Tag?.ToString()))
            {
                song.MusicId = selectedItem.Tag.ToString()!;
            }

            if (int.TryParse(MasteryLevelTextBox.Text, out int mastery))
            {
                song.MasteryLevel = Math.Clamp(mastery, 1, 50);
            }

            // 儲存難度
            if (DifficultyComboBox.SelectedItem is ComboBoxItem diffItem)
            {
                song.Difficulty = diffItem.Tag?.ToString() ?? "02";
            }

            // 儲存約束條件 (從 ListBox 解析)
            song.MustCardsAll = ParseCardIdsFromDisplay(_mustCardsAllDisplay);
            song.MustCardsAny = ParseCardIdsFromDisplay(_mustCardsAnyDisplay);
            song.BannedCards = ParseCardIdsFromDisplay(_bannedCardsDisplay);

            // 儲存必帶技能
            var skills = new List<int>();
            if (SkillScoreGainCheckBox.IsChecked == true) skills.Add(2);
            if (SkillVoltageBurstCheckBox.IsChecked == true) skills.Add(3);
            if (SkillDeckResetCheckBox.IsChecked == true) skills.Add(5);
            if (SkillScoreGainPlusCheckBox.IsChecked == true) skills.Add(7);
            if (SkillVoltagePlusCheckBox.IsChecked == true) skills.Add(8);
            song.MustSkills = skills;

            // 儲存進階設定
            song.SecondaryCenter = ParseCardIds(SecondaryCenterTextBox.Text);

            // CenterOverride 是 int?
            string centerText = CenterOverrideTextBox.Text.Trim();
            song.CenterOverride = string.IsNullOrWhiteSpace(centerText) ? null : int.TryParse(centerText, out int centerId) ? centerId : null;

            song.LeaderDesignation = LeaderDesignationTextBox.Text.Trim();

            // 儲存屬性覆蓋 (ColorOverride 是 int?)
            if (ColorOverrideComboBox.SelectedItem is ComboBoxItem colorItem)
            {
                string colorValue = colorItem.Tag?.ToString() ?? "";
                song.ColorOverride = string.IsNullOrEmpty(colorValue) ? null : int.TryParse(colorValue, out int colorId) ? colorId : null;
            }

            // 儲存朋友卡池
            song.FriendCardPool = ParseCardIds(FriendCardPoolTextBox.Text);

            // 觸發 ListBox 更新顯示
            SongsListBox.Items.Refresh();
        }

        private List<int> ParseCardIds(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<int>();

            return text.Split(new[] { ',', ' ', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                       .Where(id => id > 0)
                       .ToList();
        }

        private List<int> ParseCardIdsFromDisplay(ObservableCollection<string> displayItems)
        {
            var cardIds = new List<int>();
            foreach (var item in displayItems)
            {
                // Extract card ID from display string (format: "123456 [UR] 角色名 卡片名")
                var parts = item.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    cardIds.Add(cardId);
                }
            }
            return cardIds;
        }

        private void UpdateCardListDisplay(ObservableCollection<string> displayCollection, List<int> cardIds)
        {
            displayCollection.Clear();
            if (cardIds == null || cardIds.Count == 0)
                return;

            var cardDb = Services.DataManager.Instance.GetCardDatabase();
            foreach (var cardId in cardIds)
            {
                if (cardDb.TryGetValue(cardId.ToString(), out var card))
                {
                    var rarityName = Data.GameConstants.GetRarityString(card.Rarity);
                    var charName = Data.GameConstants.GetCharacterName(card.CharactersId);
                    displayCollection.Add($"{cardId} [{rarityName}] {charName} {card.Name}");
                }
                else
                {
                    displayCollection.Add(cardId.ToString());
                }
            }
        }

        private string FormatCardList(List<int> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
                return "";

            var cardDb = Services.DataManager.Instance.GetCardDatabase();
            var formattedCards = new List<string>();

            foreach (var cardId in cardIds)
            {
                if (cardDb.TryGetValue(cardId.ToString(), out var card))
                {
                    var rarityName = Data.GameConstants.GetRarityString(card.Rarity);
                    var charName = Data.GameConstants.GetCharacterName(card.CharactersId);
                    formattedCards.Add($"{cardId} [{rarityName}] {charName} {card.Name}");
                }
                else
                {
                    formattedCards.Add(cardId.ToString());
                }
            }

            return string.Join("\n", formattedCards);
        }

        private void UpdateCardComboBoxes()
        {
            var cardIds = _config.CardIds ?? new List<int>();

            // 為每個 ComboBox 創建獨立的選項列表
            MustCardsAllComboBox.ItemsSource = CreateCardComboBoxItems(cardIds);
            MustCardsAllComboBox.SelectedIndex = 0;

            MustCardsAnyComboBox.ItemsSource = CreateCardComboBoxItems(cardIds);
            MustCardsAnyComboBox.SelectedIndex = 0;

            BannedCardsComboBox.ItemsSource = CreateCardComboBoxItems(cardIds);
            BannedCardsComboBox.SelectedIndex = 0;
        }

        private List<ComboBoxItem> CreateCardComboBoxItems(List<int> cardIds)
        {
            var cardOptions = new List<ComboBoxItem>();
            cardOptions.Add(new ComboBoxItem { Content = "從卡池中選擇...", Tag = "" });

            foreach (var cardId in cardIds)
            {
                try
                {
                    var cardData = Services.DataManager.Instance.GetCardDatabase();
                    if (cardData.TryGetValue(cardId.ToString(), out var card))
                    {
                        var charName = Data.GameConstants.GetCharacterName(card.CharactersId);
                        var rarityName = Data.GameConstants.GetRarityString(card.Rarity);
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

            return cardOptions;
        }

        private void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (_songs.Count >= MAX_SONGS)
            {
                MessageBox.Show($"最多只能添加 {MAX_SONGS} 首歌曲", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var newSong = new SongViewModel();
            _songs.Add(newSong);
            SongsListBox.SelectedItem = newSong;
            UpdateCount();
        }

        private void RemoveSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (SongsListBox.SelectedItem is SongViewModel song)
            {
                var result = MessageBox.Show(
                    $"確定要移除歌曲 {song.DisplayName}？",
                    "確認移除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    int index = _songs.IndexOf(song);
                    _songs.Remove(song);

                    // 選擇前一首或後一首
                    if (_songs.Count > 0)
                    {
                        int newIndex = Math.Min(index, _songs.Count - 1);
                        SongsListBox.SelectedIndex = newIndex;
                    }
                    else
                    {
                        _currentSong = null;
                        EditPanel.IsEnabled = false;
                    }

                    UpdateCount();
                }
            }
        }

        // 約束條件按鈕事件
        private void AddMustCardAll_Click(object sender, RoutedEventArgs e)
        {
            AddCardToListBox(MustCardsAllComboBox, _mustCardsAllDisplay);
        }

        private void ClearMustCardsAll_Click(object sender, RoutedEventArgs e)
        {
            _mustCardsAllDisplay.Clear();
        }

        private void AddMustCardAny_Click(object sender, RoutedEventArgs e)
        {
            AddCardToListBox(MustCardsAnyComboBox, _mustCardsAnyDisplay);
        }

        private void ClearMustCardsAny_Click(object sender, RoutedEventArgs e)
        {
            _mustCardsAnyDisplay.Clear();
        }

        private void AddBannedCard_Click(object sender, RoutedEventArgs e)
        {
            AddCardToListBox(BannedCardsComboBox, _bannedCardsDisplay);
        }

        private void ClearBannedCards_Click(object sender, RoutedEventArgs e)
        {
            _bannedCardsDisplay.Clear();
        }

        private void SelectFriendCards_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSong == null) return;

            var selector = new FriendCardSelectorWindow(_currentSong.FriendCardPool)
            {
                Owner = this
            };

            if (selector.ShowDialog() == true)
            {
                _currentSong.FriendCardPool = selector.SelectedCardIds;
                FriendCardPoolTextBox.Text = FormatCardList(_currentSong.FriendCardPool);
            }
        }

        private void ClearFriendCardPool_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSong != null)
            {
                _currentSong.FriendCardPool.Clear();
                FriendCardPoolTextBox.Clear();
            }
        }

        private void UpdateMusicComboBox()
        {
            try
            {
                var musicDb = Services.DataManager.Instance.GetMusicDatabase();
                var musicOptions = new List<ComboBoxItem>();

                musicOptions.Add(new ComboBoxItem { Content = "請選擇歌曲...", Tag = "" });

                // 依歌曲 ID 排序
                foreach (var music in musicDb.OrderBy(m => m.Key))
                {
                    var content = $"{music.Value.Title} (ID: {music.Key})";
                    musicOptions.Add(new ComboBoxItem { Content = content, Tag = music.Key });
                }

                MusicSelectionComboBox.ItemsSource = musicOptions;
                MusicSelectionComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法載入歌曲資料庫：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectMusicInComboBox(string musicId)
        {
            if (string.IsNullOrEmpty(musicId))
            {
                MusicSelectionComboBox.SelectedIndex = 0;
                return;
            }

            foreach (ComboBoxItem item in MusicSelectionComboBox.Items)
            {
                if (item.Tag?.ToString() == musicId)
                {
                    MusicSelectionComboBox.SelectedItem = item;
                    return;
                }
            }

            // 如果找不到對應的歌曲，選擇第一項
            MusicSelectionComboBox.SelectedIndex = 0;
        }

        private void MusicSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentSong != null && MusicSelectionComboBox.SelectedItem is ComboBoxItem item)
            {
                string musicId = item.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(musicId))
                {
                    _currentSong.MusicId = musicId;
                    // 觸發 ListBox 更新顯示
                    SongsListBox.Items.Refresh();
                    StatusText.Text = $"正在編輯: {_currentSong.DisplayName}";
                }
            }
        }

        private void AddCardToList(ComboBox comboBox, TextBox targetTextBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
            {
                string cardId = item.Tag.ToString()!;
                var existingIds = ParseCardIds(targetTextBox.Text);
                int cardIdInt = int.Parse(cardId);

                if (!existingIds.Contains(cardIdInt))
                {
                    existingIds.Add(cardIdInt);
                    targetTextBox.Text = FormatCardList(existingIds);
                }
                else
                {
                    MessageBox.Show("此卡片已在列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                comboBox.SelectedIndex = 0;
            }
        }

        private void AddCardToListBox(ComboBox comboBox, ObservableCollection<string> targetDisplay)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && !string.IsNullOrEmpty(item.Tag?.ToString()))
            {
                string cardId = item.Tag.ToString()!;
                int cardIdInt = int.Parse(cardId);

                // Check if card is already in the list
                var existingIds = ParseCardIdsFromDisplay(targetDisplay);
                if (existingIds.Contains(cardIdInt))
                {
                    MessageBox.Show("此卡片已在列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Add formatted card to display
                var cardDb = Services.DataManager.Instance.GetCardDatabase();
                if (cardDb.TryGetValue(cardId, out var card))
                {
                    var rarityName = Data.GameConstants.GetRarityString(card.Rarity);
                    var charName = Data.GameConstants.GetCharacterName(card.CharactersId);
                    targetDisplay.Add($"{cardId} [{rarityName}] {charName} {card.Name}");
                }
                else
                {
                    targetDisplay.Add(cardId);
                }

                comboBox.SelectedIndex = 0;
            }
        }

        private void MustCardsAllListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MustCardsAllListBox.SelectedItem is string selectedItem)
            {
                var parts = selectedItem.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    _mustCardsAllDisplay.Remove(selectedItem);
                }
            }
        }

        private void MustCardsAnyListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MustCardsAnyListBox.SelectedItem is string selectedItem)
            {
                var parts = selectedItem.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    _mustCardsAnyDisplay.Remove(selectedItem);
                }
            }
        }

        private void BannedCardsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (BannedCardsListBox.SelectedItem is string selectedItem)
            {
                var parts = selectedItem.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cardId))
                {
                    _bannedCardsDisplay.Remove(selectedItem);
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 儲存當前編輯的歌曲
            if (_currentSong != null)
            {
                SaveEditorToSong(_currentSong);
            }

            // 驗證歌曲配置
            var invalidSongs = _songs.Where(s => string.IsNullOrWhiteSpace(s.MusicId)).ToList();
            if (invalidSongs.Count > 0)
            {
                MessageBox.Show(
                    "有歌曲未設定歌曲 ID，請檢查配置",
                    "驗證失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // 更新 config
            _config.Songs = _songs.Select(s => s.ToConfig()).ToList();

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
