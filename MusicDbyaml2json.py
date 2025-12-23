import yaml
import json
from pathlib import Path
from datetime import datetime, date

# --- 配置 ---
INPUT_FILE = Path("D:\Application\Tool\Game\inspix-hailstorm-main\masterdata\Musics.yaml")
OUTPUT_FILE = Path("GameData/Musics.json")
# ----------------
# Custom JSON Encoder
class DateTimeEncoder(json.JSONEncoder):
    """
    自定义 JSON 编码器：将 datetime 和 date 对象转换为 ISO 8601 字符串。
    """
    def default(self, obj):
        # 如果对象是 datetime 类型
        if isinstance(obj, datetime):
            # 使用 ISO 8601 格式，与 YAML/JSON 常见格式兼容
            return obj.isoformat()
        # 如果对象是 date 类型
        if isinstance(obj, date):
            return obj.isoformat()
        # 对于其他类型，使用默认编码器行为
        return super().default(obj)

def convert_yaml_to_json_dict(input_path: Path, output_path: Path):
    """
    读取 Musics.yaml 文件，转换为以歌曲ID为键的 JSON 字典结构。
    """
    if not input_path.exists():
        print(f"错误：找不到输入文件 {input_path}")
        return

    print(f"正在从 {input_path} 读取 YAML 数据...")
    
    # 1. 读取 YAML 文件内容
    with open(input_path, 'r', encoding='utf-8') as f:
        # 使用 safe_load
        yaml_list_data = yaml.safe_load(f)

    if not isinstance(yaml_list_data, list):
        print("错误：YAML 文件顶级结构不是列表。无法转换。")
        return

    # 2. 核心转换逻辑：将列表转换为字典
    json_dict_data = {}
    for item in yaml_list_data:
        if 'Id' in item:
            key = str(item['Id'])
            json_dict_data[key] = item
        else:
            print(f"警告：发现没有 'Id' 键的条目，跳过: {item}")

    # 3. 将字典结构写入 JSON 文件
    print(f"成功转换 {len(json_dict_data)} 条数据，正在写入 JSON 文件 {output_path}...")
    with open(output_path, 'w', encoding='utf-8') as f:
        # *** 关键修改：使用自定义编码器 ***
        json.dump(
            json_dict_data, 
            f, 
            ensure_ascii=False, 
            indent=2,
            cls=DateTimeEncoder # <-- 告诉 dump() 如何处理 datetime 对象
        )

    print("转换完成！")

if __name__ == "__main__":
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    convert_yaml_to_json_dict(INPUT_FILE, OUTPUT_FILE)