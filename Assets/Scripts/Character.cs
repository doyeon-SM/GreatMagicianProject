using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public int Character_Level = 1;
    public int Character_EXP = 0;
    public int WallHP = 30;
    public int Character_Mana = 2;
    public int Character_Gold = 0;
    public int Character_Stat = 0;
    public int Character_Int = 1;
    public int Character_Int_Level = 1;
    public int[] Character_HaveSkill;

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

        // 2) 비용 확인: NeedLevelUP_Gold == 필요한 조각 수
        var skill = skillArr[index]; // 구조체/클래스 모두 안전: 마지막에 write-back
        int cost = Mathf.Max(1, skill.NeedLevelUP_Gold); // 최소 1 보장
        int have = Character_HaveSkill[globalIndex];

        if (have < cost)
        {
            Debug.Log($"[LevelUp] 재료 부족: 필요 {cost}, 보유 {have} (Tier{tier}/{skill.skillName})");
            return;
        }

        // 3) 소비(조각 차감)
        Character_HaveSkill[globalIndex] -= cost;

        // 4) 성장 처리 (기존 규칙 유지: 10레벨 단위 큰 상승, 그 외 +1)
        int prevLevel = skill.level;

        if (tier == 0)
        {
            if (prevLevel > 0 && prevLevel % 10 == 0)
            {
                // 10,20,30...에서 강화 → 데미지 2배 & 비용 큰 폭 증가
                skill.damage *= 2;
                skill.NeedLevelUP_Gold += (prevLevel / 10) * 10;
            }
            else
            {
                // 그 외 구간 → 데미지 +1 & 비용 소폭 증가
                skill.damage += 1;
                skill.NeedLevelUP_Gold += 2;
            }
        }
        else
        {
            // 1t 이상 스킬 강화
            if (prevLevel > 0 && prevLevel % 10 == 0)
            {
                // 10,20,30...에서 강화 → 데미지 2배 & 비용 큰 폭 증가
                skill.damage *= 2;
                skill.NeedLevelUP_Gold += (prevLevel / 10) * 10;
            }
            else
            {
                // 그 외 구간 → 데미지 +1 & 비용 소폭 증가
                skill.damage += 5;
                skill.NeedLevelUP_Gold += 1;
            }
        }

        // 레벨 증가
        skill.Skill_LevelUP();

        // 5) 결과 반영 (배열에 다시 써주기: struct/class 모두 안전)
        skillArr[index] = skill;

        Debug.Log($"[LevelUp] (Tier{tier}) {skill.skillName} 강화 완료! " +
                  $"소비:{cost}, 잔량:{Character_HaveSkill[globalIndex]}, " +
                  $"레벨:{skill.level}, 다음요구치:{skill.NeedLevelUP_Gold}, 데미지:{skill.damage}");

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
                Character_NextEXP = Mathf.RoundToInt(Character_NextEXP * 1.5f);
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
        if (Character_Stat > 5 && Character_Mana > 2)
        {
            Character_Mana--;
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
            WallHP += 10;
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
            if (Character_Int_Level % 10 == 9)
            {
                Character_Int += 10;
            }
            else
            {
                Character_Int++;
            }
            Character_Int_Level++;
        }
        else
        {
            Debug.Log("캐릭터 스탯이 부족합니다.");
        }
    }
}
