using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Data")]
    public Character cha;
    public Text charactergold;

    [Header("Gacha Settings")]
    [Tooltip("10연 뽑기 비용(골드)")]
    public int tenPullCost = 1000;

    [Tooltip("티어 가중치(합계 아무 값이어도 비율만 사용). 예: 70/25/5")]
    public int weightTier0 = 70;
    public int weightTier1 = 25;
    public int weightTier2 = 5;

    [Header("Grid UI (결과 표시)")]
    public ScrollRect scrollRect;
    public RectTransform contentRect;
    public GridLayoutGroup grid;
    public GameObject skillIconPrefab;
    [Range(1, 10)] public int columns = 5;  // 5열 고정 추천 (2행 x 5 = 10)

    [Header("Child 찾기 이름(선택)")]
    public string countTextObjectName = "CountText";
    public string newBadgeObjectName = "NewText";

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (cha == null || charactergold == null) return;
        charactergold.text = "보유한 골드:" + cha.Character_Gold.ToString();
    }

    /// <summary>
    /// Get 버튼에 바인딩: 10연 가챠 실행
    /// </summary>
    public void OnClick_Get10()
    {
        if (cha == null)
        {
            Debug.LogWarning("[ShopUI] Character가 없습니다.");
            return;
        }
        if (cha.Character_Gold < tenPullCost)
        {
            Debug.LogWarning($"[ShopUI] 골드 부족 ({cha.Character_Gold} / {tenPullCost})");
            return;
        }

        // 결제
        cha.Character_Gold -= tenPullCost;
        UpdateUI();

        // 10개 뽑기
        var awards = DrawTenSkillsWeighted();

        // 결과 UI 구성(CountText 숨김, NEW만 표시)
        BuildTenRevealGrid(awards);

        // 저장(선택)
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGameData();
        }
    }

    // ================================
    // 내부 로직
    // ================================

    private class Award
    {
        public Skill_Data skill;    // 아이콘, isKnow 포함
        public int tier;            // 0/1/2
        public int index;           // 해당 tier 배열 내 인덱스(전역 인덱스 규칙 사용 시 프로젝트에 맞게 변환)
        public bool isNew;          // 이번에 처음 알게 된 경우
    }

    /// <summary>
    /// 인스펙터 가중치 기반으로 10개 추첨하여
    /// Character_HaveSkill에 반영하고 NEW 판정을 만든다.
    /// </summary>
    private List<Award> DrawTenSkillsWeighted()
    {
        var results = new List<Award>(10);

        // 풀 모으기
        var t0 = cha.tier0Skills;
        var t1 = cha.tier1Skills;
        var t2 = cha.tier2Skills;

        // 사용 가능한 티어 목록
        var pools = new List<(Skill_Data[] arr, int tier)>
        {
            (t0, 0), (t1, 1), (t2, 2)
        }.Where(p => p.arr != null && p.arr.Length > 0).ToList();

        if (pools.Count == 0)
        {
            Debug.LogWarning("[ShopUI] 뽑을 수 있는 스킬 풀이 없습니다. (tier0/1/2 배열 확인)");
            return results;
        }

        for (int i = 0; i < 10; i++)
        {
            int tier = PickTierByWeightSafe(weightTier0, weightTier1, weightTier2, pools);
            Skill_Data picked = PickRandomFromTierWithFallback(tier, t0, t1, t2);
            if (picked == null) continue;

            bool wasKnown = picked.isKnow;
            if (!wasKnown) picked.isKnow = true; // NEW → Known

            // 인덱스/티어 결정
            int skillIndex = ResolveSkillIndex(picked);
            int resolvedTier = ResolveTier(picked, t0, t1, t2);
            int tierLocalIndex = IndexOfInArray(GetTierArray(resolvedTier, t0, t1, t2), picked);

            // Character_HaveSkill에 적용
            if (skillIndex >= 0)
            {
                EnsureHaveSkillCapacity(skillIndex);
                cha.Character_HaveSkill[skillIndex] += 1;
            }
            else
            {
                Debug.LogWarning("[ShopUI] SkillIndex를 확인할 수 없어 HaveSkill에 반영되지 않았습니다. Skill_Data.skillIndex 또는 전역 인덱스 매핑을 확인하세요.");
            }

            results.Add(new Award
            {
                skill = picked,
                tier = resolvedTier,
                index = Mathf.Max(0, tierLocalIndex),
                isNew = !wasKnown
            });
        }

        return results;
    }

    /// <summary>
    /// 배열에서 안전하게 랜덤 추출. 선호 티어가 비어있으면 인접 티어로 폴백.
    /// </summary>
    private Skill_Data PickRandomFromTierWithFallback(int preferredTier, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        int[][] order =
        {
            new int[]{0,1,2},
            new int[]{1,0,2},
            new int[]{2,1,0}
        };

        foreach (int tier in order[Mathf.Clamp(preferredTier, 0, 2)])
        {
            var arr = GetTierArray(tier, t0, t1, t2);
            if (arr != null && arr.Length > 0)
            {
                int idx = Random.Range(0, arr.Length);
                return arr[idx];
            }
        }
        return null;
    }

    private Skill_Data[] GetTierArray(int tier, Skill_Data[] t0, Skill_Data[] t1, Skill_Data[] t2)
    {
        return tier == 0 ? t0 : (tier == 1 ? t1 : t2);
    }

    /// <summary>
    /// 사용 가능한 풀만 고려한 가중치 티어 선택
    /// </summary>
    private int PickTierByWeightSafe(int w0, int w1, int w2, List<(Skill_Data[] arr, int tier)> available)
    {
        int a0 = (available.Any(p => p.tier == 0) ? Mathf.Max(0, w0) : 0);
        int a1 = (available.Any(p => p.tier == 1) ? Mathf.Max(0, w1) : 0);
        int a2 = (available.Any(p => p.tier == 2) ? Mathf.Max(0, w2) : 0);

        int total = a0 + a1 + a2;
        if (total <= 0)
        {
            // 모두 0이면 사용 가능한 첫 티어 리턴
            return available[0].tier;
        }

        int r = Random.Range(0, total);
        if (r < a0) return 0;
        r -= a0;
        if (r < a1) return 1;
        return 2;
    }

    /// <summary>
    /// Score_System의 규칙을 반영한 인덱스 해석:
    /// 1) Skill_Data.skillIndex가 유효하면 사용
    /// 2) 아니면 tier0/1/2 배열에서 위치 탐색(전역 인덱스 규칙을 따로 쓰면 프로젝트에 맞게 변경)
    /// </summary>
    private int ResolveSkillIndex(Skill_Data data)
    {
        if (data == null) return -1;

        if (data.skillIndex >= 0) return data.skillIndex;

        int idx = IndexOfInArray(cha.tier0Skills, data);
        if (idx >= 0) return idx;

        idx = IndexOfInArray(cha.tier1Skills, data);
        if (idx >= 0) return idx;

        idx = IndexOfInArray(cha.tier2Skills, data);
        if (idx >= 0) return idx;

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
            if (arr[i] == target) return i;
        return -1;
    }

    /// <summary>
    /// Character_HaveSkill이 skillIndex를 담을 수 있도록 확장
    /// </summary>
    private void EnsureHaveSkillCapacity(int skillIndex)
    {
        if (cha.Character_HaveSkill == null)
        {
            cha.Character_HaveSkill = new int[skillIndex + 1];
            return;
        }
        if (skillIndex < cha.Character_HaveSkill.Length) return;

        int newLen = Mathf.Max(cha.Character_HaveSkill.Length * 2, skillIndex + 1);
        var newArr = new int[newLen];
        System.Array.Copy(cha.Character_HaveSkill, newArr, cha.Character_HaveSkill.Length);
        cha.Character_HaveSkill = newArr;
    }

    // ================================
    // 결과 UI 구성
    // ================================

    private void BuildTenRevealGrid(List<Award> awards)
    {
        if (contentRect == null || grid == null || skillIconPrefab == null)
        {
            Debug.LogWarning("[ShopUI] Grid/Prefab 레퍼런스가 비어있습니다.");
            return;
        }

        // 기존 정리
        for (int i = contentRect.childCount - 1; i >= 0; i--)
            Destroy(contentRect.GetChild(i).gameObject);

        if (awards == null || awards.Count == 0)
        {
            UpdateContentHeight(0);
            return;
        }

        foreach (var a in awards)
        {
            var go = Instantiate(skillIconPrefab, contentRect);
            var item = go.GetComponent<ResultSkillIcon>();

            Sprite icon = (a.skill != null) ? a.skill.skillIcon : null;
            bool showNew = a.isNew;

            if (item != null)
            {
                // count=1로 넘기되, CountText는 숨김 처리
                item.Setup(icon, 1, showNew);
            }

            HideCountText(go);
            ForceNewBadge(go, showNew);
        }

        UpdateContentHeight(awards.Count);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void HideCountText(GameObject itemGO)
    {
        if (itemGO == null) return;

        if (!string.IsNullOrEmpty(countTextObjectName))
        {
            var t = FindDeepChild(itemGO.transform, countTextObjectName);
            if (t != null) { t.gameObject.SetActive(false); return; }
        }

        var texts = itemGO.GetComponentsInChildren<Text>(true);
        var guess = texts.FirstOrDefault(tx => tx.name.ToLower().Contains("count"));
        if (guess != null) guess.gameObject.SetActive(false);
    }

    private void ForceNewBadge(GameObject itemGO, bool show)
    {
        if (itemGO == null) return;

        if (!string.IsNullOrEmpty(newBadgeObjectName))
        {
            var t = FindDeepChild(itemGO.transform, newBadgeObjectName);
            if (t != null) { t.gameObject.SetActive(show); return; }
        }

        var tfs = itemGO.GetComponentsInChildren<Transform>(true);
        var guess = tfs.FirstOrDefault(tf => tf.name.ToLower().Contains("new"));
        if (guess != null) guess.gameObject.SetActive(show);
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var res = FindDeepChild(child, name);
            if (res != null) return res;
        }
        return null;
    }

    private void UpdateContentHeight(int itemCount)
    {
        if (grid == null || contentRect == null || columns <= 0) return;

        int rows = Mathf.CeilToInt(itemCount / (float)columns);
        Vector2 cell = grid.cellSize;
        Vector2 spacing = grid.spacing;
        RectOffset pad = grid.padding ?? new RectOffset(0, 0, 0, 0);

        float totalHeight = (rows > 0)
            ? pad.top + rows * cell.y + (rows - 1) * spacing.y + pad.bottom
            : pad.top + pad.bottom;

        var size = contentRect.sizeDelta;
        contentRect.sizeDelta = new Vector2(size.x, totalHeight);
    }
}
