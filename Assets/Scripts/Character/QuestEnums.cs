using UnityEngine;

public enum QuestKind
{
    // 추가/삭제가 쉬운 enum 기반 설계
    Kill_Any100,           // “단순 몬스터 킬” (100마다)
    Kill_Ignis100,         // 속성별 4종 (100마다)
    Kill_Aqua100,
    Kill_Ventus100,
    Kill_Terra100,
    UseSkill_10            // 스킬 10회
}

public enum Element4
{
    None,
    Ignis, Aqua, Ventus, Terra
}

[System.Serializable]
public struct QuestReward
{
    public int exp;
    public int gold;
    public int skillDust; // 추가 보상(스킬가루 등)을 확장하고 싶다면 필드 추가
}
