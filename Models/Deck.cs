using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace DeckMiner.Models
{
    public readonly struct CardDeckInfo(int id, List<int> levels)
    {
        public readonly int CardId = id;
        public readonly List<int> Levels = levels;
    }

    /// <summary>
    /// 卡组类，管理卡牌队列和总属性。
    /// 对应 Python 的 Deck
    /// </summary>
    public class Deck
    {
        public Card[] Cards = new Card[6];
        public Card? FriendCard { get; set; } = null;  // 朋友卡片（提供數值和 Center Skill，不提供一般技能和被動）
        public List<Card> Queue { get; private set; } = new List<Card>(6);
        public int Appeal { get; private set; } = 0;
        public List<string> CardLog { get; private set; } = new List<string>();
        public Card TopCard;

        // 构造函数
        public Deck(List<CardDeckInfo> cardInfo) // [Card ID, [LV, CSkillLV, SkillLV]]
        {
            for (int i = 0; i < cardInfo.Count; i++)
            {
                var cardData = cardInfo[i];
                Cards[i] = Card.GetInstance(cardData.CardId, cardData.Levels);
            }
            Reset();
        }

        // ----------------- 队列管理 -----------------

        /// <summary>
        /// 重置技能队列 (将所有非除外卡牌加入队列)。
        /// 对应 Python 的 reset
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Queue.Clear();
            Queue.AddRange(Cards.Where(card => !card.IsExcept));
            
            // 卡组全部除外时的特殊处理
            if (Queue.Count == 0)
            {
                // 确保队列至少有一个占位符或特殊逻辑
                Queue.Add(null); 
            }
            TopCard = Queue.First();
        }

        public void ExceptCard(Card card)
        {
            if (card == null) return;
            card.IsExcept = true;
            var index = Queue.IndexOf(card);
            if (index != -1)
            {
                Queue.RemoveAt(index);
                if (Queue.Count == 0)
                    Reset(); 
            }
        }

        /// <summary>
        /// 获取队列顶部的技能并移除该卡牌。
        /// 对应 Python 的 topskill
        /// </summary>
        public (List<List<string>> Condition, List<int> Effect) TopSkill()
        {
            if (TopCard == null)
            {
                // 队列中只有 null (卡组全被除外)
                // 抛出异常或返回默认值
                throw new InvalidOperationException("技能队列为空且卡组中所有卡牌均被除外。");
            }

            CardLog.Add(TopCard.FullName);
            var result = TopCard.GetSkill();
            Queue.RemoveAt(0);
            if (Queue.Count == 0)
            {
                Reset();
            }
            TopCard = Queue.First();

            return result;
        }
        
        // ----------------- 属性计算 -----------------

        /// <summary>
        /// 计算卡组总 Appeal 值。
        /// 对应 Python 的 appeal_calc
        /// </summary>
        public int AppealCalc(int musicType) // musicType: 1(Smile), 2(Pure), 3(Cool)
        {
            int result = 0;
            switch (musicType)
            {
                case 1: // Smile
                    foreach (var card in Cards)
                        result += (card.Smile * 10) + card.Pure + card.Cool;
                    // 朋友卡片數值（已受隊長被動影響）
                    if (FriendCard != null)
                        result += (FriendCard.Smile * 10) + FriendCard.Pure + FriendCard.Cool;
                    break;
                case 2: // Pure
                    foreach (var card in Cards)
                        result += card.Smile + (card.Pure * 10) + card.Cool;
                    // 朋友卡片數值（已受隊長被動影響）
                    if (FriendCard != null)
                        result += FriendCard.Smile + (FriendCard.Pure * 10) + FriendCard.Cool;
                    break;
                case 3: // Cool
                    foreach (var card in Cards)
                        result += card.Smile + card.Pure + (card.Cool * 10);
                    // 朋友卡片數值（已受隊長被動影響）
                    if (FriendCard != null)
                        result += FriendCard.Smile + FriendCard.Pure + (FriendCard.Cool * 10);
                    break;
                default:
                    foreach (var card in Cards)
                        result += card.Smile + card.Pure + card.Cool;
                    // 朋友卡片數值（已受隊長被動影響）
                    if (FriendCard != null)
                        result += FriendCard.Smile + FriendCard.Pure + FriendCard.Cool;
                    break;
            }

            int finalAppeal = (int)Ceiling(result / 10.0);
            Appeal = finalAppeal;
            return finalAppeal;
        }

        /// <summary>
        /// 计算卡组总 Mental 值。
        /// 对应 Python 的 mental_calc
        /// </summary>
        public int MentalCalc()
        {
            int result = 0;
            foreach (var card in Cards)
            {
                result += card.Mental;
            }
            // 朋友卡片 HP（已受隊長被動影響）
            if (FriendCard != null)
            {
                result += FriendCard.Mental;
            }
            return result;
        }

        /// <summary>
        /// 获取已使用的技能次数。
        /// 对应 Python 的 used_all_skill_calc
        /// </summary>
        public int UsedAllSkillCalc()
        {
            return CardLog.Count;
        }
    }
}