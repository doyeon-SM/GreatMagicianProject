using System.Collections;
using UnityEngine;

public class Monster_Spawn : MonoBehaviour
{
    [Header("Normal Monster")]
    [Tooltip("일반 몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Header("Boss Monsters (10,20,30...웨이브 순서)")]
    public GameObject[] bossPrefabs;

    [Header("Wave Settings")]
    public float waveDuration = 10f;

    [Tooltip("시작 웨이브(보통 1)")]
    public int currentWave = 1;

    [Header("Spawn Area")]
    [Tooltip("일반/보스 스폰 X좌표 최소~최대, Y는 고정")]
    public Vector2 spawnXRange = new Vector2(-5f, 5f);
    public float spawnY = 8f;

    [Header("Debug")]
    [SerializeField] private int targetSpawnCountThisWave; // 이번 웨이브 목표 스폰 수(일반 몬스터)
    [SerializeField] private int spawnedThisWave;          // 이번 웨이브 실제 스폰된 수
    [SerializeField] private float waveTimer;              // 웨이브 경과 시간(초)
    [SerializeField] private bool isBossWave;              // 보스 웨이브 여부
    [SerializeField] private GameObject aliveBoss;         // 현재 살아있는 보스 인스턴스(있으면 유지)

    // 규칙: 1웨이브 체력=10, 11웨이브=20, 21웨이브=40 ...
    // => health = baseHealth(=10) * 2^( (wave-1)/10 )
    [Header("HP Scaling")]
    public int baseHealthPerTier = 10; // 1~10웨이브 구간의 기본 체력(=10)

    void Start()
    {
        BeginWave(currentWave);
    }

    void Update()
    {
        if (isBossWave)
        {
            // 일반 몬스터는 소환하지 않음. 보스가 처치되어야 다음 웨이브로 진행.
            // 보스 생존 여부 확인(보스가 파괴되면 참조가 null이 됨)
            if (aliveBoss == null)
            {
                NextWave();
            }
            return;
        }

        // 일반 웨이브 진행(10초 타이머)
        waveTimer += Time.deltaTime;

        // 10초 내에 목표 수만큼 고르게 소환
        if (targetSpawnCountThisWave > 0 && spawnedThisWave < targetSpawnCountThisWave)
        {
            // 스폰 타이밍 계산: 현재 진행 비율 * 목표개수 > 이미 스폰한 개수 이면 추가 스폰
            // 즉, 균등 배분(10초 동안 일정 간격)으로 떨어지도록 함.
            float expectedSpawnCount = Mathf.Floor((waveTimer / waveDuration) * targetSpawnCountThisWave);
            while (spawnedThisWave < expectedSpawnCount)
            {
                SpawnNormal();
                spawnedThisWave++;
            }
        }

        // 웨이브 시간 끝났으면 다음 웨이브로
        if (waveTimer >= waveDuration)
        {
            NextWave();
        }
    }

    void BeginWave(int wave)
    {
        // 초기화
        waveTimer = 0f;
        spawnedThisWave = 0;
        aliveBoss = null;

        // 보스 웨이브 판단(10,20,30...)
        isBossWave = (wave % 10 == 0);

        if (isBossWave)
        {
            // 보스만 소환, 일반 몬스터 0
            targetSpawnCountThisWave = 0;
            SpawnBossForWave(wave);
        }
        else
        {
            // 일반 몬스터 스폰 수: (wave%10)*10  (예: 14웨이브 -> 40)
            targetSpawnCountThisWave = (wave % 10) * 10;
            // 안전장치(이 값이 0이 되는 건 10의 배수 웨이브인데 이미 위에서 보스 웨이브로 처리됨)
            targetSpawnCountThisWave = Mathf.Max(0, targetSpawnCountThisWave);

            // 간격은 10초/목표수. 0 나눗셈 방지
            spawnInterval = (targetSpawnCountThisWave > 0) ? (waveDuration / targetSpawnCountThisWave) : 0f;

            // 시작 직후 첫 마리를 바로 뽑지 않고 균등 분배 로직으로만 관리하므로 여기서는 아무것도 안 함
        }

        Debug.Log($"[Wave] Start Wave {wave} | BossWave={isBossWave} | target={targetSpawnCountThisWave}");
    }

    void NextWave()
    {
        currentWave++;
        BeginWave(currentWave);
    }

    void SpawnNormal()
    {
        if (!monsterPrefab) return;

        Vector3 pos = new Vector3(Random.Range(spawnXRange.x, spawnXRange.y), spawnY, 0f);
        GameObject m = Instantiate(monsterPrefab, pos, Quaternion.identity);

        // 체력 스케일 적용
        int hp = GetScaledHealthForWave(currentWave);
        ApplyHealth(m, hp);
    }

    void SpawnBossForWave(int wave)
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogWarning("[Wave] Boss list is empty. Skipping boss spawn.");
            return;
        }

        // 10웨이브 -> index 0, 20웨이브 -> index 1, ...
        int tierIndex = (wave / 10) - 1;
        if (tierIndex < 0) tierIndex = 0;

        // 보스 배열 길이를 넘어가면 순환(mod) 사용
        int bossIndex = tierIndex % bossPrefabs.Length;

        Vector3 pos = new Vector3(Random.Range(spawnXRange.x, spawnXRange.y), spawnY, 0f);
        aliveBoss = Instantiate(bossPrefabs[bossIndex], pos, Quaternion.identity);

        // 보스도 동일한 체력 규칙(원한다면 별도 배율을 두어도 됨)
        int hp = GetScaledHealthForWave(wave);
        ApplyHealth(aliveBoss, hp);

        Debug.Log($"[Wave] Boss Spawned for Wave {wave} (index={bossIndex}) with HP {hp}");
    }

    int GetScaledHealthForWave(int wave)
    {
        // (wave-1)/10 의 내림 값이 티어 인덱스(0:1~10, 1:11~20, 2:21~30...)
        int tier = Mathf.FloorToInt((wave - 1) / 10f);
        // 10 * 2^tier
        int hp = baseHealthPerTier * (1 << tier);
        return hp;
    }

    void ApplyHealth(GameObject obj, int hp)
    {
        if (!obj) return;

        // 사용자가 쓰는 Monster_Base에 맞춰 적용
        var monsterBase = obj.GetComponent<Monster_Base>();
        if (monsterBase != null)
        {
            monsterBase.maxHealth = hp;
        }

    }
}
