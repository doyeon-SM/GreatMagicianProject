using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill Combination", menuName = "Skill/Skill Combination Data")]
public class Skill_Combination_Data : ScriptableObject
{
    public Skill_Data[] baseSkills;  // 조합할 기본 스킬들
    public Skill_Data resultSkill;   // 조합 결과로 생성될 스킬

    public bool IsCombination(Skill_Data skill1, Skill_Data skill2)
    {
        // 순서에 상관없이 정확히 두 스킬이 일치하는지 확인
        return (baseSkills.Length == 2) &&
               ((skill1 == baseSkills[0] && skill2 == baseSkills[1]) ||
                (skill1 == baseSkills[1] && skill2 == baseSkills[0]));
    }

}
