using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Summon_Projectile : MonoBehaviour
{
    public int attackDamage = 1;
    private bool hasCollided = false;  // 첫 충돌 여부를 확인하는 변수
    private void Start()
    {
        // 투사체가 5초 이내에 충돌하지 않으면 자동 파괴 (메모리 누수 방지)
        Destroy(gameObject, 5f);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasCollided) return;  // 이미 충돌 처리된 경우 아무 작업도 하지 않음
        if (collision.CompareTag("Monster"))
        {
            hasCollided = true;
            Monster_Base monster = collision.GetComponent<Monster_Base>();
            if (monster != null)
            {
                monster.TakeDamage(attackDamage);
                Debug.Log("Summon_Projectile: Monster hit. Damage applied: " + attackDamage);
            }
            // 충돌 후 투사체 파괴
            Destroy(gameObject);
        }
    }
}
