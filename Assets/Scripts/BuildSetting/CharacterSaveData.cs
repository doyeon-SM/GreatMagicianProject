using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CharacterSaveData
{
    public int Player_level;
    public int Player_currentEXP;
    public int Player_HP;
    public int Player_mana;
    public int Player_gold;
    public int Player_stat;
    public int Player_int;
    public int Player_int_level;
    public int Player_NextEXP;

    public List<SkillSaveData> learnedSkills;
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
