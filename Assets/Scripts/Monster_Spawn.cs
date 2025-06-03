using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster_Spawn : MonoBehaviour
{
    //몬스터 스폰 변수
    //monsterPrefab = 스폰 위치
    //spawnInterval = 스폰 시간
    //timer = realtime
    public GameObject monsterPrefab;
    public Wall_System_Base timesystem;
    public float spawnInterval = 2f;
    public float timer = 0f;
    


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnMonster();
            timer = 0f;
        }
    }

    void SpawnMonster()
    {
        // 랜덤 위치에서 몬스터 생성
        GameObject monsterInstance = Instantiate(monsterPrefab, new Vector3(Random.Range(-5f, 5f), 8f, 0f), Quaternion.identity);

        // timesystem.PlayTime을 분 단위로 변환하여 1분 이상이면 체력을 두 배로 설정
        if (Mathf.FloorToInt(timesystem.PlayTime / 60f) >= 1)
        {
            Monster_Base monsterBase = monsterInstance.GetComponent<Monster_Base>();
            if (monsterBase != null)
            {
                monsterBase.maxHealth *= 2 * Mathf.FloorToInt(timesystem.PlayTime / 60f);
            }
        }
    }

}
