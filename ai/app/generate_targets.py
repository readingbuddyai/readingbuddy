# src/generate_targets.py
# -*- coding: utf-8 -*-
"""
한국어 자음 + 모음 + 받침 조합으로 TARGETS 자동 생성
모델 vocab 기준(예: slplab/wav2vec2-xls-r-300m_phone-mfa_korean)
"""

from typing import Dict, List
import json
from pathlib import Path

def generate_targets() -> Dict[str, List[str]]:
    """
    모든 가능한 자음/모음/받침 조합을 기반으로 TARGETS 딕셔너리 생성
    """

    # 자음, 모음, 받침 정의
    # (모델 vocab의 발음 심볼 기준으로 매핑)
    consonants = {
        "ㄱ": "G", "ㄲ": "GG", "ㄴ": "N", "ㄷ": "D", "ㄸ": "DD",
        "ㄹ": "L", "ㅁ": "M", "ㅂ": "B", "ㅃ": "BB", "ㅅ": "S",
        "ㅆ": "SS", "ㅇ": "NG", "ㅈ": "J", "ㅉ": "JJ", "ㅊ": "CHh",
        "ㅋ": "Kh", "ㅌ": "Th", "ㅍ": "Ph", "ㅎ": "H",
    }

    vowels = {
        "ㅏ": "A", "ㅐ": "E", "ㅑ": "iA", "ㅒ": "iE", "ㅓ": "EO", "ㅔ": "E",
        "ㅕ": "iEO", "ㅖ": "iE", "ㅗ": "O", "ㅘ": "oA", "ㅙ": "oE", "ㅚ": "uI",
        "ㅛ": "iO", "ㅜ": "U", "ㅝ": "uEO", "ㅞ": "uI", "ㅟ": "uI", "ㅠ": "iU",
        "ㅡ": "EU", "ㅢ": "uI", "ㅣ": "I"
    }

    batchims = {
        "ㄱ": "G", "ㄲ": "GG", "ㄳ": "G S", "ㄴ": "N", "ㄵ": "N J", "ㄶ": "N H",
        "ㄷ": "D", "ㄹ": "L", "ㄺ": "L G", "ㄻ": "L M", "ㄼ": "L B",
        "ㄽ": "L S", "ㄾ": "L Th", "ㄿ": "L Ph", "ㅀ": "L H", "ㅁ": "M",
        "ㅂ": "B", "ㅄ": "B S", "ㅅ": "S", "ㅆ": "SS", "ㅇ": "NG",
        "ㅈ": "J", "ㅊ": "CHh", "ㅋ": "Kh", "ㅌ": "Th", "ㅍ": "Ph", "ㅎ": "H",
    }

    targets: Dict[str, List[str]] = {}

    # 자음 단독 → “ㅡ” 붙이기
    for c, csym in consonants.items():
        key = c
        targets[key] = [csym, "EU"]  # ㄱ → 그

    # 모음 단독 → “ㅇ” 붙이기
    for v, vsym in vowels.items():
        key = v
        targets[key] = ["NG", vsym]  # ㅏ → 아

    # 자음 + 모음 조합
    for c, csym in consonants.items():
        for v, vsym in vowels.items():
            key = f"{c}+{v}"
            targets[key] = [csym, vsym]  # ㄱ+ㅏ → 가

    # 자음 + 모음 + 받침 조합
    for c, csym in consonants.items():
        for v, vsym in vowels.items():
            for b, bsym in batchims.items():
                key = f"{c}+{v}+{b}"
                # 받침 복합의 경우 "L B"처럼 공백 포함되어 있을 수 있음
                targets[key] = [csym, vsym] + bsym.split()

    return targets


if __name__ == "__main__":
    targets = generate_targets()
    out_path = Path(__file__).parent / "targets.json"
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(targets, f, ensure_ascii=False, indent=2)
    print(f"TARGETS JSON 파일 생성 완료: {out_path}")
