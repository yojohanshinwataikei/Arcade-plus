#!/usr/bin/env python3

import json
import sys
from typing import Set, Any

def get_all_keys(obj: Any, prefix: str = '') -> Set[str]:
    keys = set()
    
    if isinstance(obj, dict):
        for key, value in obj.items():
            full_key = f"{prefix}.{key}" if prefix else key
            keys.add(full_key)
            keys.update(get_all_keys(value, full_key))
    elif isinstance(obj, list):
        for item in obj:
            if isinstance(item, (dict, list)):
                keys.update(get_all_keys(item, prefix))
    
    return keys

def main():
    if len(sys.argv) != 2:
        print("usage: python check_missing_locale.py <target-locale-json>")
        sys.exit(1)
    
    file_a = "locales/zh_Hans.json"
    file_b = sys.argv[1]
    
    try:
        with open(file_a, 'r', encoding='utf-8') as f:
            data_a = json.load(f)
    except Exception as e:
        print(f"failed to read {file_a}: {e}", file=sys.stderr)
        sys.exit(1)
    
    try:
        with open(file_b, 'r', encoding='utf-8') as f:
            data_b = json.load(f)
    except Exception as e:
        print(f"failed to read {file_b}: {e}", file=sys.stderr)
        sys.exit(1)
    
    keys_a = get_all_keys(data_a)
    keys_b = get_all_keys(data_b)
    
    missing = sorted(keys_a - keys_b)
    
    for key in missing:
        print(key)

if __name__ == "__main__":
    main()