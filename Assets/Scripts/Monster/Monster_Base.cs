using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Monster_Base : MonoBehaviour
{
    public Character character;

    public int maxHealth = 10;
    private int currentHealth;
    public MonsterElement monsterElement;

    [Header("UI Prefabs (Canvas 없는 Item 프리팹)")]
    public GameObject healthTextPrefab;   // Text만 있는 프리팹(내부 Canvas 없음)
    public GameObject damageTextPrefab;   // Text만 있는 프리팹(내부 Canvas 없음)

    [Header("Refs")]
    public Score_System scoreSystem;

    // 내부 상태
    private GameObject healthTextInstance;
    private Text healthText;
    private RectTransform healthRT;

    // 캐시
    private Camera cam;
    private RectTransform canvasRT;   // 메인 Canvas의 RectTransform
    private Transform uiParent;       // 메인 Canvas Transform

    public bool MonsterIsDead => currentHealth <= 0;

    // 이동/효과
    public float moveSpeed = 1.0f;
    private float slowMultiplier = 1.0f;
    private bool isMoveEffect = false;
    private Vector3 MoveEffectVelocity;
    private float MoveEffectTimeRemaining = 0f;

    public bool isBlind = false;

    // 드롭
    public GameObject normalSpherePrefab;
    public GameObject advancedSpherePrefab;

    // DOT
    private Coroutine periodicDamageCoroutine;

    public enum MonsterElement { None, Ignis, Aqua, Ventus, Terra }

    void Start()
    {
        currentHealth = maxHealth;

        // === 카메라/캔버스 캐시 ===
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Monster_Base] Main Camera not found.");
            enabled = false; return;
        }

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[Monster_Base] Canvas(메인) 를 찾지 못했습니다. Canvas 이름/배치를 확인하세요.");
            enabled = false; return;
        }
        uiParent = canvasGO.transform;
        canvasRT = canvasGO.GetComponent<RectTransform>();

        // === HP 텍스트 생성 (Canvas 자식으로 바로) ===
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, uiParent);
            healthText = healthTextInstance.GetComponent<Text>();
            healthRT = healthTextInstance.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        // 이동
        if (isMoveEffect)
        {
            transform.Translate(MoveEffectVelocity * Time.deltaTime);
            MoveEffectTimeRemaining -= Time.deltaTime;
            if (MoveEffectTimeRemaining <= 0f) isMoveEffect = false;
        }
        else
        {
            transform.Translate(Vector3.down * moveSpeed * slowMultiplier * Time.deltaTime);
        }

        // HP UI 갱신
        if (healthRT != null && canvasRT != null)
        {
            UpdateUIPosition(healthRT, transform.position + new Vector3(0f, 0.0f, 0f));
            if (healthText != null) healthText.text = currentHealth.ToString();
        }

        if (MonsterIsDead) Die();
    }

    // Screen → UI local 변환으로 anchoredPosition 세팅
    private void UpdateUIPosition(RectTransform targetRT, Vector3 worldPos)
    {
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        Vector2 localPos;
        // SS-Camera Canvas에서는 cam 전달, Overlay면 null
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, cam, out localPos))
        {
            targetRT.anchoredPosition = localPos;
        }
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = damage + (character.Character_Int / 10);
        currentHealth -= finalDamage;

        ShowDamageText(finalDamage);

        if (currentHealth <= 0) Die();
    }

    private void ShowDamageText(int damageValue)
    {
        if (damageTextPrefab == null || canvasRT == null) return;

        // Canvas 자식으로 생성
        GameObject dmgTextObj = Instantiate(damageTextPrefab, uiParent);
        var dmgText = dmgTextObj.GetComponent<Text>();
        var dmgRT = dmgTextObj.GetComponent<RectTransform>();

        if (dmgText) dmgText.text = damageValue.ToString();

        // 초기 위치 배치
        UpdateUIPosition(dmgRT, transform.position + new Vector3(0f, 0.5f, 0f));

        Destroy(dmgTextObj, 1f);
    }
    // 이동/슬로우/도트 동일
    public void ApplyMoveEffect(Vector3 velocity, float duration) { isMoveEffect = true; MoveEffectVelocity = velocity; MoveEffectTimeRemaining = duration; }
    public void ApplySlowEffect(float slowFactor) { slowMultiplier = slowFactor; }
    public void RemoveSlowEffect() { slowMultiplier = 1.0f; }

    public void ApplyContinuousDamage(float duration, int dam)
    {
        periodicDamageCoroutine = StartCoroutine(ApplyPeriodicDamage(duration, dam, 0.5f));
    }
    private IEnumerator ApplyPeriodicDamage(float duration, int dam, float interval)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            if (this == null || gameObject == null) yield break;
            TakeDamage(dam);
            elapsedTime += interval;
            yield return new WaitForSeconds(interval);
        }
    }

    private void Die()
    {
        scoreSystem.score += maxHealth;

        if (QuestManager.Instance != null)
            QuestManager.Instance.ReportMonsterKill(monsterElement);

        float roll = Random.value;
        if (roll < 0.05f && advancedSpherePrefab != null)
        {
            var drop = Instantiate(advancedSpherePrefab, transform.position, Quaternion.identity);
            var di = drop.GetComponent<MonsterDropItem>();
            if (di) { di.itemType = MonsterDropItem.ItemType.Advanced; di.scoreSystem = scoreSystem; }
        }
        else if (roll < 0.25f && normalSpherePrefab != null)
        {
            var drop = Instantiate(normalSpherePrefab, transform.position, Quaternion.identity);
            var di = drop.GetComponent<MonsterDropItem>();
            if (di) { di.itemType = MonsterDropItem.ItemType.Normal; di.scoreSystem = scoreSystem; }
        }

        if (healthTextInstance) Destroy(healthTextInstance);
        Destroy(gameObject);
    }

    public void ApplyElement(string element)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (!sr) return;
        switch (element)
        {
            case "Ignis": monsterElement = MonsterElement.Ignis; sr.color = Color.red; break;
            case "Aqua": monsterElement = MonsterElement.Aqua; sr.color = Color.blue; break;
            case "Ventus": monsterElement = MonsterElement.Ventus; sr.color = Color.gray; break;
            case "Terra": monsterElement = MonsterElement.Terra; sr.color = Color.green; break;
            default: monsterElement = MonsterElement.None; break;
        }
    }
    public void ApplyBlindEffect(bool tmp) { isBlind = tmp; }
    public void RemoveBlindEffect() { isBlind = false; }
}
