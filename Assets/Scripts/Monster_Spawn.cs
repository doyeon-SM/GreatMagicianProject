using System.Collections.Generic;
using UnityEngine;

public class Monster_Spawn : MonoBehaviour
{
    [System.Serializable]
    public struct MonsterVariant
    {
        [Tooltip("소환할 몬스터 프리팹")]
        public GameObject prefab;

        [Tooltip("가중치(확률). 값이 클수록 잘 뽑힘")]
        public int weight;

        [Tooltip("이 몬스터가 등장 가능한 최소/최대 웨이브 (포함)")]
        public int minWave;
        public int maxWave;

        [Header("옵션")]
        [Tooltip("체력 추가 배율 (최종 HP *= 이 값) — 1이면 기본")]
        public float hpMultiplier;
    }

    [Header("Monster Pool (일반 웨이브용)")]
    [Tooltip("여러 몬스터 변종을 등록하세요")]
    public List<MonsterVariant> normalMonsterPool = new List<MonsterVariant>();

    [Header("Boss Monsters (10,20,30... 웨이브 순서)")]
    public GameObject[] bossPrefabs;

    [Header("Wave Settings")]
    [Tooltip("웨이브 하나의 진행 시간(초) - 요구: 10초")]
    public float waveDuration = 10f;
    [Tooltip("시작 웨이브(보통 1)")]
    public int currentWave = 1;

    [Header("Spawn Area")]
    [Tooltip("일반/보스 스폰 X좌표 최소~최대, Y는 고정")]
    public Vector2 spawnXRange = new Vector2(-5f, 5f);
    public float spawnY = 8f;

    [Header("HP Scaling")]
    [Tooltip("1~10웨: 10, 11~20웨: 20, 21~30웨: 40 ... (10 * 2^티어)")]
    public int baseHealthPerTier = 10;

    [Header("Debug (읽기전용)")]
    [SerializeField] private int targetSpawnCountThisWave; // 이번 웨이브 목표 스폰 수(일반)
    [SerializeField] private int spawnedThisWave;          // 이번 웨이브 실제 스폰 수
    [SerializeField] private float waveTimer;              // 웨이브 경과 시간
    [SerializeField] private bool isBossWave;              // 보스 웨이브 여부
    [SerializeField] private GameObject aliveBoss;         // 현재 살아있는 보스 인스턴스

    void Start()
    {
        BeginWave(currentWave);
    }

    void Update()
    {
        if (isBossWave)
        {
            // 보스가 처치되어야만 다음 웨이브
            if (aliveBoss == null)
            {
                NextWave();
            }
            return;
        }

        // 일반 웨이브 진행
        waveTimer += Time.deltaTime;

        // 10초 내에 목표 수만큼 균등 분배 소환
        if (targetSpawnCountThisWave > 0 && spawnedThisWave < targetSpawnCountThisWave)
        {
            float expectedSpawnCount = Mathf.Floor((waveTimer / waveDuration) * targetSpawnCountThisWave);
            while (spawnedThisWave < expectedSpawnCount)
            {
                SpawnNormalRandom(); // 랜덤 변종 소환
                spawnedThisWave++;
            }
        }

        if (waveTimer >= waveDuration)
        {
            NextWave();
        }
    }

    void BeginWave(int wave)
    {
        waveTimer = 0f;
        spawnedThisWave = 0;
        aliveBoss = null;

        // 보스 웨이브 판단(10, 20, 30...)
        isBossWave = (wave % 10 == 0);

        if (isBossWave)
        {
            targetSpawnCountThisWave = 0;
            SpawnBossForWave(wave);
        }
        else
        {
            // (wave % 10) * 10
            targetSpawnCountThisWave = Mathf.Max(0, (wave % 10) * 5);
        }

        Debug.Log($"[Wave] Start Wave {wave} | BossWave={isBossWave} | target={targetSpawnCountThisWave}");
    }

    void NextWave()
    {
        currentWave++;
        BeginWave(currentWave);
    }

    // ====== 랜덤 변종 소환 ======
    void SpawnNormalRandom()
    {
        var eligible = GetEligibleVariants(currentWave);
        if (eligible.Count == 0)
        {
            Debug.LogWarning("[Wave] Eligible normal monster not found. Skip spawn.");
            return;
        }

        // 가중치 랜덤 선택
        int pickIndex = WeightedPickIndex(eligible);
        var variant = eligible[pickIndex];

        Vector3 pos = new Vector3(Random.Range(spawnXRange.x, spawnXRange.y), spawnY, 0f);
        GameObject m = Instantiate(variant.prefab, pos, Quaternion.identity);

        // 체력 스케일 적용
        int hp = GetScaledHealthForWave(currentWave);
        hp = Mathf.RoundToInt(hp * Mathf.Max(variant.hpMultiplier, 0.01f)); // 0 보호

        ApplyHealth(m, hp);

        // 스폰 컨텍스트(옵션): 필요하면 수신 측에서 OnSpawned(MonsterSpawnContext) 구현
        var ctx = new MonsterSpawnContext(currentWave, hp, isBoss: false);
        m.SendMessage("OnSpawned", ctx, SendMessageOptions.DontRequireReceiver);
    }

    // 현재 웨이브에 등장 가능한 변종 필터링
    List<MonsterVariant> GetEligibleVariants(int wave)
    {
        var list = new List<MonsterVariant>();
        foreach (var v in normalMonsterPool)
        {
            if (v.prefab == null) continue;
            if (wave < Mathf.Max(1, v.minWave)) continue;
            if (v.maxWave > 0 && wave > v.maxWave) continue; // maxWave=0이면 무제한
            list.Add(v);
        }
        return list;
    }

    int WeightedPickIndex(List<MonsterVariant> variants)
    {
        int total = 0;
        for (int i = 0; i < variants.Count; i++)
            total += Mathf.Max(0, variants[i].weight);

        if (total <= 0) return 0;

        int r = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            acc += Mathf.Max(0, variants[i].weight);
            if (r < acc) return i;
        }
        return variants.Count - 1;
    }

    // ====== 보스 소환 ======
    void SpawnBossForWave(int wave)
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
        {
            Debug.LogWarning("[Wave] Boss list is empty. Skipping boss spawn.");
            return;
        }

        int tierIndex = (wave / 10) - 1;
        if (tierIndex < 0) tierIndex = 0;
        int bossIndex = tierIndex % bossPrefabs.Length;

        Vector3 pos = new Vector3(Random.Range(spawnXRange.x, spawnXRange.y), spawnY, 0f);
        aliveBoss = Instantiate(bossPrefabs[bossIndex], pos, Quaternion.identity);

        int hp = GetScaledHealthForWave(wave);
        ApplyHealth(aliveBoss, hp);

        var ctx = new MonsterSpawnContext(wave, hp, isBoss: true);
        aliveBoss.SendMessage("OnSpawned", ctx, SendMessageOptions.DontRequireReceiver);

        Debug.Log($"[Wave] Boss Spawned for Wave {wave} (index={bossIndex}) with HP {hp}");
    }

    // ====== HP 적용 ======
    int GetScaledHealthForWave(int wave)
    {
        // (wave-1)/10 내림 → 0:1~10, 1:11~20, 2:21~30...
        int tier = Mathf.FloorToInt((wave - 1) / 10f);
        return baseHealthPerTier * (1 << tier); // 10 * 2^tier
    }

    void ApplyHealth(GameObject obj, int hp)
    {
        if (!obj) return;
        var monsterBase = obj.GetComponent<Monster_Base>();
        if (monsterBase != null)
        {
            monsterBase.maxHealth = hp;
            // 필요하면 InitializeHealth(hp) 같은 초기화 메서드를 만들어 호출 권장
        }

        // 다른 스크립트 형태라면 여기에 맞춰 세터 호출 추가
        // var enemy = obj.GetComponent<Enemy>();
        // if (enemy != null) enemy.SetMaxAndCurrentHP(hp);
    }

    // ====== 스폰 컨텍스트(옵션) ======
    public struct MonsterSpawnContext
    {
        public int wave;
        public int hp;
        public bool isBoss;

        public MonsterSpawnContext(int wave, int hp, bool isBoss)
        {
            this.wave = wave;
            this.hp = hp;
            this.isBoss = isBoss;
        }
    }
}
