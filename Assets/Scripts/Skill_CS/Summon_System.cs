using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Summon_System : MonoBehaviour
{
    public Skill_Data skillData;
    public int SummonMaxHP;
    public int SummonAttackDamage;
    public int currentHealth; // 현재 체력
    public GameObject healthTextPrefab;  // 체력을 표시할 텍스트 프리팹
    public GameObject SummonAttackPrefab;  // 투사체 프리팹

    // 자동 공격 관련 추가 필드
    public float SummonAttackRange = 5f;    // 몬스터 탐색 범위
    public float SummonAttackSpeed = 10f;   // 투사체 속도

    private GameObject healthTextInstance; // 텍스트 인스턴스
    private Text healthText;

    private float damageInterval = 1f; // 1초에 한 번 체력이 감소
    private float damageTimer = 0f;

    void Start()
    {
        SummonMaxHP = skillData.ApplySummonHP();
        SummonAttackDamage = skillData.ApplySummonAD();
        currentHealth = SummonMaxHP; // 처음엔 체력이 최대치
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, transform.position, Quaternion.identity);
            healthTextInstance.transform.SetParent(GameObject.Find("Canvas").transform, false);  // 캔버스에 부착
            healthText = healthTextInstance.GetComponent<Text>();
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0f, 0));
            healthTextInstance.transform.position = screenPosition;
            if (healthTextInstance != null)
            {
                healthText.text = currentHealth.ToString();
            }
        }
    }

    void Update()
    {
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            currentHealth--;  // 1초마다 체력 1 감소
            if (healthTextInstance != null)
            {
                healthText.text = currentHealth.ToString();
            }
            Attack();         // 1초마다 공격 실행
            damageTimer = 0f;
        }

        if(currentHealth <= 0)
        {
            DestroyCreate();
        }
    }
    private void Attack()
    {
        // Summon 위치에서 일정 범위 내 몬스터 탐색 (태그 "Monster" 사용)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, SummonAttackRange);
        Monster_Base target = null;
        float closestDistance = Mathf.Infinity;
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Monster"))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    target = col.GetComponent<Monster_Base>();
                }
            }
        }

        if (target != null)
        {
            // 투사체 생성 및 target 방향으로 발사
            GameObject projectile = Instantiate(SummonAttackPrefab, transform.position, Quaternion.identity);

            // SummonAttackDamage 값을 투사체의 attackDamage로 할당
            Summon_Projectile projectileScript = projectile.GetComponent<Summon_Projectile>();
            if (projectileScript != null)
            {
                projectileScript.attackDamage = SummonAttackDamage;
            }
            else
            {
                Debug.LogError("SummonAttackPrefab에 Summon_Projectile 컴포넌트가 없습니다.");
            }

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (target.transform.position - transform.position).normalized;
                rb.velocity = direction * SummonAttackSpeed;
            }
            else
            {
                Debug.LogError("SummonAttackPrefab에 Rigidbody2D 컴포넌트가 없습니다.");
            }
        }
    }
    private void DestroyCreate()
    {
        Debug.Log("소환수가 파괴되었습니다.");
        Destroy(gameObject);  // 몬스터 게임 오브젝트 제거
        if (healthText != null)
        {
            Destroy(healthText);  // 체력 텍스트도 제거
        }
    }

    public void Heal_Summon(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, SummonMaxHP);
        if (healthTextInstance != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }
}
