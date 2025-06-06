using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Around_OrbitingProjectile : MonoBehaviour
{
    public Skill_Data skillData;
    public Transform center;
    public float radius;
    public float angularSpeed;
    public float startAngle; // 외부에서 설정
    private float currentAngle;

    private void Start()
    {
        currentAngle = startAngle; // 시작 각도 적용
    }

    private void Update()
    {
        if (center == null) return;

        currentAngle += angularSpeed * Time.deltaTime;
        float rad = Mathf.Deg2Rad * currentAngle;

        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
        transform.position = center.position + offset;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Monster"))
        {
            Monster_Base monster = col.GetComponent<Monster_Base>();
            if (monster != null)
            {
                skillData.ApplyDamage(monster);
            }
        }
    }
}
