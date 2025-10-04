using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Summon_System : MonoBehaviour
{
    public Skill_Data skillData;
    public int SummonMaxHP;
    public int SummonAttackDamage;
    public int currentHealth;

    [Header("UI Prefab (내부에 Canvas 없는 Text 프리팹)")]
    public GameObject healthTextPrefab;

    [Header("Attack")]
    public GameObject SummonAttackPrefab;  // 투사체 프리팹
    public float SummonAttackRange = 5f;   // 탐색 반경
    public float SummonAttackSpeed = 10f;  // 투사체 속도

    // UI 인스턴스/캐시
    private GameObject healthTextInstance;
    private Text healthText;
    private RectTransform healthRT;
    private Camera cam;
    private RectTransform canvasRT;
    private Transform uiParent;

    // 주기 처리
    private float tickInterval = 1f; // 1초마다 체력 1감소 + 공격 1회
    private float tickTimer = 0f;

    private void Start()
    {
        // 능력치 적용
        SummonMaxHP = skillData.ApplySummonHP();
        SummonAttackDamage = skillData.ApplySummonAD();
        currentHealth = SummonMaxHP;

        // === 메인 카메라/캔버스 캐시 ===
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Summon] Main Camera not found.");
            enabled = false; return;
        }

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[Summon] Canvas(메인) 를 찾지 못했습니다. Canvas 이름/배치 확인!");
            enabled = false; return;
        }
        uiParent = canvasGO.transform;
        canvasRT = canvasGO.GetComponent<RectTransform>();

        // === HP 텍스트 생성 (메인 Canvas 하위) ===
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, uiParent);
            healthText = healthTextInstance.GetComponent<Text>();
            healthRT = healthTextInstance.GetComponent<RectTransform>();
        }

        RefreshHpText();
        if (healthRT != null) UpdateUIPosition(healthRT, transform.position);
    }

    private void Update()
    {
        // UI 위치 계속 추적
        if (healthRT != null && canvasRT != null)
        {
            UpdateUIPosition(healthRT, transform.position);
        }

        // 1초마다: 체력 1 감소 + 공격 1회
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            currentHealth = Mathf.Max(0, currentHealth - 1);
            RefreshHpText();

            Attack();

            tickTimer = 0f;
        }

        if (currentHealth <= 0)
        {
            DestroyCreate();
        }
    }

    // 월드→스크린→Canvas local 로 변환하여 anchoredPosition 세팅
    private void UpdateUIPosition(RectTransform targetRT, Vector3 worldPos)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out var localPos))
        {
            targetRT.anchoredPosition = localPos;
        }
    }

    private void RefreshHpText()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
    }

    private void Attack()
    {
        // 반경 내 가장 가까운 Monster 찾기
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, SummonAttackRange);
        Monster_Base target = null;
        float best = float.PositiveInfinity;

        foreach (var c in cols)
        {
            if (!c.CompareTag("Monster")) continue;
            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d < best)
            {
                best = d;
                target = c.GetComponent<Monster_Base>();
            }
        }

        if (target == null) return;
        if (SummonAttackPrefab == null) { Debug.LogWarning("[Summon] SummonAttackPrefab is null."); return; }

        // 투사체 생성 및 설정
        GameObject proj = Instantiate(SummonAttackPrefab, transform.position, Quaternion.identity);

        var projLogic = proj.GetComponent<Summon_Projectile>();
        if (projLogic != null) projLogic.attackDamage = SummonAttackDamage;
        else Debug.LogError("[Summon] SummonAttackPrefab에 Summon_Projectile 컴포넌트가 없습니다.");

        var rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            rb.velocity = dir * SummonAttackSpeed;
        }
        else
        {
            Debug.LogError("[Summon] SummonAttackPrefab에 Rigidbody2D 컴포넌트가 없습니다.");
        }
    }

    private void DestroyCreate()
    {
        Debug.Log("[Summon] 소환수가 파괴되었습니다.");
        if (healthTextInstance) Destroy(healthTextInstance); 
        Destroy(gameObject);
    }

    public void Heal_Summon(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, SummonMaxHP);
        RefreshHpText();
    }

    private void OnDisable()
    {
        if (healthTextInstance) Destroy(healthTextInstance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 에디터에서 공격 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, SummonAttackRange);
    }
#endif
}
