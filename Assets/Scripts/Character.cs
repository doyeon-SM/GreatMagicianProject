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
    public Skill_Data[] tier0Skills; // 0Tier 스킬 배열 (예: 4개)
    public Skill_Data[] tier1Skills;
    public Skill_Data[] tier2Skills;

    public Skill_Combination_Data[] tier1SkillsCombination;
    public Skill_Combination_Data[] tier2SkillsCombination;

    public int Character_NextEXP = 10;

    // 해당 인덱스의 스킬 레벨을 올리고 데미지를 증가시키는 메서드
    public void LevelUpSkill(int index)
    {
        if (index >= 0 && index < tier0Skills.Length)
        {
            // 포인트가 충분한 경우
            if (Character_Gold >= tier0Skills[index].NeedLevelUP_Gold)
            {
                Character_Gold -= tier0Skills[index].NeedLevelUP_Gold;  // 레벨업에 필요한 골드 소모
                if(tier0Skills[index].level%10 == 0)
                {
                    tier0Skills[index].NeedLevelUP_Gold += tier0Skills[index].level / 10 * 10;
                    tier0Skills[index].damage *= 2;
                }
                else
                {
                    tier0Skills[index].NeedLevelUP_Gold += 2;
                    // 스킬의 데미지를 레벨업에 따라 증가시키는 예시 (증가량은 필요에 따라 조정)
                    tier0Skills[index].damage += 1;
                }
                
                tier0Skills[index].level++; // 스킬 레벨 증가                

                Debug.Log($"{tier0Skills[index].skillName} 레벨 업! 현재 레벨: {tier0Skills[index].level}");
            }
            else
            {
                Debug.Log("골드가 부족합니다.");
            }
        }
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
