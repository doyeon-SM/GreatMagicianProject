using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenuUI : MonoBehaviour
{
    public Character cha;

    public Text CharacterLevelText;
    public Text CharacterEXPText;
    public Text CharacterIntText;
    public Text CharacterManaText;
    public Text CharacterWallHPText;
    public Text CharacterStatPointText;

    public Button CharacterManaUpgradeButton;

    public void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if(cha != null)
        {
            CharacterLevelText.text = "캐릭터 레벨: " + cha.Character_Level.ToString();
            CharacterEXPText.text = "EXP: " + cha.Character_EXP.ToString() + " / " + cha.Character_NextEXP.ToString();
            CharacterIntText.text = "캐릭터 학습력: " + cha.Character_Int.ToString();
            CharacterManaText.text = "Mana: " + cha.Character_Mana.ToString();
            CharacterWallHPText.text = "WallHP: " + cha.WallHP.ToString();
            CharacterStatPointText.text = "보유한 스탯: " + cha.Character_Stat.ToString();
        }
        if (CharacterManaUpgradeButton != null) CharacterManaUpgradeButton.gameObject.SetActive(cha.Character_Level >= 10 && cha.Character_Stat >= 5);
    }
}
