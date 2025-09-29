using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score_System : MonoBehaviour
{
    public Character character;
    public int score = 0;
    // 결과창에서 사용할 "이번 결과에서 획득한 스킬" 기록
    [System.Serializable]
    public class AwardedSkillInfo
    {
        public Skill_Data skill;   // 참조
        public int skillIndex;     // Character_HaveSkill 인덱스
        public int tier;           // 0/1/2
        public bool isNew;         // 이번에 isKnow=false -> true로 바뀌었는지
    }
    public List<AwardedSkillInfo> LastAwarded = new List<AwardedSkillInfo>();

    public void ResultScore()
    {
        if(score > 0)
        {
            character.Character_Gold = score;
            character.CharacterLevelUP(score/100);

            AwardRandomSkills(score);
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

        LastAwarded.Clear(); // 이번 결과 기록 초기화

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
                    isNew = !wasKnown
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
        //캐릭터가 들고있는 tier 배열에서 위치를 찾아 반환 (전역 인덱스가 아니라면 프로젝트 규칙에 맞게 매핑)
        int idx = IndexOfInArray(character.tier0Skills, data);
        if (idx >= 0) return idx; // 예: 0티어는 0~N-1 범위 사용

        idx = IndexOfInArray(character.tier1Skills, data);
        if (idx >= 0) return idx; // 전역 인덱스가 필요하면 오프셋 더하기 등으로 변경

        idx = IndexOfInArray(character.tier2Skills, data);
        if (idx >= 0) return idx;

        // 마지막 수단: 실패
        Debug.LogWarning("[Score_System] 스킬 인덱스를 확인할 수 없습니다. Skill_Data에 SkillIndex(Id) 필드를 제공하는 것을 권장합니다.");
        return -1;
    }
    private int ResolveTier(Skill_Data data, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        if (IndexOfInArray(t0, data) >= 0) return 0;
        if (IndexOfInArray(t1, data) >= 0) return 1;
        if (IndexOfInArray(t2, data) >= 0) return 2;
        return 0; // 기본 0티어
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
}
