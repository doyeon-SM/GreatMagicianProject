using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wall_System_Create : MonoBehaviour
{
    public int maxHealth = 10; // 벽의 최대 체력
    public int currentHealth; // 현재 체력
    public GameObject healthTextPrefab;  // 체력을 표시할 텍스트 프리팹
    private GameObject healthTextInstance; // 텍스트 인스턴스
    private Text healthText;

    private int monstersInContact => contactingMonsters.Count;  // 현재 벽에 충돌 중인 몬스터의 개수
    private float damageInterval = 1f; // 1초에 한 번 체력이 감소
    private float damageTimer = 0f;

    public Skill_Data skillData;

    // Start is called before the first frame update
    void Start()
    {
        maxHealth = skillData.ApplyCreate();
        currentHealth = maxHealth; // 처음엔 체력이 최대치
        if (healthTextPrefab != null)
        {
            healthTextInstance = Instantiate(healthTextPrefab, transform.position, Quaternion.identity);
            healthTextInstance.transform.SetParent(GameObject.Find("Canvas").transform, false);  // 캔버스에 부착
            healthText = healthTextInstance.GetComponent<Text>();
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0f, 0));
            healthTextInstance.transform.position = screenPosition;
        }
        TakeDamage(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (monstersInContact > 0)
        {
            damageTimer += Time.deltaTime;
            //Blind 효과에 따른 데미지 처리
            contactingMonsters.RemoveWhere(monster => monster == null || monster.isBlind);

            if (damageTimer >= damageInterval)
            {
                TakeDamage(1);  // 1초마다 체력 1 감소
                damageTimer = 0f;
                //효과에 따른 반사데미지
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
        // 체력이 0 이하가 되면 게임 종료
        if (currentHealth <= 0)
        {
            DestroyCreate();
        }
    }

    // 적이 충돌했을 때 호출
    private HashSet<Monster_Base> contactingMonsters = new HashSet<Monster_Base>();
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            Monster_Base monster = collision.gameObject.GetComponent<Monster_Base>();
            if (monster != null && !monster.isBlind)
            {
                contactingMonsters.Add(monster);
            }
        }
    }

    // 적이 벽에서 떨어질 때 호출
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            Monster_Base monster = collision.gameObject.GetComponent<Monster_Base>();
            if (monster != null && contactingMonsters.Contains(monster))
            {
                contactingMonsters.Remove(monster); // 무조건 제거 (상태가 바뀌었더라도)
            }
        }
    }
    // 체력을 감소시키는 함수
    public void TakeDamage(int damage)
    {
        currentHealth = currentHealth - (damage * monstersInContact);
        if (healthTextInstance != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }
    // 체력을 UI로 업데이트하는 함수
    
    private void DestroyCreate()
    {
        Debug.Log("벽이 파괴되었습니다.");
        Destroy(gameObject);  // 몬스터 게임 오브젝트 제거
        if (healthText != null)
        {
            Destroy(healthText);  // 체력 텍스트도 제거
        }
    }

    //벽 반사함수
    private void ReflectDamage(string element)
    {
        foreach (Monster_Base monster in contactingMonsters)
        {
            if (monster != null)
            {
                monster.ApplyElement(element);
                monster.TakeDamage(skillData.damage); //반사 데미지
            }
        }
    }
}
