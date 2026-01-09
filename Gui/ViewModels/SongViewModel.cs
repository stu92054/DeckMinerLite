using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace DeckMiner.Gui.ViewModels
{
    public partial class SongViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _musicId = "";

        [ObservableProperty]
        private string _difficulty = "02";

        [ObservableProperty]
        private int _masteryLevel = 50;

        [ObservableProperty]
        private List<int> _mustCardsAll = new();

        [ObservableProperty]
        private List<int> _mustCardsAny = new();

        [ObservableProperty]
        private List<int> _mustSkills = new();

        [ObservableProperty]
        private List<int> _bannedCards = new();

        [ObservableProperty]
        private List<int> _secondaryCenter = new();

        [ObservableProperty]
        private List<int> _friendCardPool = new();

        [ObservableProperty]
        private int? _centerOverride = null;

        [ObservableProperty]
        private int? _colorOverride = null;

        [ObservableProperty]
        private string _leaderDesignation = "0";

        // 顯示用屬性
        public string DifficultyText => Difficulty switch
        {
            "01" => "Normal",
            "02" => "Hard",
            "03" => "Expert",
            "04" => "Master",
            _ => "Unknown"
        };

        public string SongTitle
        {
            get
            {
                if (string.IsNullOrEmpty(MusicId))
                    return "未知歌曲";

                try
                {
                    var musicDb = Services.DataManager.Instance.GetMusicDatabase();
                    if (musicDb.TryGetValue(MusicId, out var music))
                    {
                        return music.Title;
                    }
                }
                catch
                {
                    // 若無法載入數據庫，返回 ID
                }

                return MusicId;
            }
        }

        public string DisplayName => string.IsNullOrEmpty(MusicId)
            ? "未設定歌曲"
            : $"{SongTitle} ({DifficultyText})";

        // 從 Config.SongConfig 建立
        public static SongViewModel FromConfig(Config.SongConfig config)
        {
            return new SongViewModel
            {
                MusicId = config.MusicId,
                Difficulty = config.Difficulty,
                MasteryLevel = config.MasteryLevel,
                MustCardsAll = new List<int>(config.MustcardsAll ?? new()),
                MustCardsAny = new List<int>(config.MustcardsAny ?? new()),
                MustSkills = new List<int>(config.MustSkills ?? new()),
                BannedCards = new List<int>(config.BannedCards ?? new()),
                SecondaryCenter = new List<int>(config.SecondaryCenter ?? new()),
                FriendCardPool = new List<int>(config.FriendCardPool ?? new()),
                CenterOverride = config.CenterOverride,
                ColorOverride = config.ColorOverride,
                LeaderDesignation = config.LeaderDesignation ?? "0"
            };
        }

        // 轉換為 Config.SongConfig
        public Config.SongConfig ToConfig()
        {
            return new Config.SongConfig
            {
                MusicId = MusicId,
                Difficulty = Difficulty,
                MasteryLevel = MasteryLevel,
                MustcardsAll = MustCardsAll.ToList(),
                MustcardsAny = MustCardsAny.ToList(),
                MustSkills = MustSkills.Count > 0 ? MustSkills.ToList() : new List<int>(),
                BannedCards = BannedCards.ToList(),
                SecondaryCenter = SecondaryCenter.ToList(),
                FriendCardPool = FriendCardPool.ToList(),
                CenterOverride = CenterOverride,
                ColorOverride = ColorOverride,
                LeaderDesignation = LeaderDesignation
            };
        }

        // 複製方法
        public SongViewModel Clone()
        {
            return new SongViewModel
            {
                MusicId = MusicId,
                Difficulty = Difficulty,
                MasteryLevel = MasteryLevel,
                MustCardsAll = new List<int>(MustCardsAll),
                MustCardsAny = new List<int>(MustCardsAny),
                MustSkills = new List<int>(MustSkills),
                BannedCards = new List<int>(BannedCards),
                SecondaryCenter = new List<int>(SecondaryCenter),
                FriendCardPool = new List<int>(FriendCardPool),
                CenterOverride = CenterOverride,
                ColorOverride = ColorOverride,
                LeaderDesignation = LeaderDesignation
            };
        }
    }
}
