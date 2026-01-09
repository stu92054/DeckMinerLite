import json

# Load card data
with open('GameData/CardDatas.json', 'r', encoding='utf-8') as f:
    cards_db = json.load(f)

# Load rhythm game skills
with open('GameData/RhythmGameSkills.json', 'r', encoding='utf-8') as f:
    skills_db = json.load(f)

# Target deck
target_deck = [1021702, 1041513, 1021701, 1022702, 1023702, 1031533]

# SkillEffectType enum
# ScoreGain = 2
# DeckReset = 5

print("=== Deck Skill Analysis ===\n")

allowed_first = []
allowed_last = []

for card_id in target_deck:
    card_str = str(card_id)
    if card_str not in cards_db:
        print(f"Card {card_id}: NOT FOUND")
        continue

    card_data = cards_db[card_str]
    name = card_data.get('Name', 'Unknown')
    skill_series_list = card_data.get('RhythmGameSkillSeriesId', [])

    if not skill_series_list:
        print(f"{card_id}: {name} - NO SKILL")
        continue

    # Use last skill series
    skill_series = skill_series_list[-1]
    skill_id = f"{skill_series}14"

    if skill_id not in skills_db:
        print(f"{card_id}: {name} - SKILL {skill_id} NOT FOUND")
        continue

    skill_data = skills_db[skill_id]
    skill_name = skill_data.get('Name', 'Unknown')
    effect_ids = skill_data.get('RhythmGameSkillEffectId', [])

    # Extract effect types
    effect_types = []
    has_score_gain = False
    has_deck_reset = False

    for effect in effect_ids:
        effect_type = effect // 100000000
        effect_types.append(effect_type)
        if effect_type == 2:  # ScoreGain
            has_score_gain = True
        if effect_type == 5:  # DeckReset
            has_deck_reset = True

    # Check constraints
    can_be_first = not has_score_gain
    can_be_last = not has_deck_reset

    if can_be_first:
        allowed_first.append(card_id)
    if can_be_last:
        allowed_last.append(card_id)

    status = []
    if can_be_first:
        status.append("FIRST OK")
    else:
        status.append("FIRST NO (ScoreGain)")

    if can_be_last:
        status.append("LAST OK")
    else:
        status.append("LAST NO (DeckReset)")

    print(f"{card_id}: {name}")
    print(f"  Skill: {skill_name}")
    print(f"  Effects: {effect_types}")
    print(f"  Status: {', '.join(status)}")
    print()

print("=== Constraint Check ===")
print(f"Cards allowed as FIRST (no ScoreGain): {len(allowed_first)}")
print(f"  {allowed_first}")
print(f"\nCards allowed as LAST (no DeckReset): {len(allowed_last)}")
print(f"  {allowed_last}")

print("\n=== Result ===")
if len(allowed_first) == 0:
    print("FAILED: All cards have ScoreGain - cannot place any card in first position")
elif len(allowed_last) == 0:
    print("FAILED: All cards have DeckReset - cannot place any card in last position")
else:
    print(f"PASS: {len(allowed_first)} cards can be first, {len(allowed_last)} cards can be last")
    print("This deck CAN be generated")
