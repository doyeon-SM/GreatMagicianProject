using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public int Character_Level = 1;
    public int Character_EXP = 0;
    public int WallHP = 30;
    public float Character_Mana = 2.0f;
    public int Character_Gold = 0;
    public int Character_Stat = 0;
    public int Character_Int = 1;
    public int Character_Int_Level = 1;
    public int[] Character_HaveSkill;
    public int Character_SkillDust = 0;

    public Skill_Data[] tier0Skills; // 0Tier 스킬 배열 (예: 4개)
    public Skill_Data[] tier1Skills;
    public Skill_Data[] tier2Skills;

    public Skill_Combination_Data[] tier1SkillsCombination;
    public Skill_Combination_Data[] tier2SkillsCombination;

    public int Character_NextEXP = 10;

    // 해당 인덱스의 스킬 레벨을 올리고 데미지를 증가시키는 메서드
    public void LevelUpSkill(int tier, int index)
    {
        // 1) 유효성 검사
        var skillArr = GetTierArray(tier);
        if (skillArr == null)
        {
            Debug.LogError($"[LevelUp] 유효하지 않은 티어: {tier}");
            return;
        }
        if (index < 0 || index >= skillArr.Length)
        {
            Debug.LogError($"[LevelUp] 티어 {tier} 인덱스 범위 초과: {index}");
            return;
        }

        int globalIndex = GetGlobalSkillIndex(tier, index);
        if (Character_HaveSkill == null ||
            globalIndex < 0 || globalIndex >= Character_HaveSkill.Length)
        {
            Debug.LogError($"[LevelUp] Character_HaveSkill 범위 오류. globalIndex={globalIndex}, len={(Character_HaveSkill == null ? -1 : Character_HaveSkill.Length)}");
            return;
        }

        // 만렙 정의: 0티어=100, 1티어 이상=50
        int maxLevel = (tier == 0) ? 100 : 50;

        // 스킬 참조
        var skill = skillArr[index];

        // 이미 만렙?
        if (skill.level >= maxLevel)
        {
            Debug.Log($"[LevelUp] 이미 만렙입니다. (Tier{tier}/{skill.skillName}, Lv.{skill.level}/{maxLevel})");
            return;
        }

        // 2) 비용 확인 (NeedLevelUP_Gold == 필요한 조각 수)
        int cost = Mathf.Max(1, skill.NeedLevelUP_Gold); // 최소 1 보장
        int have = Character_HaveSkill[globalIndex];
        if (have < cost)
        {
            Debug.Log($"[LevelUp] 재료 부족: 필요 {cost}, 보유 {have} (Tier{tier}/{skill.skillName})");
            return;
        }

        // 3) 소비(조각 차감)
        Character_HaveSkill[globalIndex] -= cost;

        // 현재 레벨(업그레이드 전)
        int prevLevel = skill.level;
        bool crossingTen = (prevLevel % 10 == 9); // 9->10, 19->20, ...

        // 4) 성장 처리
        //  - 기본 데미지 증가: 0티어 +1 / 1티어 이상 +5
        if (tier == 0)
        {
            skill.damage += 1;
        }
        else
        {
            skill.damage += 5;
        }

        //  - 10레벨 단위 보너스
        if (crossingTen)
        {
            if (tier == 0)
            {
                // 0티어: 데미지 *2
                skill.damage = Mathf.RoundToInt(skill.damage * 2f);
            }
            else
            {
                // 1티어 이상: effect_Value, AreaTime이 0이 아닐 때 각각 *1.2
                if (skill.Effect_Value != 0f) skill.Effect_Value *= 1.2f;
                if (skill.AreaTime != 0f) skill.AreaTime *= 1.2f;
            }
        }

        // 5) 필요 골드(조각) 갱신 - 다음 레벨 요구치 규칙
        if (tier == 0)
        {
            // 0티어: 레벨업마다 +2, 9->10 등 넘어갈 때 *2
            skill.NeedLevelUP_Gold += 2;
            if (crossingTen)
            {
                skill.NeedLevelUP_Gold = Mathf.Max(1, skill.NeedLevelUP_Gold * 2);
            }
        }
        else
        {
            // 1티어 이상: 레벨업마다 +5, 9->10 등 넘어갈 때 +10
            skill.NeedLevelUP_Gold += 5;
            if (crossingTen)
            {
                skill.NeedLevelUP_Gold += 10;
            }
        }

        // 6) 레벨 증가 (최대치 보정)
        skill.Skill_LevelUP();
        if (skill.level > maxLevel) skill.level = maxLevel;

        // 7) 결과 반영
        skillArr[index] = skill;

        Debug.Log($"[LevelUp] (Tier{tier}) {skill.skillName} 강화 완료! " +
                  $"소비:{cost}, 잔량:{Character_HaveSkill[globalIndex]}, " +
                  $"레벨:{skill.level}/{maxLevel}, 다음요구치:{skill.NeedLevelUP_Gold}, " +
                  $"데미지:{skill.damage}, 효과값:{skill.Effect_Value}, 지속:{skill.AreaTime}");
    }

    // 헬퍼: 티어 배열 접근
    private Skill_Data[] GetTierArray(int tier)
    {
        switch (tier)
        {
            case 0: return tier0Skills;
            case 1: return tier1Skills;
            case 2: return tier2Skills;
            default: return null;
        }
    }

    // 헬퍼: 전역 인덱스 계산 (Character_HaveSkill에서의 위치)
    private int GetGlobalSkillIndex(int tier, int localIndex)
    {
        int offset = 0;
        // tier0 이전 없음
        if (tier > 0) offset += (tier0Skills?.Length ?? 0);
        if (tier > 1) offset += (tier1Skills?.Length ?? 0);
        // tier==2면 위 두 합이 오프셋
        return offset + localIndex;
    }


    // UI 업데이트를 위한 헬퍼 메서드
    public Skill_Data GetSkillData(int index)
    {
        if (index >= 0 && index < tier0Skills.Length)
            return tier0Skills[index];
        return null;
    }

    public void CharacterLevelUP(int score)
    {        
        for(int score_tmp = score; score_tmp > 0;)
        {
            if(score_tmp >= Character_NextEXP)
            {
                Character_Level++;
                Character_Stat += 2;
                score_tmp -= Character_NextEXP;
                Character_NextEXP = Mathf.RoundToInt(Character_NextEXP * 1.1f);
                Debug.Log("레벨업했습니다" + Character_Level + "||" + Character_EXP + "/" + Character_NextEXP);
            }
            else
            {
                Character_EXP = score_tmp;
                score_tmp = 0;
                Debug.Log("경험치를 획득했습니다" + Character_EXP + "/" + Character_NextEXP);
            }
        }
    }

    public void CharacterStatManaUP()
    {
        if (Character_Stat > 5 && Character_Mana > 1.0f && Character_Level >= 10)
        {
            Character_Mana -= 0.1f;
            Character_Stat -= 5;
        }
        else
        {
            Debug.Log("캐릭터 스탯이 부족합니다.");
        }
    }

    public void CharacterStatWallHPUP()
    {
        if(Character_Stat > 0)
        {
            Character_Stat--;
            WallHP += 5;
        }
        else
        {
            Debug.Log("캐릭터 스탯이 부족합니다.");
        }
    }

    public void CharacterStatIntUP()
    {
        if(Character_Stat > 0)
        {
            Character_Stat--;
            if (Character_Int_Level % 10 == 0)
            {
                Character_Int *= 2;
            }
            else
            {
                Character_Int += 2;
            }
            Character_Int_Level++;
        }
        else
        {
            Debug.Log("캐릭터 스탯이 부족합니다.");
        }
    }
}
