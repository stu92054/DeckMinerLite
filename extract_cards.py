import json
from collections import defaultdict

# Load card data
with open('GameData/CardDatas.json', 'r', encoding='utf-8') as f:
    cards = json.load(f)

# Load center skills
with open('GameData/CenterSkills.json', 'r', encoding='utf-8') as f:
    center_skills = json.load(f)

# Corrected character names mapping (without 1053)
CHARACTER_NAMES = {
    1011: '大賀美沙知',
    1021: '乙宗梢',
    1022: '夕霧綴理',
    1023: '藤島慈',
    1031: '日野下花帆',
    1032: '村野さやか',
    1033: '大沢瑠璃乃',
    1041: '百生吟子',
    1042: '徒町小鈴',
    1043: '安養寺姬芽',
    1051: '桂城泉',
    1052: 'セラス',
}

# Rarity mapping
RARITY_NAMES = {5: 'UR', 9: 'BR', 7: 'LR'}
RARITY_ORDER = {5: 0, 9: 1, 7: 2}  # LR > BR > UR

# Target center skills
TARGET_SKILLS = ['ボルテージゲイン', 'APゲイン', 'スコアアップ']

# Build a mapping from CenterSkillSeriesId to skill names
skill_series_map = {}
for skill_id, skill_data in center_skills.items():
    series_id = skill_data.get('CenterSkillSeriesId')
    skill_name = skill_data.get('CenterSkillName', '')
    if series_id and any(target in skill_name for target in TARGET_SKILLS):
        skill_series_map[series_id] = skill_name

print(f'找到 {len(skill_series_map)} 個符合條件的技能系列')

# Group cards by character
character_cards = defaultdict(list)

for card_id, card_data in cards.items():
    rarity = card_data.get('Rarity')
    char_id = card_data.get('CharactersId')
    center_skill_series_id = card_data.get('CenterSkillSeriesId')

    # Filter: only UR/BR/LR
    if rarity not in [5, 9, 7]:
        continue

    # Check if center skill matches target
    if center_skill_series_id not in skill_series_map:
        continue

    # Get character name
    char_name = CHARACTER_NAMES.get(char_id, f'Unknown_{char_id}')

    character_cards[char_name].append({
        'card_id': card_id,
        'name': card_data.get('Name', 'Unknown'),
        'rarity': RARITY_NAMES[rarity],
        'rarity_order': RARITY_ORDER[rarity],
        'skill': skill_series_map[center_skill_series_id]
    })

# Sort and output
output_lines = [
    '# UR/BR/LR 卡片清單（含 ボルテージゲイン、APゲイン、スコアアップ）',
    '# 生成日期: 2025-12-23',
    ''
]

# Sort characters by ID order
sorted_chars = sorted(character_cards.keys(), key=lambda x: [k for k, v in CHARACTER_NAMES.items() if v == x][0])

for char_name in sorted_chars:
    cards_list = character_cards[char_name]
    # Sort by rarity (LR > BR > UR), then by card_id
    cards_list.sort(key=lambda x: (x['rarity_order'], x['card_id']))

    output_lines.append(f'# {char_name}')
    for card in cards_list:
        output_lines.append(f"- {card['card_id']} # {card['name']} ({card['rarity']}, {card['skill']})")
    output_lines.append('')

# Write to file
with open('high_rarity_cards.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(output_lines))

print(f'已生成 high_rarity_cards.txt')
print(f'總共 {sum(len(v) for v in character_cards.values())} 張卡片')
print(f'涵蓋 {len(character_cards)} 個角色')
