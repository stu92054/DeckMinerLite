import json
import yaml

# Load config
with open('member-stu92054.yaml', 'r', encoding='utf-8') as f:
    config = yaml.safe_load(f)

# Load card data
with open('GameData/CardDatas.json', 'r', encoding='utf-8') as f:
    cards = json.load(f)

# Target deck
target_deck = [1021702, 1041513, 1021701, 1022702, 1023702, 1031533]
card_pool = config['card_ids']
lgp_mode = config['lgp_mode']

print("=== 卡組生成測試 ===\n")

# Check 1: All cards in pool
print("檢查 1: 卡池包含檢查")
missing_cards = [card for card in target_deck if card not in card_pool]
if missing_cards:
    print(f"❌ 缺少卡片: {missing_cards}")
    for card_id in missing_cards:
        if str(card_id) in cards:
            print(f"   {card_id}: {cards[str(card_id)].get('Name', 'Unknown')}")
else:
    print(f"✅ 所有 6 張卡都在卡池中")

# Check 2: Character distribution
print("\n檢查 2: 角色分布")
char_counts = {}
for card_id in target_deck:
    card_str = str(card_id)
    if card_str in cards:
        char_id = cards[card_str].get('CharactersId')
        char_counts[char_id] = char_counts.get(char_id, 0) + 1

double_card_chars = sum(1 for count in char_counts.values() if count > 1)
print(f"角色統計:")
for char_id, count in sorted(char_counts.items()):
    print(f"  角色 {char_id}: {count} 張")
print(f"\n雙卡角色數: {double_card_chars}")
print(f"單卡角色數: {len(char_counts) - double_card_chars}")
print(f"總角色數: {len(char_counts)}")

if lgp_mode:
    if double_card_chars <= 3:
        print(f"✅ LGP 模式: 雙卡角色數 {double_card_chars} ≤ 3")
    else:
        print(f"❌ LGP 模式: 雙卡角色數 {double_card_chars} > 3")
else:
    if double_card_chars == 0:
        print(f"✅ 日常模式: 無雙卡角色")
    else:
        print(f"❌ 日常模式: 有 {double_card_chars} 個雙卡角色")

# Check 3: Valid role distribution
print("\n檢查 3: 角色分布是否有效")
if double_card_chars == 1 and len(char_counts) == 5:
    print(f"✅ 符合: 1 雙卡 + 4 單卡 = 5 角色 (2+4=6張)")
elif double_card_chars == 0 and len(char_counts) == 6:
    print(f"✅ 符合: 6 單卡 = 6 角色 (6張)")
elif double_card_chars == 2 and len(char_counts) == 4:
    print(f"✅ 符合: 2 雙卡 + 2 單卡 = 4 角色 (4+2=6張)")
elif double_card_chars == 3 and len(char_counts) == 3:
    print(f"✅ 符合: 3 雙卡 = 3 角色 (6張)")
else:
    print(f"❌ 不符合有效分布")

# Check 4: Rarity check (DR count)
print("\n檢查 4: DR 卡數量")
dr_count = 0
for card_id in target_deck:
    card_str = str(card_id)
    if card_str in cards:
        rarity = cards[card_str].get('Rarity')
        if rarity == 9:  # DR = 9
            dr_count += 1

if lgp_mode:
    if dr_count <= 1:
        print(f"✅ LGP 模式: DR 卡數量 {dr_count} ≤ 1")
    else:
        print(f"❌ LGP 模式: DR 卡數量 {dr_count} > 1")
else:
    print(f"ℹ️  日常模式: DR 卡數量 {dr_count} (無限制)")

# Check 5: mustcards_all constraint
print("\n檢查 5: mustcards_all 約束")
song_config = config['songs'][0]
mustcards_all = song_config.get('mustcards_all', [])
if mustcards_all:
    all_included = all(card in target_deck for card in mustcards_all)
    if all_included:
        print(f"✅ 包含所有必須卡片: {mustcards_all}")
    else:
        missing = [card for card in mustcards_all if card not in target_deck]
        print(f"❌ 缺少必須卡片: {missing}")
else:
    print(f"ℹ️  無 mustcards_all 約束")

# Check 6: Detailed card info
print("\n=== 卡組詳細資訊 ===")
for card_id in target_deck:
    card_str = str(card_id)
    if card_str in cards:
        card = cards[card_str]
        name = card.get('Name', 'Unknown')
        char_id = card.get('CharactersId')
        rarity = card.get('Rarity')
        rarity_name = {5: 'UR', 7: 'LR', 8: 'BR', 9: 'DR'}.get(rarity, f'R{rarity}')
        print(f"{card_id}: {name} (角色 {char_id}, {rarity_name})")

print("\n=== 結論 ===")
if (not missing_cards and
    double_card_chars <= 3 and
    dr_count <= 1 and
    len(char_counts) >= 3):
    print("✅ 這個卡組應該能被 C# 生成器生成")
else:
    print("❌ 這個卡組無法被生成，原因見上述檢查")
