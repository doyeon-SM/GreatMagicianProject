using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CharacterSaveData
{
    public int Player_level;
    public int Player_currentEXP;
    public int Player_HP;
    public float Player_mana;
    public int Player_gold;
    public int Player_stat;
    public int Player_int;
    public int Player_int_level;
    public int Player_NextEXP;
    public int Player_dust;

    public int[] Player_haveSkills;

    public List<SkillSaveData> learnedSkills;

    public string story_lastStageId;
    public QuestManager.QuestSOProgressSave questSO;  // 퀘스트 카운터 세이브
}

[System.Serializable]
public class SkillSaveData
{
    public string skillName;
    public int damage;
    public int level;
    public int Need_gold;
    public bool Player_know;
}
