using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterDropItem : MonoBehaviour
{
    public enum ItemType { Normal, Advanced }
    public ItemType itemType;

    public Score_System scoreSystem;  // 점수 시스템 참조

    // 아이템 클릭 시 실행 (OnMouseDown은 Collider가 있어야 호출됩니다)
    private void OnMouseDown()
    {
        float outcome = Random.value; // 0 ~ 1 사이 난수

        if (itemType == ItemType.Normal && UnderUI_System_Base.Instance.HasEmptySkillSlot())
        {
            // 일반 구체: 50% 확률로 점수 1 획득, 50%로 0tier 스킬 생성
            if (outcome < 0.5f)
            {
                scoreSystem.score += 1;
                Debug.Log("일반 구체: 점수 1 획득");
                // 아이템 효과 적용 후 파괴
                Destroy(gameObject);
            }
            else
            {
                UnderUI_System_Base.Instance.AddSkill();
                Debug.Log("일반 구체: 0tier 스킬 생성");
                // 아이템 효과 적용 후 파괴
                Destroy(gameObject);
            }
        }
        else if (itemType == ItemType.Advanced && UnderUI_System_Base.Instance.HasEmptySkillSlot())
        {
            // 고급 구체: 50% 확률로 점수 3 획득, 25% 확률로 0tier 스킬 생성, 25% 확률로 1tier 스킬 생성
            if (outcome < 0.5f)
            {
                scoreSystem.score += 3;
                Debug.Log("고급 구체: 점수 3 획득");
                Destroy(gameObject);
            }
            else if (outcome < 0.5f + 0.25f)
            {
                //0티어 스킬 생성
                UnderUI_System_Base.Instance.AddSkill();
                Debug.Log("고급 구체: 0tier 스킬 생성");
                Destroy(gameObject);
            }
            else
            {
                //1티어 스킬 생성
                UnderUI_System_Base.Instance.Tier1AddSkill();
                Debug.Log("고급 구체: 1tier 스킬 생성");
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("스킬 슬롯이 부족합니다.");
        }
    }
}
