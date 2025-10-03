using UnityEngine;

public class MonsterDropItem : MonoBehaviour
{
    public enum ItemType { Normal, Advanced }
    public ItemType itemType;
    public Score_System scoreSystem;

    [Header("Rendering")]
    [Tooltip("아이템 전용 Sorting Layer 이름 (Project Settings > Tags and Layers에서 생성)")]
    public string sortingLayerName = "Items";
    [Tooltip("레이어 내 순서 (다른 월드 스프라이트보다 충분히 높게)")]
    public int sortingOrder = 5000;

    private void Awake()
    {
        // 자신 및 자식의 SpriteRenderer를 모두 올려서 월드 내 최상단에 배치
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }
    }

    private void Update()
    {
        if (InputStarted())
        {
            Vector3 inputPos = Camera.main.ScreenToWorldPoint(GetInputPosition());
            inputPos.z = 0f;

            Collider2D hit = Physics2D.OverlapPoint(inputPos);
            if (hit != null && hit.gameObject == gameObject)
            {
                HandleItemEffect();
            }
        }
    }

    private bool InputStarted()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    private Vector3 GetInputPosition()
    {
#if UNITY_EDITOR
        return Input.mousePosition;
#else
        return Input.GetTouch(0).position;
#endif
    }

    private void HandleItemEffect()
    {
        float outcome = Random.value;

        if (itemType == ItemType.Normal && UnderUI_System_Base.Instance.HasEmptySkillSlot())
        {
            if (outcome < 0.5f)
            {
                scoreSystem.score += 100;
                Debug.Log("일반 구체: 점수 1 획득");
            }
            else
            {
                UnderUI_System_Base.Instance.AddSkill();
                Debug.Log("일반 구체: 0tier 스킬 생성");
            }
            Destroy(gameObject);
        }
        else if (itemType == ItemType.Advanced && UnderUI_System_Base.Instance.HasEmptySkillSlot())
        {
            if (outcome < 0.5f)
            {
                scoreSystem.score += 300;
                Debug.Log("고급 구체: 점수 3 획득");
            }
            else if (outcome < 0.75f)
            {
                UnderUI_System_Base.Instance.AddSkill();
                Debug.Log("고급 구체: 0tier 스킬 생성");
            }
            else
            {
                UnderUI_System_Base.Instance.Tier1AddSkill();
                Debug.Log("고급 구체: 1tier 스킬 생성");
            }
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("스킬 슬롯이 부족합니다.");
        }
    }
}
