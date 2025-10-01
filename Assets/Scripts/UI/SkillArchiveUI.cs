using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillArchiveUI : MonoBehaviour
{
    public Character character;
    // Character 파일 내 스킬 데이터 (각 배열은 미리 Character 객체에서 할당)
    public List<Skill_Data> tier0Skills;
    public List<Skill_Data> tier1Skills;
    public List<Skill_Data> tier2Skills;

    // 통합 스킬 데이터 목록
    private List<Skill_Data> allSkills;

    // UI 관련 변수
    public Transform gridParent;         // Grid Layout Group이 적용된 컨테이너
    public Sprite UnknowSkillIcon;      // 배우지 않은 스킬 아이콘
    public GameObject skillIconPrefab;   // 스킬 아이콘 프리팹
    public Button previousButton;
    public Button nextButton;
    public Sprite starSprite;

    [Header("Popup")]
    public Transform popupParent;              // 팝업을 붙일 부모(FullScreen Canvas 밑)
    public GameObject skillUpgradeUIPrefab;    // SkillUpgradeUI 프리팹
    private SkillUpgradeUI currentPopup;       // 중복 생성 방지/관리

    [Header("Bulk Upgrade")]
    public Button allUpgradeButton;   // AllUpgradeButton 연결
    public GameObject bulkResultPopupPrefab;   // 모달 팝업 프리팹(배경+패널+스크롤+닫기)
    public GameObject bulkResultItemPrefab;    // 스크롤 아이템 프리팹(아이콘+텍스트들)

    private string exclamationChildName = "bUpgrade";   //프리팹 업그레이드 가능 확인 이미지 이름
    private GameObject currentBulkPopup = null;

    // 페이징 및 필터 관련 변수
    private int currentPage = 0;
    private int skillsPerPage = 16;
    // filterTier 값: -1 (또는 0)인 경우 전체 스킬, 그 외 0, 1, 2 등 특정 티어만 필터
    private enum FilterMode
    {
        AllTiers,       // 기존: 전체 보기(티어 0→1→2 순)
        Tier0Only,
        Tier1Only,
        Tier2Only,
        LearnedOnly,    // 티어 무시, 배운 스킬만
        UpgradableOnly  // 티어 무시, 업그레이드 가능한 스킬만
    }
    private FilterMode filterMode = FilterMode.AllTiers;
    private struct SkillRef
    {
        public Skill_Data data;
        public int tier;
        public int localIndex; // 각 tier 리스트 안의 인덱스
        public SkillRef(Skill_Data d, int t, int li) { data = d; tier = t; localIndex = li; }
    }
    private class UpgradeResult
    {
        public Skill_Data skill;
        public int tier;
        public int prevLevel;
        public int newLevel;
        public int spent;     // 소비한 스킬 조각(수)
        public Sprite icon;
    }

    void Start()
    {
        tier0Skills.AddRange(character.tier0Skills);
        tier1Skills.AddRange(character.tier1Skills);
        tier2Skills.AddRange(character.tier2Skills);

        // Character 파일 내에서 스킬 데이터를 모두 병합
        allSkills = new List<Skill_Data>();
        allSkills.AddRange(tier0Skills);
        allSkills.AddRange(tier1Skills);
        allSkills.AddRange(tier2Skills);

        if (allUpgradeButton != null)
            allUpgradeButton.onClick.AddListener(OnClickAllUpgrade);

        // 기본 화면 갱신
        RefreshSkillGrid();
    }
    // 특정 티어 버튼에서 호출할 메서드 예시
    public void OnTierFilterButtonClicked(int tier)
    {
        filterMode = (FilterMode)(tier + 1);   // 예: 0이면 tier0, 1이면 tier1, 2이면 tier2
        currentPage = 0;     // 필터 변경 시 첫 페이지부터 시작
        RefreshSkillGrid();
    }

    // 전체 보기 버튼을 위한 메서드 (티어 필터 해제)
    public void OnClearFilterButtonClicked()
    {
        filterMode = FilterMode.AllTiers;
        currentPage = 0;
        RefreshSkillGrid();
    }
    // 배운것만 보기
    public void OnShowLearnedOnly()
    {
        filterMode = FilterMode.LearnedOnly;
        currentPage = 0;
        RefreshSkillGrid();
    }
    //업그레이드 가능한 것만 보기
    public void OnShowUpgradableOnly()
    {
        filterMode = FilterMode.UpgradableOnly;
        currentPage = 0;
        RefreshSkillGrid();
    }

    // 페이지 이동 버튼 메서드
    public void OnClickNextPage()
    {
        currentPage++;
        RefreshSkillGrid();
    }

    public void OnClickPreviousPage()
    {
        currentPage--;
        RefreshSkillGrid();
    }

    // 스킬 그리드를 갱신하는 메서드
    public void RefreshSkillGrid()
    {
        // 기존 아이콘 제거
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        // 필터 결과
        List<SkillRef> filtered = BuildFilteredList();

        // 페이징 계산(4x4 = 16)
        int startIndex = currentPage * skillsPerPage;      // skillsPerPage=16
        int endIndex = Mathf.Min(startIndex + skillsPerPage, filtered.Count);

        // 페이지 범위 보정(현재 페이지가 너무 뒤로 가있을 때)
        if (startIndex >= filtered.Count && filtered.Count > 0)
        {
            currentPage = Mathf.Max(0, (filtered.Count - 1) / skillsPerPage);
            startIndex = currentPage * skillsPerPage;
            endIndex = Mathf.Min(startIndex + skillsPerPage, filtered.Count);
        }

        // 아이콘 생성
        for (int i = startIndex; i < endIndex; i++)
        {
            var sref = filtered[i];
            Skill_Data skillData = sref.data;

            GameObject icon = Instantiate(skillIconPrefab, gridParent);

            // 아이콘 이미지
            var iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                // LearnedOnly/UpgradableOnly에서는 대부분 isKnow=true지만, 안전하게 처리
                iconImage.sprite = (skillData.isKnow) ? skillData.skillIcon : UnknowSkillIcon;
            }
            // skillIconPrefab 안에 "SUpgradePanel"이라는 이름의 자식을 가져온다.
            var sUpgradePanel = icon.transform.Find("SUpgradePanel");
            if (sUpgradePanel != null)
            {
                // 레벨 10당 별 1개
                int starCount = Mathf.Clamp(skillData.level / 10, 0, 10);
                PopulateStarGrid(sUpgradePanel, starCount);
            }
            else
            {
                Debug.LogWarning("[SkillArchiveUI] SUpgradePanel을 찾을 수 없습니다. 프리팹 구조를 확인하세요.");
            }
            // 느낌표(bUpgrade) 토글
            Transform exMarkTr = icon.transform.Find(exclamationChildName);
            if (exMarkTr != null)
            {
                bool canUpgrade = IsUpgradable(skillData, sref.tier, sref.localIndex);
                exMarkTr.gameObject.SetActive(canUpgrade);
            }

            // 버튼 연결
            var btn = icon.GetComponent<Button>() ?? icon.AddComponent<Button>();
            int capturedTier = sref.tier;
            int capturedLocalIndex = sref.localIndex;
            Skill_Data capturedRef = skillData;

            // 배운 것만 보기 모드에서는 사실상 전부 isKnow지만, 기본 로직은 유지
            btn.interactable = capturedRef.isKnow;

            btn.onClick.AddListener(() =>
            {
                if (!capturedRef.isKnow)
                {
                    Debug.Log("아직 알지 못하는 스킬입니다.");
                    return;
                }

                if (currentPopup != null)
                {
                    Destroy(currentPopup.gameObject);
                    currentPopup = null;
                }

                if (skillUpgradeUIPrefab == null)
                {
                    Debug.LogError("[SkillArchiveUI] skillUpgradeUIPrefab이 설정되지 않았습니다.");
                    return;
                }

                Transform parent = popupParent != null ? popupParent : this.transform.root;
                var go = Instantiate(skillUpgradeUIPrefab, parent);
                currentPopup = go.GetComponent<SkillUpgradeUI>();
                if (currentPopup == null)
                {
                    Debug.LogError("[SkillArchiveUI] SkillUpgradeUI 컴포넌트가 프리팹에 없습니다.");
                    return;
                }

                currentPopup.Init(
                    character,
                    capturedTier,
                    capturedLocalIndex,
                    GetSkillFromCharacter,
                    onClosed: () =>
                    {
                        currentPopup = null;
                        RefreshSkillGrid();   // 닫힐 때 갱신
                },
                    onUpgraded: () =>
                    {
                        RefreshSkillGrid();   // 강화 직후 갱신
                }
                );
            });
        }

        // 페이지 버튼 활성화
        previousButton.interactable = (currentPage > 0);
        nextButton.interactable = (startIndex + skillsPerPage) < filtered.Count;
    }

    // 팝업에서 최신 데이터를 읽어오기 위한 함수
    private Skill_Data GetSkillFromCharacter(int tier, int localIndex)
    {
        if (character == null) return null;
        switch (tier)
        {
            case 0:
                if (character.tier0Skills != null && localIndex >= 0 && localIndex < character.tier0Skills.Length)
                    return character.tier0Skills[localIndex];
                break;
            case 1:
                if (character.tier1Skills != null && localIndex >= 0 && localIndex < character.tier1Skills.Length)
                    return character.tier1Skills[localIndex];
                break;
            case 2:
                if (character.tier2Skills != null && localIndex >= 0 && localIndex < character.tier2Skills.Length)
                    return character.tier2Skills[localIndex];
                break;
        }
        return null;
    }

    // Character의 전역 인덱스 계산 (Tier0 → Tier1 → Tier2 순)
    private int GetGlobalIndexFromTierLocal(int tier, int localIndex)
    {
        if (character == null) return -1;
        int offset = 0;
        if (tier > 0) offset += (character.tier0Skills?.Length ?? 0);
        if (tier > 1) offset += (character.tier1Skills?.Length ?? 0);
        return offset + localIndex;
    }

    private int GetHaveCount(int globalIndex)
    {
        if (character == null || character.Character_HaveSkill == null) return 0;
        if (globalIndex < 0 || globalIndex >= character.Character_HaveSkill.Length) return 0;
        return character.Character_HaveSkill[globalIndex];
    }
    private int GetMaxLevelByTier(int tier)
    {
        return (tier == 0) ? 100 : 50;
    }
    private bool IsUpgradable(Skill_Data s, int tier, int localIndex)
    {
        if (s == null) return false;

        // 1) 모르는 스킬은 업그레이드 X
        if (!s.isKnow) return false;

        // 2) 최대 레벨 체크 로직이 있다면 여기서 걸러주기
        if (s.level == GetMaxLevelByTier(s.Tier)) return false;

        // 3) Need / Have 계산 (SkillUpgradeUI와 동일한 해석)
        int need = Mathf.Max(1, s.NeedLevelUP_Gold);
        int globalIndex = GetGlobalIndexFromTierLocal(tier, localIndex);
        int have = GetHaveCount(globalIndex);

        return have >= need;
    }
    // 업그레이드에 따른 별 추가
    // 업그레이드에 따른 별 추가
    private void PopulateStarGrid(Transform panel, int starCount)
    {
        if (panel == null) return;

        // 기존 자식 제거(중복 생성 방지)
        for (int i = panel.childCount - 1; i >= 0; i--)
            Destroy(panel.GetChild(i).gameObject);

        // GridLayoutGroup 보장
        var grid = panel.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = panel.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(20f, 20f);

        // 칸 간격 5px
        grid.spacing = new Vector2(5f, 5f);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;         // 5열 고정 → 5x2 형태

        // 정렬 기준: 오른쪽 아래
        grid.startCorner = GridLayoutGroup.Corner.LowerRight;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.LowerRight;

        // 가장자리 간격 10px
        grid.padding = new RectOffset(10, 10, 10, 10);

        // 별 생성 (최대 10개)
        starCount = Mathf.Clamp(starCount, 0, 10);
        for (int i = 0; i < starCount; i++)
        {
            var starGO = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            starGO.transform.SetParent(panel, false);

            var rt = (RectTransform)starGO.transform;
            rt.sizeDelta = new Vector2(20f, 20f);

            var img = starGO.GetComponent<Image>();
            img.sprite = starSprite;
            img.preserveAspect = true;   // 이미지 비율 유지
            img.raycastTarget = false;   // 클릭 방해 X
        }
    }



    //필터 리스트
    private List<SkillRef> BuildFilteredList()
    {
        var list = new List<SkillRef>();

        void AddTierList(List<Skill_Data> source, int tierId)
        {
            for (int idx = 0; idx < source.Count; idx++)
            {
                var s = source[idx];
                // LearnedOnly
                if (filterMode == FilterMode.LearnedOnly && !s.isKnow) continue;
                // UpgradableOnly
                if (filterMode == FilterMode.UpgradableOnly && !IsUpgradable(s, tierId, idx)) continue;

                list.Add(new SkillRef(s, tierId, idx));
            }
        }

        switch (filterMode)
        {
            case FilterMode.Tier0Only:
                AddTierList(tier0Skills, 0);
                break;
            case FilterMode.Tier1Only:
                AddTierList(tier1Skills, 1);
                break;
            case FilterMode.Tier2Only:
                AddTierList(tier2Skills, 2);
                break;
            case FilterMode.LearnedOnly:
            case FilterMode.UpgradableOnly:
            case FilterMode.AllTiers:
            default:
                // 티어 순서 유지 (0→1→2)
                AddTierList(tier0Skills, 0);
                AddTierList(tier1Skills, 1);
                AddTierList(tier2Skills, 2);
                break;
        }

        return list;
    }

    // allupgrade 
    private void OnClickAllUpgrade()
    {
        if (character == null || character.Character_HaveSkill == null) return;

        var results = new List<UpgradeResult>();

        // 0→1→2 티어 순으로 처리
        UpgradeTierList(tier0Skills, 0, results);
        UpgradeTierList(tier1Skills, 1, results);
        UpgradeTierList(tier2Skills, 2, results);

        // UI 갱신
        RefreshSkillGrid();

        // 팝업 띄우기
        ShowBulkUpgradePopup(results);
    }

    private void UpgradeTierList(List<Skill_Data> list, int tier, List<UpgradeResult> results)
    {
        if (list == null || character == null || character.Character_HaveSkill == null) return;

        for (int localIndex = 0; localIndex < list.Count; localIndex++)
        {
            var s = list[localIndex];
            if (s == null || !s.isKnow) continue;

            int maxLv = GetMaxLevelByTier(s.Tier);
            if (s.level >= maxLv) continue;

            int globalIndex = GetGlobalIndexFromTierLocal(tier, localIndex);
            if (globalIndex < 0 || globalIndex >= character.Character_HaveSkill.Length) continue;

            int startLevel = s.level;
            int spent = 0;

            // 안전 가드(무한루프 방지)
            int guard = 0;
            while (s.level < maxLv && guard++ < 1000)
            {
                int have = character.Character_HaveSkill[globalIndex];
                int need = Mathf.Max(1, s.NeedLevelUP_Gold);   // 현재 레벨 기준 요구 조각 수

                if (have < need) break;

                // 소비량 집계는 호출 전에 현재 need를 누적
                spent += need;

                // 실제 레벨업(요구치/데미지/보정/조각 차감은 Character.LevelUpSkill 내부에서 처리)
                character.LevelUpSkill(tier, localIndex);
            }

            if (s.level > startLevel)
            {
                results.Add(new UpgradeResult
                {
                    skill = s,
                    tier = tier,
                    prevLevel = startLevel,
                    newLevel = s.level,
                    spent = spent,
                    icon = s.skillIcon
                });
            }
        }
    }


    private void ShowBulkUpgradePopup(List<UpgradeResult> results)
    {
        // 기존 팝업이 있다면 제거
        if (currentBulkPopup != null)
        {
            Destroy(currentBulkPopup);
            currentBulkPopup = null;
        }

        if (bulkResultPopupPrefab == null)
        {
            Debug.LogError("[SkillArchiveUI] bulkResultPopupPrefab이 설정되지 않았습니다.");
            return;
        }
        if (bulkResultItemPrefab == null)
        {
            Debug.LogError("[SkillArchiveUI] bulkResultItemPrefab이 설정되지 않았습니다.");
            return;
        }

        // 부모 설정
        Transform parent = popupParent != null ? popupParent : this.transform.root;

        // 팝업 인스턴스
        var popupGO = Instantiate(bulkResultPopupPrefab, parent);
        currentBulkPopup = popupGO;

        // 뒤 클릭 차단(배경 딤 이미지가 반드시 raycastTarget=true)
        var backdropImg = popupGO.GetComponent<Image>();
        if (backdropImg != null) backdropImg.raycastTarget = true;

        // 필수 트랜스폼들 탐색
        var panel = popupGO.transform.Find("Panel");
        if (panel == null)
        {
            Debug.LogError("[SkillArchiveUI] Popup Prefab에 'Panel' 오브젝트가 없습니다.");
            return;
        }

        // CloseButton
        var closeBtnTr = panel.Find("CloseButton");
        if (closeBtnTr == null)
        {
            Debug.LogError("[SkillArchiveUI] Popup Prefab에 'CloseButton'이 없습니다.");
            return;
        }
        var closeBtn = closeBtnTr.GetComponent<Button>();
        if (closeBtn == null)
        {
            Debug.LogError("[SkillArchiveUI] CloseButton에 Button 컴포넌트가 없습니다.");
            return;
        }

        // Scroll 구조
        var scroll = panel.Find("Scroll")?.GetComponent<ScrollRect>();
        if (scroll == null)
        {
            Debug.LogError("[SkillArchiveUI] 'Panel/Scroll(ScrollRect)'을 찾을 수 없습니다.");
            return;
        }
        var viewport = scroll.transform.Find("Viewport") as RectTransform;
        var content = viewport?.Find("Content") as RectTransform;
        if (viewport == null || content == null)
        {
            Debug.LogError("[SkillArchiveUI] Scroll 안에 'Viewport/Content' 구조가 정확하지 않습니다.");
            return;
        }

        // 스크롤 초기화(기존 항목 제거)
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // 결과 없을 때 메시지
        if (results == null || results.Count == 0)
        {
            var emptyGO = new GameObject("EmptyText", typeof(RectTransform), typeof(Text));
            emptyGO.transform.SetParent(content, false);
            var t = emptyGO.GetComponent<Text>();
            t.text = "강화 가능한 스킬이 없습니다.";
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.fontSize = 20;
        }
        else
        {
            // 0 → 1 → 2 티어 순으로 이미 정렬되어 results가 들어옴
            foreach (var r in results)
            {
                CreateResultItem(content, r);
            }
        }

        // 닫기
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() =>
        {
            Destroy(popupGO);
            currentBulkPopup = null;
            // 도감 UI 다시 상호작용 가능
            RefreshSkillGrid();
        });
    }
    private void CreateResultItem(Transform content, UpgradeResult r)
    {
        var item = Instantiate(bulkResultItemPrefab, content);
        // 필수 하위 찾기
        var icon = item.transform.Find("SkillIconImage")?.GetComponent<Image>();
        var nameText = item.transform.Find("SkillNameText")?.GetComponent<Text>();
        var infoText = item.transform.Find("SkillInfoText")?.GetComponent<Text>();

        if (icon == null || nameText == null || infoText == null)
        {
            Debug.LogError("[SkillArchiveUI] bulkResultItemPrefab 구조가 잘못되었습니다. (Icon/NameText/InfoText 필요)");
            return;
        }

        // 데이터 바인딩
        icon.sprite = (r.icon != null) ? r.icon : UnknowSkillIcon;
        icon.preserveAspect = true;

        nameText.text = $"{r.skill.skillName}  [Tier {r.tier}]";
        infoText.text = $"Lv {r.prevLevel} → {r.newLevel}   (-{r.spent})";
    }

}
