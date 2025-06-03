using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnhanceSkillUI : MonoBehaviour
{
    public Image skillIcon;
    public Text skillNameText;
    public Text skillLevelText;
    public Text skillDamageText;
    public Text skillDescriptionText;
    public Button levelUpButton;
    public Text NeedGold;

    public int skillIndex;

    // UI 업데이트 메서드: 해당 스킬 데이터를 받아 UI 구성요소를 갱신합니다.
    public void UpdateUI(Skill_Data skillData)
    {
        if (skillData != null)
        {
            skillIcon.sprite = skillData.skillIcon;
            skillNameText.text = skillData.skillName;
            skillLevelText.text = "Level: " + skillData.level.ToString();
            skillDamageText.text = "Damage: " + skillData.damage.ToString();
            skillDescriptionText.text = skillData.skillscript;
            NeedGold.text = "필요한 Gold: "+skillData.NeedLevelUP_Gold.ToString();
        }
    }
}
