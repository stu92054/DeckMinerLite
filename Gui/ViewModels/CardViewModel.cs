using CommunityToolkit.Mvvm.ComponentModel;

namespace DeckMiner.Gui.ViewModels
{
    public partial class CardViewModel : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CharacterId { get; set; }
        public int Rarity { get; set; }
        
        public string MemberName { get; set; }
        public string RarityString { get; set; }

        [ObservableProperty]
        private int _level;

        [ObservableProperty]
        private int _centerSkillLevel;

        [ObservableProperty]
        private int _skillLevel;

        public string DisplayName => $"[{RarityString}] {MemberName} - {Name}";
        
        // Helper for filtering/display
        public string SearchText => $"{Id} {Name} {MemberName} {CharacterId}".ToLower();
    }
}
