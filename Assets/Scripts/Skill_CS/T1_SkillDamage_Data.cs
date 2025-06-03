using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New T1 Skill Damage Data", menuName = "Skill/T1 Skill Damage Data")]
public class T1_SkillDamage_Data : ScriptableObject
{
    [System.Serializable]
    public class T1_SkillDamageEntry
    {
        public string skillName;  // 티어 1 스킬 이름
        public float damageMultiplier;  // 기본 데미지에 곱할 배수
    }

    public List<T1_SkillDamageEntry> t1SkillDamageEntries;  // 모든 티어 1 스킬의 데미지 항목 리스트

    public int CalculateT1SkillDamage(string skillName, List<Skill_Data> baseSkills)
    {
        //Debug.Log($"Calculating damage for skill: {skillName}");

        foreach (var entry in t1SkillDamageEntries)
        {
            if (entry.skillName == skillName)
            {
                int baseDamageSum = 0;

                // 기본 스킬의 데미지 합산
                foreach (var baseSkill in baseSkills)
                {
                    baseDamageSum += baseSkill.damage;
                    //Debug.Log($"Adding base skill damage: {baseSkill.skillName} = {baseSkill.damage}");
                }
                // 최종 데미지 계산
                int finalDamage = Mathf.RoundToInt(baseDamageSum * entry.damageMultiplier);
                //Debug.Log($"Final calculated damage: {finalDamage}");
                return finalDamage;
            }
        }

        //Debug.LogError($"Skill '{skillName}' not found in T1_SkillDamage_Data.");
        return 0;
    }

}
