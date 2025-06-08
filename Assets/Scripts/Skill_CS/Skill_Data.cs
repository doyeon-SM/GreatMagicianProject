using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skill/Skill Data")]
public class Skill_Data : ScriptableObject
{
    public string skillName;  // 스킬 이름
    public int damage;  // 스킬의 기본 공격력
    public int Tier;
    public int level = 1;  // 기본 레벨 0
    public int NeedLevelUP_Gold = 1;
    public Sprite skillIcon;  // 스킬 아이콘
    public GameObject rangePrefab;  // 스킬 범위 (Range) 프리팹
    public GameObject useableRangePrefab;  // 스킬 사거리 (Useable_Range) 프리팹
    public GameObject attackPrefab;  // 투사체 프리팹

    public SkillType skillType;  // 스킬 타입을 설정
    public SkillEffect skillEffect; //스킬 효과 설정
    public T1_SkillDamage_Data t1SkillDamageData;  // 티어 1 스킬 데미지 데이터 참조
    public T2_SkillDamage_Data t2SkillDamageData;   // 티어 2 스킬 데미지 데이터 참조

    // 추가적인 효과나 특수한 능력을 위한 필드 (예: 상태 효과, 범위 크기 등)
    public bool isCombinationSkill;  // 조합 스킬 여부
    public List<Skill_Data> requiredBaseSkills;  // 이 스킬을 얻기 위해 필요한 기본 스킬들

    public float Effect_Value; //효과 적용량(넉백량, 지속시간, 등)
    public float AreaTime = 0f;

    public string skillscript;
    public bool isKnow = false;

    public enum SkillType
    {   
        Projectile,      // 투사체 타입 
        Chain,           // 체인(Projectile 확장)
        AreaOfEffect,    // 범위 공격 타입
        Area,            // 영역 효과를 포함한 타입 (AreaOfEffect 확장)
        StraightLine,    // 직선 영역 효과(AreaOfEffect 확장)
        Create,          // 설치형 타입 
        Scattered,       // 산탄형 투사체
        Summon,          // 소환수
        Around           // 회전체
    }
    public enum SkillEffect
    {
        None,               //무효과
        Rolling,            //굴리기
        Slow,               //슬로우: 몬스터 이동속도 -70%
        Explosion,          //폭발 효과(투사체)
        Paralysis,          //마비: 50%확률 몬스터 이동속도 -90%
        Knockback,          // 넉백 효과를 포함한 타입 (AreaOfEffect 확장)
        Freezing,           //얼음: 50%확률 몬스터 이동속도 -90%
        Burn,               //화상: 스킬의 10% 데미지 지속시간동안 도트뎀
        Bind,               //속박: 몬스터 이동속도 0
        Fear,               //공포: 뒤로 몬스터 강제 이동
        Gravity,            //중력: 중심으로 끌어당김
        Posion,             //독
        Blind               //실명
    }
    private void OnEnable()
    {
        UpdateDamage();
    }
    public void UpdateDamage()
    {
        if (isCombinationSkill && Tier == 1 && t1SkillDamageData != null)
        {
            damage = t1SkillDamageData.CalculateT1SkillDamage(skillName, requiredBaseSkills);
        }
        else if(isCombinationSkill && Tier == 2 && t2SkillDamageData != null)
        {
            damage = t2SkillDamageData.CalculateT2SkillDamage(skillName, requiredBaseSkills);
        }
    }

    public void ApplyDamage(Monster_Base monster)
    {
        int calculatedDamage = damage;  // 기본적으로 스킬의 데미지 사용

        /*if (isCombinationSkill && Tier == 1 && t1SkillDamageData != null)
        {
            calculatedDamage = t1SkillDamageData.CalculateT1SkillDamage(skillName, requiredBaseSkills);            
        }*/
        Debug.Log("적용된 데미지: "+calculatedDamage);
        monster.TakeDamage(calculatedDamage);
    }

    public void ApplyKnockback(Monster_Base monster, Vector3 origin)
    {
        if (monster != null)
        {
            // 넉백 방향 계산 (몬스터가 origin에서 떨어지는 방향)
            Vector3 direction = (monster.transform.position - origin).normalized;
            // knockbackVelocity를 넉백량에 따라 결정합니다.
            Vector3 knockbackVelocity = direction * Effect_Value;
            // 예시: 넉백 지속 시간 0.5초 (필요에 따라 조절)
            float knockbackDuration = 0.5f;
            monster.ApplyMoveEffect(knockbackVelocity, knockbackDuration);
        }
    }
    public void ApplyFear(Monster_Base monster)
    {
        if (monster == null) return;
        Vector3 direction = Vector3.up;
        float Duration = Effect_Value;
        monster.ApplyMoveEffect(direction, Duration);
    }

    public void ExecuteSkill(Monster_Base monster, Vector3 origin)
    {
        ApplyDamage(monster);

        if (skillEffect == SkillEffect.Knockback)
        {
            ApplyKnockback(monster, origin);
        }
        else if(skillEffect == SkillEffect.Freezing)
        {
            ApplyFreezing(monster);
        }
        else if(skillEffect == SkillEffect.Paralysis)
        {
            ApplyParalysis(monster);
        }
        else if(skillEffect == SkillEffect.Burn)
        {
            ApplyBurn(monster);
        }
        else if(skillEffect == SkillEffect.Fear)
        {
            ApplyFear(monster);
        }
        else if(skillEffect == SkillEffect.Gravity)
        {
            ApplyGravity(monster, origin);
        }
        else if(skillEffect == SkillEffect.Posion)
        {
            ApplyPosion(monster);
        }
    }

    public int ApplyCreate()
    {
        int Createint = damage;
        /*if (isCombinationSkill && Tier == 1 && t1SkillDamageData != null)
        {
            Createint = t1SkillDamageData.CalculateT1SkillDamage(skillName, requiredBaseSkills);
        }*/
        Debug.Log("Create값이 설정되었습니다: " + Createint);
        return Createint;
    }

    public int ApplySummonHP()
    {
        int SummonHP = damage / 2;
        return SummonHP;
    }
    public int ApplySummonAD()
    {
        int SummonAD = damage;
        return SummonAD;
    }

    public void ApplyFreezing(Monster_Base monster)
    {
        if(monster != null)
        {
            float v = (Random.value < 0.5f) ? 0.1f : 1.0f;
            monster.ApplyElement("Aqua");
            monster.ApplySlowEffect(v);
        }
    }

    public void ApplyParalysis(Monster_Base monster)
    {
        if (monster == null) return;

        float v = (Random.value < 0.5f) ? 0.1f : 1.0f;
        monster.ApplyElement("Ignis");
        monster.ApplySlowEffect(v);
    }

    public void ApplyBurn(Monster_Base monster)
    {
        if (monster == null) return;
        int i = damage / 10;
        monster.ApplyContinuousDamage(Effect_Value, i);
        monster.ApplyElement("Ignis");
    }
    public void ApplyPosion(Monster_Base monster)
    {
        if (monster == null) return;
        int i = damage / 10;
        monster.ApplyContinuousDamage(Effect_Value, i);
        monster.ApplyElement("Terra");
    }

    public void ApplyGravity(Monster_Base monster, Vector3 origin)
    {
        if (monster == null) return;

        Vector3 direction = (origin - monster.transform.position).normalized;
        Vector3 GravityVelocity = direction * Effect_Value;
        float GravityDuration = 0.2f;

        monster.ApplyMoveEffect(GravityVelocity, GravityDuration);
    }
}



