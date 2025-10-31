using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class Skill_Range_System : MonoBehaviour
{
    public Skill_Data skillData;  // 현재 스킬에 대한 SkillData 참조
    private List<Monster_Base> monstersInRange = new List<Monster_Base>();  // 범위 내의 몬스터 리스트
    private List<Wall_System_Create> WCreateInRange = new List<Wall_System_Create>();//범위 내의 벽 리스트
    private List<Summon_System> SCreateInRange = new List<Summon_System>(); //범위 내의 소환수 리스트
    private Coroutine periodicDamageCoroutine;

    public GameObject createPrefab; //Create 타입 프리팹

    [Header("SFX (SkillSFX 그룹)")]
    public AudioClip skillSFX_Rangetick;                // 틱마다 재생할 사운드
    public AudioClip skillSFX_Use;                      // 스킬 사용시 사운드
    public AudioMixerGroup skillSFXGroup;               // SkillSFX 오디오 믹서 그룹
    [Range(0f, 1f)] public float sfxVolume = 1f;        // 개별 볼륨(프로젝트 전체는 믹서에서 제어)
    private AudioSource _sfxSource;                     // 내부용 오디오 소스

    private void Start()
    {
        if (skillSFXGroup == null && SoundSettingsManager.Instance != null)
            skillSFXGroup = SoundSettingsManager.Instance.GetSkillSFXGroup();

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;      // 2D
            _sfxSource.ignoreListenerPause = false; // 일시정지 중 재생 원치 않으면 false(원하면 true)
            _sfxSource.outputAudioMixerGroup = skillSFXGroup;
            _sfxSource.volume = sfxVolume;
        }

        // 볼륨 시스템이 재적용될 때 라우팅 보정
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied += RebindSkillGroup;
    }
    private void Awake()
    {
        // 그룹 자동 할당
        if (skillSFXGroup == null && SoundSettingsManager.Instance != null)
            skillSFXGroup = SoundSettingsManager.Instance.GetSkillSFXGroup();

        // 오디오소스 생성
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;            // 2D
            _sfxSource.ignoreListenerPause = false;  // 필요 시 true
            _sfxSource.outputAudioMixerGroup = skillSFXGroup;
            _sfxSource.volume = 1f;                  // ← 이중 감쇠 방지 (PlayOneShot에서만 스케일)
        }
    }
    private void OnEnable()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied += RebindSkillGroup;
    }

    private void OnDisable()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied -= RebindSkillGroup;
    }
    private void EnsureSfxSource()
    {
        if (_sfxSource != null) return;

        // 그룹 확보
        if (skillSFXGroup == null && SoundSettingsManager.Instance != null)
            skillSFXGroup = SoundSettingsManager.Instance.GetSkillSFXGroup();

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
        _sfxSource.ignoreListenerPause = false;
        _sfxSource.outputAudioMixerGroup = skillSFXGroup;
        _sfxSource.volume = 1f; // 이중 감쇠 방지
    }

    private void RebindSkillGroup()
    {
        if (_sfxSource == null) return;
        var group = SoundSettingsManager.Instance?.GetSkillSFXGroup();
        if (group != null) SoundSettingsManager.Instance.ConfigureSourceToGroup(_sfxSource, group, is2D: true);
    }

    public void StartRangeEffect()
    {
        if (skillData != null && skillData.skillEffect == Skill_Data.SkillEffect.Explosion)
        {
            periodicDamageCoroutine = StartCoroutine(ApplyPeriodicDamage(0.2f, 0.1f));
        }
        else if (skillData != null && skillData.skillType == Skill_Data.SkillType.Area)
        {
            periodicDamageCoroutine = StartCoroutine(ApplyPeriodicDamage(skillData.AreaTime, 0.5f));
        }
        else
        {
            Debug.LogError("Invalid skill data or skill type for range effect.");
        }
    }

    private IEnumerator ApplyPeriodicDamage(float duration, float interval)
    {
        
        float effectvalue = 1f;
        bool tmpblind = false;
        string monster_Element_String = "None";

        if (skillData == null) yield break;
        
        switch (skillData.skillEffect.ToString())
        {
            case "Slow":
                effectvalue = 0.3f;
                break;
            case "Bind":
                effectvalue = 0f;
                break;
            case "Blind":
                tmpblind = true;
                break;
            default:
                effectvalue = 1.0f;
                break;
        }


        int totalTicks = Mathf.Max(1, Mathf.CeilToInt(duration / Mathf.Max(0.0001f, interval)));

        for (int tick = 0; tick < totalTicks; tick++)
        {
            if (this == null || gameObject == null) yield break;

            // 범위 내 몬스터에 지속효과 적용(슬로우/블라인드 등)
            foreach (Monster_Base monster in monstersInRange)
            {
                if (monster != null)
                {
                    if (monster_Element_String != "None")
                        monster.ApplyElement(monster_Element_String);

                    monster.ApplySlowEffect(effectvalue);
                    monster.ApplyBlindEffect(tmpblind);
                }
            }

            // 즉시 타격/회복 실행
            if (skillData.skillEffect == Skill_Data.SkillEffect.Heal)
                ApplyHealingInRange();
            else
                ApplyDamageInRange();


            // 마지막 틱이 아니면 interval만큼 대기
            if (tick < totalTicks - 1)
                yield return new WaitForSeconds(interval);
        }

        foreach (Monster_Base monster in monstersInRange)
        {
            if (monster != null)
            {
                monster.RemoveSlowEffect();
                monster.RemoveBlindEffect();
            }
        }
        
        // 오브젝트가 파괴되지 않았을 때만 파괴 시도
        if (gameObject != null)
        {
            Debug.Log("Destroying range object after periodic damage");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Debug.Log("Skill_Range_System destroyed: " + gameObject.name);

        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied -= RebindSkillGroup;

        if (periodicDamageCoroutine != null)
            StopCoroutine(periodicDamageCoroutine);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            Monster_Base monster = collision.GetComponent<Monster_Base>();
            if (monster != null && !monstersInRange.Contains(monster))
            {
                monstersInRange.Add(monster);                
            }
        }
        else if(collision.CompareTag("Create"))
        {
            Wall_System_Create WCreate = collision.GetComponent<Wall_System_Create>();
            Summon_System SCreate = collision.GetComponent<Summon_System>();
            if(WCreate != null && !WCreateInRange.Contains(WCreate))
            {
                WCreateInRange.Add(WCreate);
            }
            if(SCreate != null && !SCreateInRange.Contains(SCreate))
            {
                SCreateInRange.Add(SCreate);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Monster"))
        {
            Monster_Base monster = collision.GetComponent<Monster_Base>();
            if (monster != null && monstersInRange.Contains(monster))
            {
                monstersInRange.Remove(monster);
                
            }
        }
        else if (collision.CompareTag("Create"))
        {
            Wall_System_Create WCreate = collision.GetComponent<Wall_System_Create>();
            Summon_System SCreate = collision.GetComponent<Summon_System>();
            if (WCreate != null && !WCreateInRange.Contains(WCreate))
            {
                WCreateInRange.Remove(WCreate);
            }
            if (SCreate != null && !SCreateInRange.Contains(SCreate))
            {
                SCreateInRange.Remove(SCreate);
            }
        }
    }

    public void ApplyDamageInRange()
    {
        // monstersInRange 리스트의 복사본을 사용하여 반복을 수행합니다.
        List<Monster_Base> monstersToDamage = new List<Monster_Base>(monstersInRange);

        foreach (Monster_Base monster in monstersToDamage)
        {
            if (monster != null)
            {
                Debug.Log("Attack: " + monster);
                skillData.ApplyDamage(monster);

                if (monster.MonsterIsDead) // 몬스터가 죽었는지 확인
                {
                    monstersInRange.Remove(monster); // 원본 리스트에서 몬스터를 제거
                }
            }
        }

        // 틱마다 스킬 SFX 재생
        if (skillSFX_Rangetick != null)
        {
            EnsureSfxSource();
            if (_sfxSource.outputAudioMixerGroup == null && SoundSettingsManager.Instance != null)
                SoundSettingsManager.Instance.ConfigureSourceToGroup(_sfxSource, SoundSettingsManager.Instance.GetSkillSFXGroup(), is2D: true);

            _sfxSource.PlayOneShot(skillSFX_Rangetick, Mathf.Clamp01(sfxVolume));
        }
    }



    private void ApplyDamageToMonsters()
    {
        List<Monster_Base> monstersToDamage = new List<Monster_Base>(monstersInRange);

        foreach (Monster_Base monster in monstersToDamage)
        {
            if (monster != null)
            {
                skillData.ExecuteSkill(monster, transform.position);
            }
        }

        List<Monster_Base> monstersToRemove = new List<Monster_Base>();

        foreach (Monster_Base monster in monstersToDamage)
        {
            if (monster != null && monster.MonsterIsDead)
            {
                monstersToRemove.Add(monster);
            }
        }

        foreach (Monster_Base monster in monstersToRemove)
        {
            monstersInRange.Remove(monster);
        }
    }
    public void ApplySkillEffect(Vector3 mousePosition)
    {
        if (skillData == null)
        {
            Debug.LogError("SkillData is not assigned.");
            return;
        }
        // 스킬 사용시 SFX 재생
        if (skillSFX_Use != null)
            PlayUseSfxAtPoint(skillSFX_Use, transform.position);

        switch (skillData.skillType.ToString())
        {
            case "Projectile":
                if (skillData.skillEffect == Skill_Data.SkillEffect.Explosion)
                {
                    LaunchProjectile(mousePosition);
                    break;                    
                }
                else if(skillData.skillEffect == Skill_Data.SkillEffect.Rolling)
                {
                    AimAndApplyProjectile_Rolling(mousePosition);
                    break;
                }
                else
                {
                    AimAndApplyProjectile(mousePosition);
                    break;
                }
            case "Chain":
                AimAndApplyProjectile(mousePosition);
                break;

            case "Area":
                CreateArea(mousePosition);
                break;

            case "Create":
            case "Summon":
                CreateSkillPrefab(mousePosition);
                break;

            case "Scattered":
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, 0f);
                    AimAndApplyProjectile(mousePosition + randomOffset);
                }
                break;

            case "Around":
                CreateAroundSkill();
                break;

            case "StraightLine":   // 직선형은 캐스트 위치로 다시 조준 후 타격
                AimAndApplyStraightLine(mousePosition);
                break;

            default:               // AreaOfEffect 등
                ApplyDamageToMonsters();
                break;
        }
    }

    private void LaunchProjectile(Vector3 mousePosition)
    {
        if (skillData != null && skillData.attackPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(0, -8, 0); 

            if (skillData.skillEffect.ToString() == "Rolling")
            {
                spawnPosition = new Vector3(mousePosition.x, -8, 0);
            }

            GameObject projectile = Instantiate(skillData.attackPrefab, spawnPosition, Quaternion.identity);
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                Vector2 direction = (mousePosition - spawnPosition).normalized;
                if (skillData.skillEffect.ToString() == "Rolling")
                {
                    direction = Vector2.up;
                }
                rb.velocity = direction * 10f;

                Skill_System_Projectile projectileScript = projectile.GetComponent<Skill_System_Projectile>();
                if (projectileScript != null)
                {
                    projectileScript.Initialize(skillData, mousePosition);

                    // Fire 관련 코드 제외, 다른 스킬에 대한 추가 처리가 필요하면 여기서 추가 가능
                }
                else
                {
                    Debug.LogError("Skill_System_Projectile component is missing on the attackPrefab.");
                }

                Destroy(projectile, 5f); // 투사체가 5초 후에 제거되도록 설정
            }
            else
            {
                Debug.LogError("Rigidbody2D component is missing on the attackPrefab.");
            }
        }
        else
        {
            Debug.LogError("Attack prefab is not assigned in SkillData.");
        }
    }

    private void CreateArea(Vector3 mousePosition)
    {
        if (skillData != null && skillData.rangePrefab != null)
        {
            GameObject rangeObject = Instantiate(skillData.rangePrefab, mousePosition, Quaternion.identity);
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
    private void CreateSkillPrefab(Vector3 position)
    {
        if (createPrefab == null) return;

        if (skillData.skillType == Skill_Data.SkillType.Create) 
        {
            Instantiate(createPrefab, position, Quaternion.identity);
        }
        else if (skillData.skillType == Skill_Data.SkillType.Summon)
        {
            Instantiate(createPrefab, position, Quaternion.identity);
        }        
    }
    private void CreateAroundSkill()
    {
        Vector3 centerPosition = new Vector3(0f, -8f, 0f);
        GameObject centerObject = new GameObject("AroundCenter");
        centerObject.transform.position = centerPosition;
        centerObject.tag = "AroundCenter";

        int numberOfOrbitProjectiles = 3;
        float orbitRadius = 7.0f;
        float orbitSpeed = 180.0f;
        float duration = skillData.AreaTime;
        Debug.Log("Around Center 생성 완료");

        for (int i = 0; i < numberOfOrbitProjectiles; i++)
        {
            float angleOffset = (360f / numberOfOrbitProjectiles) * i;
            float rad = Mathf.Deg2Rad * angleOffset;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
            Vector3 spawnPos = centerPosition + offset;

            GameObject orbitProjectile = Instantiate(skillData.attackPrefab, spawnPos, Quaternion.identity);
            orbitProjectile.tag = "AroundProjectile";

            var orbitScript = orbitProjectile.AddComponent<Around_OrbitingProjectile>();
            orbitScript.skillData = skillData;
            orbitScript.center = centerObject.transform;
            orbitScript.radius = orbitRadius;
            orbitScript.angularSpeed = orbitSpeed;
            orbitScript.startAngle = angleOffset; // 각도 분배로 위상 차이 부여
            
            // 투사체도 지속시간 후 제거
            Destroy(orbitProjectile, duration);
        }

        // 중심 오브젝트도 일정 시간 뒤 제거
        Destroy(centerObject, duration);
    }

    //Heal 기능
    private void ApplyHealingInRange()
    {
        List<Wall_System_Create> WCreatetoHeal = new List<Wall_System_Create>(WCreateInRange);
        List<Summon_System> SCreatetoHeal = new List<Summon_System>(SCreateInRange);

        foreach(Wall_System_Create WCreate in WCreatetoHeal)
        {
            if(WCreate != null)
            {
                Debug.Log("Heal:" + WCreate);
                WCreate.Heal_Create(skillData.damage);
            }
        }
        foreach(Summon_System SCreate in SCreatetoHeal)
        {
            if(SCreate != null)
            {
                Debug.Log("Heal:" + SCreate);
                SCreate.Heal_Summon(skillData.damage);
            }
        }

        // 틱마다 스킬 SFX 재생
        if (skillSFX_Rangetick != null)
        {
            EnsureSfxSource();
            if (_sfxSource.outputAudioMixerGroup == null && SoundSettingsManager.Instance != null)
                SoundSettingsManager.Instance.ConfigureSourceToGroup(_sfxSource, SoundSettingsManager.Instance.GetSkillSFXGroup(), is2D: true);

            _sfxSource.PlayOneShot(skillSFX_Rangetick, Mathf.Clamp01(sfxVolume));
        }
    }
    private void AimAndApplyStraightLine(Vector3 castPos)
    {
        // 드래그 시스템과 동일한 ‘아랫변 중앙’ 기준점
        Vector3 bottomCenter = new Vector3(0f, -8f, 0f);

        // 이 Range 오브젝트(직사각형 프리팹)가 바로 회전/적용의 주체
        transform.position = bottomCenter;

        Vector3 dir = castPos - bottomCenter;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.right; // 방어적: 0벡터면 오른쪽으로
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 현재 콜라이더에 겹쳐진 대상들에게 적용
        ApplyDamageToMonsters();
    }

    private void AimAndApplyProjectile(Vector3 castPos)
    {
        Vector3 bottomCenter = new Vector3(0f, -8f, 0f);
        transform.position = bottomCenter;

        Vector3 dir = castPos - bottomCenter;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        LaunchProjectile(castPos);
    }

    private void AimAndApplyProjectile_Rolling(Vector3 castPos)
    {
        Vector3 bottomCenter = new Vector3(castPos.x, -8f, 0f);
        transform.position = bottomCenter;

        Vector3 dir = castPos - bottomCenter;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;

        transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        LaunchProjectile(castPos);
    }

    private void PlayUseSfxAtPoint(AudioClip clip, Vector3 worldPos)
    {
        if (clip == null) return;

        // 임시 오브젝트 생성
        var go = new GameObject("SkillSFX_Use_Temp");
        go.transform.position = worldPos;

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;              // 2D 재생 (3D 원하면 1f로)
        src.ignoreListenerPause = false;    // 일시정지 중에도 들리게 하려면 true
        src.loop = false;

        // Mixer 그룹 라우팅 (SkillSFX)
        var group = skillSFXGroup != null
            ? skillSFXGroup
            : SoundSettingsManager.Instance?.GetSkillSFXGroup();
        if (group != null) src.outputAudioMixerGroup = group;

        // 볼륨 (이중 감쇠 피하려면 AudioSource.volume만 사용)
        src.volume = Mathf.Clamp01(sfxVolume);

        src.clip = clip;
        src.Play();

        Destroy(go, clip.length + 0.05f);
    }
}
