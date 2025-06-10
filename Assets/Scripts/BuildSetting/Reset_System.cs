using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reset_System : MonoBehaviour
{
    // Character 스크립트가 부착된 오브젝트를 할당 (0Tier 스킬 배열 포함)
    public Character character;
    public SaveSystem save;

    // 각 스킬의 데미지를 저장할 배열
    private int[] initialDamages = {10, 5, 3, 5};

    // SkillReset() 함수: 0Tier 스킬의 레벨과 데미지를 초기값으로 복원
    public void SkillReset()
    {
        if (character != null && character.tier0Skills != null && initialDamages != null)
        {
            for (int i = 0; i < character.tier0Skills.Length; i++)
            {
                if (character.tier0Skills[i] != null)
                {
                    character.tier0Skills[i].level = 1;
                    character.tier0Skills[i].damage = initialDamages[i];
                    character.tier0Skills[i].NeedLevelUP_Gold = 1;
                }
            }
            Debug.Log("모든 0Tier 스킬이 초기화되었습니다.");
            save.SaveGameData();
        }
        else
        {
            Debug.LogError("초기값 배열이 초기화되지 않았거나 character가 할당되지 않았습니다.");
        }
    }
    public void CharacterReset()
    {
        if(character != null)
        {
            character.Character_Level = 1;
            character.Character_EXP = 0;
            character.Character_NextEXP = 10;
            character.Character_Mana = 2;
            character.WallHP = 30;
            character.Character_Gold = 0;
            character.Character_Stat = 0;
            character.Character_Int = 1;
            character.Character_Int_Level = 1;
            Debug.Log("모든 캐릭터 설정이 초기화 되었습니다.");
            save.SaveGameData();
        }
        else
        {
            Debug.LogError("초기값 배열이 초기화되지 않았거나 character가 할당되지 않았습니다.");
        }
    }
    public void SkillknowReset()
    {
        if(character != null && character.tier1Skills != null && character.tier2Skills != null)
        {
            for (int i = 0; i < character.tier1Skills.Length; i++)
            {
                character.tier1Skills[i].isKnow = false;
            }
            for (int i = 0; i < character.tier2Skills.Length; i++)
            {
                character.tier2Skills[i].isKnow = false;
            }
        }
        save.SaveGameData();
    }
}
