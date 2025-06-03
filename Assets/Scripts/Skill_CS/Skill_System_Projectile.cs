using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill_System_Projectile : MonoBehaviour
{
    public Skill_Data skillData;  // 해당 스킬의 데이터 참조
    private bool hasCollided = false;  // 첫 충돌 여부를 확인하는 변수

    private void Start()
    {
        // Skill_System_Projectile이 자기 자신에게 적용되는 경우 무한 루프 방지
        if (skillData != null && skillData.attackPrefab == null)
        {
            Debug.LogError("SkillData or attackPrefab is missing in Skill_System_Projectile.");
            return; // 이 프리팹이 제대로 설정되지 않았을 때, 투사체 생성을 방지
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasCollided) return;  // 이미 충돌 처리된 경우 아무 작업도 하지 않음

        if (collision.CompareTag("Monster") && 
            (skillData.skillType == Skill_Data.SkillType.Projectile || skillData.skillType == Skill_Data.SkillType.Scattered))
        {
            // 충돌 위치를 Debug.Log로 출력
            Debug.Log("Collision detected at position: " + transform.position);
            Vector3 hitPosition = collision.transform.position;
            Monster_Base monster = collision.GetComponent<Monster_Base>();
            

            switch (skillData.skillEffect.ToString())
            {
                case "Rolling":
                    if (monster != null)
                    {
                        skillData.ApplyDamage(monster);  // 데미지 적용
                    }
                    break;
                case "Explosion":
                    hasCollided = true;
                    CreateRange(hitPosition);
                    Destroy(gameObject);
                    break;
                // 다른 스킬에 대한 처리 추가 가능
                default:
                    hasCollided = true;
                    if (monster != null)
                    {
                        skillData.ApplyDamage(monster);  // 데미지 적용
                    }
                    Destroy(gameObject);  // Ignis 투사체는 충돌 후 사라짐
                    break;
            }
        }
        else if(collision.CompareTag("Monster") && skillData.skillType == Skill_Data.SkillType.Chain)
        {
            // 충돌 위치를 Debug.Log로 출력
            Debug.Log("Collision detected at position: " + transform.position);
            Vector3 hitPosition = collision.transform.position;
            Monster_Base monster = collision.GetComponent<Monster_Base>();

            hasCollided = true;
            if(monster != null)
            {
                //첫 몬스터에 데미지 적용
                skillData.ApplyDamage(monster);
                //검색 범위 결정: rangePrefab의 콜라이더 반경 사용
                float searchRadius = 0f;

                if(skillData.rangePrefab != null)
                {
                    GameObject tempRange = Instantiate(skillData.rangePrefab, hitPosition, Quaternion.identity);
                    CircleCollider2D circle = tempRange.GetComponent<CircleCollider2D>();
                    if (circle != null)
                        searchRadius = circle.radius * tempRange.transform.localScale.x;
                    Destroy(tempRange);
                }

                //주어진 반경 내의 몬스터들 탐색(현재 몬스터 제외)
                Collider2D[] colliders = Physics2D.OverlapCircleAll(hitPosition, searchRadius);
                Monster_Base nextTarget = null;
                float minDistance = Mathf.Infinity;
                foreach(Collider2D col in colliders)
                {
                    if (col.CompareTag("Monster"))
                    {
                        Monster_Base potential = col.GetComponent<Monster_Base>();
                        if (potential != null && potential != monster)
                        {
                            float distance = Vector2.Distance(hitPosition, potential.transform.position);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nextTarget = potential;
                            }
                        }
                    }
                }
                if (nextTarget != null)
                {
                    // 새 투사체 생성 후, nextTarget을 향해 발사
                    GameObject newProjectile = Instantiate(skillData.attackPrefab, hitPosition, Quaternion.identity);
                    Rigidbody2D rb = newProjectile.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 direction = (nextTarget.transform.position - hitPosition).normalized;
                        rb.velocity = direction * 15f;  // 속도 조절 가능
                    }
                }
            }
            // 체인 효과는 한 번의 추가 점격 후 종료
            Destroy(gameObject);
        }
    }

    private void CreateRange(Vector3 hitPosition)
    {
        if (skillData != null && skillData.rangePrefab != null)
        {
            GameObject rangeObject = Instantiate(skillData.rangePrefab, hitPosition, Quaternion.identity);
            Skill_Range_System rangeSystem = rangeObject.GetComponent<Skill_Range_System>();

            if (rangeSystem != null)
            {
                rangeSystem.skillData = skillData;
                rangeSystem.StartRangeEffect();
            }
            else
            {
                Debug.LogError("Skill_Range_System component is missing on the rangePrefab.");
            }
        }
        else
        {
            Debug.LogError("rangePrefab is not assigned in SkillData.");
        }
    }
}
