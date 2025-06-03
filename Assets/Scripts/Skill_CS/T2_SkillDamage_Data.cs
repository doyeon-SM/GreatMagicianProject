using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New T2 Skill Damage Data", menuName = "Skill/T2 Skill Damage Data")]
public class T2_SkillDamage_Data : ScriptableObject
{
    [System.Serializable]
    public class T2_SkillDamageEntry
    {
        public string skillName;  // 티어 2 스킬 이름
        public float damageMultiplier;  // 기본 데미지에 곱할 배수
    }
    public List<T2_SkillDamageEntry> t2SkillDamageEntries;  // 모든 티어 2 스킬의 데미지 항목 리스트

    public int CalculateT2SkillDamage(string skillName, List<Skill_Data> baseSkills)
    {
        //Debug.Log($"Calculating damage for skill: {skillName}");

        foreach (var entry in t2SkillDamageEntries)
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
                Debug.Log($"Final calculated damage: {finalDamage}");
                return finalDamage;
            }
        }

        Debug.LogError($"Skill '{skillName}' not found in T2_SkillDamage_Data.");
        return 0;
    }
}
