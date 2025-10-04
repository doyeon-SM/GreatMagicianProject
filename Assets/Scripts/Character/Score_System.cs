using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score_System : MonoBehaviour
{
    public Character character;
    public int score = 0;

    private bool _resultApplied = false;
    private int T0Len => character?.tier0Skills?.Length ?? 0;
    private int T1Len => character?.tier1Skills?.Length ?? 0;
    private int T2Len => character?.tier2Skills?.Length ?? 0;

    // 결과창에서 사용할 "이번 결과에서 획득한 스킬" 기록
    [System.Serializable]
    public class AwardedSkillInfo
    {
        public enum AwardSource { Random, Guaranteed }

        public Skill_Data skill;
        public int skillIndex;
        public int tier;
        public bool isNew;
        public AwardSource source;
    }
    public List<AwardedSkillInfo> LastAwarded = new List<AwardedSkillInfo>();

    // 스테이지마다 결과 적재 플래그 리셋
    public void BeginStageRun()
    {
        _resultApplied = false;
        LastAwarded.Clear();
    }
    private void OnEnable()
    {
        _resultApplied = false;
    }

    public void ResultScore()
    {
        if (_resultApplied) return;   // 두 번 이상 호출 방지
        _resultApplied = true;
        if (score > 0)
        {
            character.Character_Gold += score;
            character.CharacterLevelUP(score / 100);

            AwardRandomSkills(score);
            // AwardGuaranteedSkills(); // 확정 보상 로직이 분리돼 있다면 여기서 함께
        }
    }
    /// <summary>
    /// 점수 기반 랜덤 스킬 지급
    /// score 100당 1개. 티어 확률은 점수 구간에 따라 변동.
    /// </summary>
    /// <param name="totalScore">최종 점수</param>
    private void AwardRandomSkills(int totalScore)
    {
        int count = totalScore / 100; // 100점당 1개
        if (count <= 0 || character == null)
            return;

        var t0 = character.tier0Skills;
        var t1 = character.tier1Skills;
        var t2 = character.tier2Skills;

        //LastAwarded.Clear(); // 이번 결과 기록 초기화

        for (int i = 0; i < count; i++)
        {
            // 점수 구간에 따른 확률
            (int w0, int w1, int w2) = GetTierWeights(totalScore);

            // 티어 선택
            int tier = PickTierByWeight(w0, w1, w2);

            // 선택 티어에서 스킬 하나 랜덤 픽 (없으면 폴백 티어 순회)
            Skill_Data picked = PickRandomSkillWithFallback(tier, t0, t1, t2);
            if (picked == null)
                continue; // 정말 아무 것도 없으면 스킵

            // 신규 습득 여부 판단
            bool wasKnown = picked.isKnow;
            if (!wasKnown) picked.isKnow = true; // 이번에 배움

            // 인덱스/티어 결정
            int skillIndex = ResolveSkillIndex(picked);
            int resolvedTier = ResolveTier(picked, t0, t1, t2);

            if (skillIndex >= 0)
            {
                EnsureHaveSkillCapacity(skillIndex);
                character.Character_HaveSkill[skillIndex] += 1;

                // 결과기록 추가
                LastAwarded.Add(new AwardedSkillInfo
                {
                    skill = picked,
                    skillIndex = skillIndex,
                    tier = resolvedTier,
                    isNew = !wasKnown,
                    source = AwardedSkillInfo.AwardSource.Random
                });
            }
        }
    }

    /// <summary>
    /// 점수에 따른 티어 가중치
    /// 1티어: 1만점, 2티어: 5만점
    /// </summary>
    private (int w0, int w1, int w2) GetTierWeights(int sc)
    {
        if (sc <= 10000) return (100, 0, 0);   // 0티어 100%
        else if (sc <= 50000) return (70, 30, 0);   // 0티어 70%, 1티어 30%
        else return (40, 35, 25);  // 0티어 40%, 1티어 35%, 2티어 25%
    }

    /// <summary>
    /// 가중치로 티어 선택 (0/1/2)
    /// </summary>
    private int PickTierByWeight(int w0, int w1, int w2)
    {
        int total = w0 + w1 + w2;
        int r = Random.Range(0, total);
        if (r < w0) return 0;
        r -= w0;
        if (r < w1) return 1;
        return 2;
    }

    /// <summary>
    /// 선택 티어에서 랜덤 스킬을 시도하고, 비어있으면 인접 티어로 폴백
    /// </summary>
    private Skill_Data PickRandomSkillWithFallback(int preferredTier, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        int[][] order =
        {
            new int[]{0,1,2},
            new int[]{1,0,2},
            new int[]{2,1,0}
        };

        foreach (int tier in order[Mathf.Clamp(preferredTier, 0, 2)])
        {
            Skill_Data s = PickRandomSkillFromTier(tier, t0, t1, t2);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>
    /// 티어에서 랜덤 스킬 하나 선택 (없으면 null)
    /// </summary>
    private Skill_Data PickRandomSkillFromTier(int tier, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        Skill_Data[] arr = (tier == 0) ? t0 : (tier == 1) ? t1 : t2;
        if (arr == null || arr.Length == 0) return null;
        int idx = Random.Range(0, arr.Length);
        return arr[idx];
    }

    /// <summary>
    /// Skill_Data에서 스킬 인덱스를 안전하게 추출
    /// 우선순위:
    /// 1) Skill_Data.SkillIndex(또는 Id)에 값이 있는 경우 그 값 사용
    /// 2) 없다면 Character의 tier 배열에서 해당 스킬의 인덱스를 찾아 사용
    ///    (티어 혼합 인덱스가 아니라 전역 인덱스가 필요하다면 Character가 제공하는 전역 인덱스를 쓰세요)
    /// </summary>
    private int ResolveSkillIndex(Skill_Data data)
    {
        if (data.skillIndex >= 0) return data.skillIndex;

        // 0티어에서 찾기
        int idx = IndexOfInArray(character.tier0Skills, data);
        if (idx >= 0) return ToGlobalIndex(0, idx);

        // 1티어
        idx = IndexOfInArray(character.tier1Skills, data);
        if (idx >= 0) return ToGlobalIndex(1, idx);

        // 2티어
        idx = IndexOfInArray(character.tier2Skills, data);
        if (idx >= 0) return ToGlobalIndex(2, idx);

        Debug.LogWarning("[Score_System] 전역 인덱스를 찾지 못했습니다. Skill_Data가 어떤 티어 배열에도 없습니다.");
        return -1;
    }

    private int ResolveTier(Skill_Data data, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        if (IndexOfInArray(t0, data) >= 0) return 0;
        if (IndexOfInArray(t1, data) >= 0) return 1;
        if (IndexOfInArray(t2, data) >= 0) return 2;
        return 0;
    }

    private int IndexOfInArray(Skill_Data[] arr, Skill_Data target)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == target) return i;
        }
        return -1;
    }

    /// <summary>
    /// Character_HaveSkill이 skillIndex를 담을 수 있도록 확장
    /// </summary>
    private void EnsureHaveSkillCapacity(int skillIndex)
    {
        if (character.Character_HaveSkill == null)
        {
            character.Character_HaveSkill = new int[skillIndex + 1];
            return;
        }
        if (skillIndex < character.Character_HaveSkill.Length) return;

        int newLen = Mathf.Max(character.Character_HaveSkill.Length * 2, skillIndex + 1);
        var newArr = new int[newLen];
        System.Array.Copy(character.Character_HaveSkill, newArr, character.Character_HaveSkill.Length);
        character.Character_HaveSkill = newArr;
    }

    // 스토리모드 확정 스킬 보상 유틸
    public void AddGuaranteedSkillByIndex(int globalSkillIndex)
    {
        if (character == null || globalSkillIndex < 0) return;

        if (!TryFromGlobalIndex(globalSkillIndex, out int tier, out int local))
        {
            Debug.LogWarning($"[Score_System] 전역 인덱스({globalSkillIndex})가 유효하지 않습니다. T0={T0Len}, T1={T1Len}, T2={T2Len}");
            return;
        }

        Skill_Data picked = null;
        if (tier == 0) picked = character.tier0Skills[local];
        else if (tier == 1) picked = character.tier1Skills[local];
        else if (tier == 2) picked = character.tier2Skills[local];

        if (picked == null)
        {
            Debug.LogWarning($"[Score_System] 보장 스킬(global={globalSkillIndex}) 객체가 null 입니다.");
            return;
        }

        bool wasKnown = picked.isKnow;
        if (!wasKnown) picked.isKnow = true;

        EnsureHaveSkillCapacity(globalSkillIndex);
        character.Character_HaveSkill[globalSkillIndex] += 1;

        LastAwarded.Add(new AwardedSkillInfo
        {
            skill = picked,
            skillIndex = globalSkillIndex,
            tier = tier,
            isNew = !wasKnown,
            source = AwardedSkillInfo.AwardSource.Guaranteed
        });
    }


    // (tier, localIndex) -> globalIndex
    private int ToGlobalIndex(int tier, int localIndex)
    {
        if (tier == 0) return localIndex;
        if (tier == 1) return T0Len + localIndex;
        if (tier == 2) return T0Len + T1Len + localIndex;
        return -1;
    }

    // globalIndex -> (tier, localIndex)
    private bool TryFromGlobalIndex(int globalIndex, out int tier, out int localIndex)
    {
        tier = 0; localIndex = 0;
        if (globalIndex < 0) return false;

        if (globalIndex < T0Len)
        {
            tier = 0; localIndex = globalIndex; return true;
        }
        globalIndex -= T0Len;
        if (globalIndex < T1Len)
        {
            tier = 1; localIndex = globalIndex; return true;
        }
        globalIndex -= T1Len;
        if (globalIndex < T2Len)
        {
            tier = 2; localIndex = globalIndex; return true;
        }
        return false;
    }
}
