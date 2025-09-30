using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Monster_Base : MonoBehaviour
{
    public Character character;

    public int maxHealth = 10;  // 몬스터의 최대 체력
    private int currentHealth;  // 몬스터의 현재 체력
    public MonsterElement monsterElement;
    public GameObject healthTextPrefab;  // 체력을 표시할 텍스트 프리팹
    public Score_System scoreSystem;
    private GameObject healthTextInstance; // 텍스트 인스턴스
    private Text healthText;
    public bool MonsterIsDead => currentHealth <= 0;  // 몬스터가 죽었는지 확인하는 속성

    // 이동 관련 변수 추가
    public float moveSpeed = 1.0f;           // 기본 이동 속도
    private float slowMultiplier = 1.0f;    // 슬로우 효과 적용 배수
    // 넉백 관련 변수
    private bool isMoveEffect = false;
    private Vector3 MoveEffectVelocity;
    private float MoveEffectTimeRemaining = 0f;
    //실명 관련 변수
    public bool isBlind = false;

    // 드롭 아이템 prefab (Inspector에서 할당)
    public GameObject normalSpherePrefab;   // 20% 확률 드롭 (일반 구체)
    public GameObject advancedSpherePrefab;   // 5% 확률 드롭 (고급 구체)
    // 지속 데미지
    private Coroutine periodicDamageCoroutine;

    public GameObject damageTextPrefab;

    public enum MonsterElement
    {
        None,
        Ignis,
        Aqua,
        Ventus,
        Terra
    }
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;  // 시작할 때 체력을 최대 체력으로 설정
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, transform.position, Quaternion.identity);
            healthTextInstance.transform.SetParent(GameObject.Find("Canvas").transform, false);  // 캔버스에 부착
            healthText = healthTextInstance.GetComponent<Text>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 넉백 상태이면 knockbackVelocity로 이동, 아니라면 아래로 걷기
        if (isMoveEffect)
        {
            transform.Translate(MoveEffectVelocity * Time.deltaTime);
            MoveEffectTimeRemaining -= Time.deltaTime;
            if (MoveEffectTimeRemaining <= 0f)
            {
                isMoveEffect = false;
            }
        }
        else
        {
            transform.Translate(Vector3.down * moveSpeed * slowMultiplier * Time.deltaTime);
        }
        // 체력 텍스트 위치를 몬스터의 바로 위로 설정
        if (healthTextInstance != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0f, 0)); // 몬스터 위에 위치
            healthTextInstance.transform.position = screenPosition;

            // 현재 체력 표시
            healthText.text = currentHealth.ToString();
        }

        // 몬스터가 죽었으면 게임 오브젝트 제거
        if (MonsterIsDead)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = damage + (character.Character_Int / 10);
        currentHealth -= finalDamage;
        Debug.Log("몬스터가 데미지를 받았습니다! 현재 체력: " + currentHealth);

        // 데미지 텍스트 출력
        ShowDamageText(finalDamage);
        if (currentHealth <= 0)
        {
            Die();  // 체력이 0 이하이면 죽음 처리
        }
    }
    private void ShowDamageText(int damageValue)
    {
        if (damageTextPrefab == null) return;

        GameObject dmgTextObj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        dmgTextObj.transform.SetParent(GameObject.Find("Canvas").transform, false);

        Text dmgText = dmgTextObj.GetComponent<Text>();
        if (dmgText != null)
        {
            dmgText.text = damageValue.ToString();
        }

        // 화면 좌표에 배치
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0.5f, 0));
        dmgTextObj.transform.position = screenPosition;

        // 1초 후 삭제
        Destroy(dmgTextObj, 1f);
    }

    // 넉백 효과 적용 메서드: knockbackVelocity는 넉백 방향과 크기를 나타내고, duration은 넉백 지속 시간입니다.
    public void ApplyMoveEffect(Vector3 velocity, float duration)
    {
        isMoveEffect = true;
        MoveEffectVelocity = velocity;
        MoveEffectTimeRemaining = duration;
    }
    // 슬로우 효과 적용: slowFactor 값이 0.5면 이동속도가 50%로 감소
    public void ApplySlowEffect(float slowFactor)
    {
        slowMultiplier = slowFactor;
    }

    // 슬로우 효과 해제: 원래 속도로 복원
    public void RemoveSlowEffect()
    {
        slowMultiplier = 1.0f;
    }
    // 지속 데미지 효과: 코루틴 시작
    public void ApplyContinuousDamage(float duration, int dam)
    {
        periodicDamageCoroutine = StartCoroutine(ApplyPeriodicDamage(duration, dam, 0.5f));
    }
    // 지속 데미지 효과: 코루틴 함수(지속시간, 데미지, 틱)
    private IEnumerator ApplyPeriodicDamage(float duration, int dam, float interval)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            if (this == null || gameObject == null)
            {
                yield break; // 오브젝트가 파괴된 경우 코루틴 종료
            }
            TakeDamage(dam);
            elapsedTime += interval;
            yield return new WaitForSeconds(interval);
        }
    }
    private void Die()
    {
        // 기본 점수 증가
        scoreSystem.score += maxHealth;
        Debug.Log("몬스터가 죽었습니다! Score: " + scoreSystem.score);

        // 아이템 드롭 확률 처리
        // advanced: 5% chance, normal: 20% chance (서로 배타적으로 처리)
        float roll = Random.value;  // 0~1 사이 난수
        if (roll < 0.05f)
        {
            // 5% 확률: 고급 구체 드롭
            if (advancedSpherePrefab != null)
            {
                GameObject drop = Instantiate(advancedSpherePrefab, transform.position, Quaternion.identity);
                // DropItem 스크립트의 itemType을 Advanced로 설정
                MonsterDropItem dropItem = drop.GetComponent<MonsterDropItem>();
                if (dropItem != null)
                {
                    dropItem.itemType = MonsterDropItem.ItemType.Advanced;
                    // scoreSystem와 character 등 필요한 참조를 전달
                    dropItem.scoreSystem = scoreSystem;
                }
            }
        }
        else if (roll < 0.05f + 0.20f)
        {
            // 20% 확률: 일반 구체 드롭
            if (normalSpherePrefab != null)
            {
                GameObject drop = Instantiate(normalSpherePrefab, transform.position, Quaternion.identity);
                // DropItem 스크립트의 itemType을 Normal로 설정
                MonsterDropItem dropItem = drop.GetComponent<MonsterDropItem>();
                if (dropItem != null)
                {
                    dropItem.itemType = MonsterDropItem.ItemType.Normal;
                    dropItem.scoreSystem = scoreSystem;
                }
            }
        }

        Destroy(gameObject);
        if (healthTextInstance != null)
        {
            Destroy(healthTextInstance);
        }
    }

    public void ApplyElement(string element)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        switch(element)
        {
            case "Ignis":
                monsterElement = MonsterElement.Ignis;
                sr.color = Color.red;
                break;
            case "Aqua":
                monsterElement = MonsterElement.Aqua;
                sr.color = Color.blue;
                break;
            case "Ventus":
                monsterElement = MonsterElement.Ventus;
                sr.color = Color.gray;
                break;
            case "Terra":
                monsterElement = MonsterElement.Terra;
                sr.color = Color.green;
                break;
            default:
                monsterElement = MonsterElement.None;
                break;
        }
    }

    public void ApplyBlindEffect(bool tmp)
    {
        isBlind = tmp;
    }
    public void RemoveBlindEffect()
    {
        isBlind = false;
    }
}
