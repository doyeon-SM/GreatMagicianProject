using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wall_System_Create : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI Prefab (내부에 Canvas 없는 Text 프리팹)")]
    public GameObject healthTextPrefab;

    private GameObject healthTextInstance;
    private Text healthText;
    private RectTransform healthRT;

    // Canvas/Camera 캐시
    private Camera cam;
    private RectTransform canvasRT;
    private Transform uiParent;

    private int monstersInContact => contactingMonsters.Count;
    private float damageInterval = 1f;
    private float damageTimer = 0f;

    // 자연 붕괴(2초마다 1)
    private float decayInterval = 2f;
    private float decayTimer = 0f;

    public Skill_Data skillData;

    // 충돌 중 몬스터
    private HashSet<Monster_Base> contactingMonsters = new HashSet<Monster_Base>();

    private void Start()
    {
        // 스탯 적용
        maxHealth = skillData.ApplyCreate();
        currentHealth = maxHealth;

        // === 카메라/캔버스 캐시 ===
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[WallCreate] Main Camera not found.");
            enabled = false; return;
        }

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[WallCreate] Canvas(메인) 를 찾지 못했습니다. Canvas 이름/배치 확인!");
            enabled = false; return;
        }
        uiParent = canvasGO.transform;
        canvasRT = canvasGO.GetComponent<RectTransform>();

        // === HP 텍스트 생성(메인 Canvas 하위) ===
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, uiParent);
            healthText = healthTextInstance.GetComponent<Text>();
            healthRT = healthTextInstance.GetComponent<RectTransform>();
        }

        // 첫 표시
        RefreshHpText();
        if (healthRT != null) UpdateUIPosition(healthRT, transform.position);
    }

    private void Update()
    {
        // 접촉 중이면 주기적 피해
        if (monstersInContact > 0)
        {
            damageTimer += Time.deltaTime;

            // Blind 몬스터 제거(무효 처리)
            contactingMonsters.RemoveWhere(m => m == null || m.isBlind);

            if (damageTimer >= damageInterval)
            {
                TakeDamage(1);
                damageTimer = 0f;

                // 반사 데미지
                if (skillData.skillEffect == Skill_Data.SkillEffect.Burn ||
                    skillData.skillEffect == Skill_Data.SkillEffect.Paralysis)
                {
                    ReflectDamage("Ignis");
                }
                else if (skillData.skillEffect == Skill_Data.SkillEffect.Knockback ||
                         skillData.skillEffect == Skill_Data.SkillEffect.Freezing)
                {
                    ReflectDamage("Aqua");
                }
            }
        }

        // 자연 붕괴(접촉 무관)
        decayTimer += Time.deltaTime;
        if (decayTimer >= decayInterval)
        {
            ReduceHealthFlat(1);
            decayTimer = 0f;
        }

        // UI 위치 추적
        if (healthRT != null && canvasRT != null)
        {
            UpdateUIPosition(healthRT, transform.position);
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            var monster = collision.gameObject.GetComponent<Monster_Base>();
            if (monster != null && !monster.isBlind)
                contactingMonsters.Add(monster);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            var monster = collision.gameObject.GetComponent<Monster_Base>();
            if (monster != null)
                contactingMonsters.Remove(monster);
        }
    }

    // 접촉 수 비례 피해
    public void TakeDamage(int damage)
    {
        currentHealth -= (damage * Mathf.Max(1, monstersInContact));
        RefreshHpText();
    }

    // 고정량 감소(자연 붕괴 등)
    private void ReduceHealthFlat(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        RefreshHpText();
    }

    private void RefreshHpText()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
    }

    private void DestroyCreate()
    {
        Debug.Log("[WallCreate] 벽이 파괴되었습니다.");
        if (healthTextInstance) Destroy(healthTextInstance);   
        Destroy(gameObject);
    }

    // 벽 반사
    private void ReflectDamage(string element)
    {
        foreach (var monster in contactingMonsters)
        {
            if (monster == null) continue;
            monster.ApplyElement(element);
            monster.TakeDamage(skillData.damage);
        }
    }

    public void Heal_Create(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        RefreshHpText();
    }

    private void OnDisable()
    {
        if (healthTextInstance) Destroy(healthTextInstance);
    }
}
