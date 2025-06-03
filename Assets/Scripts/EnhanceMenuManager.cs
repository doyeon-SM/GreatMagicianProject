using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceMenuManager : MonoBehaviour
{
    public Character character;  // 캐릭터 정보 (포인트, 스킬 데이터 포함)
    public EnhanceSkillUI[] enhanceSkillUIs;  // 0Tier 스킬에 대한 UI 패널 배열

    public Text Character_PointText;    //남은 캐릭터 포인트

    void Start()
    {
        UpdateAllSkillUI();
    }

    // 각 스킬 UI를 업데이트합니다.
    public void UpdateAllSkillUI()
    {
        for (int i = 0; i < enhanceSkillUIs.Length; i++)
        {
            Skill_Data skillData = character.GetSkillData(i);
            enhanceSkillUIs[i].UpdateUI(skillData);
        }
        Character_PointText.text = "Gold: "+character.Character_Gold.ToString();
    }

    // 레벨업 버튼이 눌렸을 때 호출되는 메서드
    // 각 UI의 Button OnClick 이벤트에서 해당 인덱스를 인자로 전달할 수 있습니다.
    public void OnLevelUpSkill(int skillIndex)
    {
        character.LevelUpSkill(skillIndex);
        UpdateAllSkillUI();
        
    }
}
