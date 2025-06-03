using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    public Skill_Data[] T1allSkillData;
    public Skill_Data[] T2allSkillData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 이미 인스펙터에서 할당된 값이 있다면 재초기화하지 않습니다.
            if (T1allSkillData == null || T1allSkillData.Length == 0 ||
                T2allSkillData == null || T2allSkillData.Length == 0)
            {
                Debug.LogWarning("allSkillData가 인스펙터에서 할당되지 않았습니다.");
                // 필요하다면 Resources.LoadAll<Skill_Data>("...")를 여기서 호출할 수 있습니다.
            }
            else
            {
                Debug.Log("allSkillData 배열이 인스펙터를 통해 할당되었습니다. 총 " + T1allSkillData.Length + "개의 스킬 데이터가 있습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReloadSkillData()
    {
        foreach (Skill_Data skill in T1allSkillData)
        {
            skill.UpdateDamage();
        }
        foreach(Skill_Data skill in T2allSkillData)
        {
            skill.UpdateDamage();
        }
        Debug.Log("SkillManager: 모든 스킬 데이터 업데이트 완료");
    }
}
