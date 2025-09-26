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

    [Header("Popup")]
    public Transform popupParent;              // 팝업을 붙일 부모(FullScreen Canvas 밑)
    public GameObject skillUpgradeUIPrefab;    // SkillUpgradeUI 프리팹
    private SkillUpgradeUI currentPopup;       // 중복 생성 방지/관리

    // 페이징 및 필터 관련 변수
    private int currentPage = 0;
    private int skillsPerPage = 16;
    // filterTier 값: -1 (또는 0)인 경우 전체 스킬, 그 외 0, 1, 2 등 특정 티어만 필터
    private int filterTier = -1;

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

        // 기본 화면 갱신
        RefreshSkillGrid();
    }
    // 특정 티어 버튼에서 호출할 메서드 예시
    public void OnTierFilterButtonClicked(int tier)
    {
        filterTier = tier;   // 예: 0이면 tier0, 1이면 tier1, 2이면 tier2
        currentPage = 0;     // 필터 변경 시 첫 페이지부터 시작
        RefreshSkillGrid();
    }

    // 전체 보기 버튼을 위한 메서드 (티어 필터 해제)
    public void OnClearFilterButtonClicked()
    {
        filterTier = -1; // 전체 스킬 보기
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
        // 기존 아이콘 삭제
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // 필터 조건에 따라 스킬 목록 구성
        List<Skill_Data> filteredSkills = new List<Skill_Data>();
        if (filterTier >= 0)
        {
            // 특정 티어만 보기: 각 티어 배열은 이미 원하는 순서로 정렬되어 있다고 가정
            if (filterTier == 0)
                filteredSkills.AddRange(tier0Skills);
            else if (filterTier == 1)
                filteredSkills.AddRange(tier1Skills);
            else if (filterTier == 2)
                filteredSkills.AddRange(tier2Skills);
        }
        else
        {
            // 전체보기: 낮은 티어부터 높은 티어 순으로 배열의 순서를 그대로 유지
            filteredSkills.AddRange(tier0Skills);
            filteredSkills.AddRange(tier1Skills);
            filteredSkills.AddRange(tier2Skills);
        }

        int startIndex = currentPage * skillsPerPage;
        int endIndex = Mathf.Min(startIndex + skillsPerPage, filteredSkills.Count);

        int t0Count = tier0Skills.Count;
        int t1Count = tier1Skills.Count;
        int t2Count = tier2Skills.Count;

        for (int i = startIndex; i < endIndex; i++)
        {
            Skill_Data skillData = filteredSkills[i];

            // 현재 아이템의 실제 (tier, localIndex) 계산
            int tier, localIndex;
            if (filterTier >= 0)
            {
                tier = filterTier;
                // 필터링 된 리스트는 해당 티어의 순서를 그대로 따르므로
                localIndex = i; // i가 0부터 시작하지 않으니 보정 필요
                // i는 filteredSkills 기준 인덱스이므로, localIndex는 (i - startIndex) 가 아님
                // → 실제 localIndex는 "필터 전체에서의 i"이므로 그냥 i가 맞음.
                // 다만 페이지가 바뀌면 i는 커지니, tier 리스트의 실제 인덱스를 써야 함:
                // filteredSkills == tierXSkills와 동일 순서이므로 아래처럼 재계산:
                if (tier == 0) localIndex = i;                       // 0..tier0Count-1
                if (tier == 1) localIndex = i;                       // 0..tier1Count-1
                if (tier == 2) localIndex = i;                       // 0..tier2Count-1

                // 하지만 페이지 시작점이 0이 아닐 때도, filteredSkills[i]의 "원래 리스트 인덱스"는 i가 맞음.
                // 단, 안전하게 원 리스트에서 IndexOf로 역참조하는 방법도 가능:
                // localIndex = GetLocalIndexByRef(skillData, tier);
                localIndex = GetLocalIndexByRef(skillData, tier);
            }
            else
            {
                // 전체 보기: tier0 → tier1 → tier2 순으로 이어붙였음
                if (i < t0Count)
                {
                    tier = 0;
                    localIndex = i;
                }
                else if (i < t0Count + t1Count)
                {
                    tier = 1;
                    localIndex = i - t0Count;
                }
                else
                {
                    tier = 2;
                    localIndex = i - t0Count - t1Count;
                }
            }

            // 아이콘 생성 및 이미지 표시
            GameObject icon = Instantiate(skillIconPrefab, gridParent);
            var iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = (skillData.isKnow) ? skillData.skillIcon : UnknowSkillIcon;
            }

            // 클릭 리스너 등록
            var btn = icon.GetComponent<Button>();
            if (btn == null) btn = icon.AddComponent<Button>();

            // 캡쳐 변수 백업
            int capturedTier = tier;
            int capturedLocalIndex = localIndex;
            Skill_Data capturedRef = skillData;

            btn.onClick.AddListener(() =>
            {
                // 1) 미습득(unknown) 스킬은 무시 (원하면 토스트/팝업으로 가이드 출력)
                if (!capturedRef.isKnow)
                {
                    Debug.Log("아직 알지 못하는 스킬입니다.");
                    return;
                }

                // 2) 중복 팝업 방지
                if (currentPopup != null)
                {
                    Destroy(currentPopup.gameObject);
                    currentPopup = null;
                }

                // 3) 팝업 생성
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

                // 4) 팝업 초기화
                currentPopup.Init(
                    character,
                    capturedTier,
                    capturedLocalIndex,
                    GetSkillFromCharacter,      // 최신 스킬 참조를 다시 뽑아오는 함수
                    onClosed: () =>
                    {
                        currentPopup = null;
                        // 닫힐 때 그리드 갱신
                        RefreshSkillGrid();
                    },
                    onUpgraded: () =>
                    {
                        // 강화 직후에도 그리드 갱신(아이콘 표시 변동 등 대비)
                        RefreshSkillGrid();
                    }
                );
            });
        }

        // 페이지 이동 버튼 활성화/비활성화 처리
        previousButton.interactable = (currentPage > 0);
        nextButton.interactable = (startIndex + skillsPerPage) < filteredSkills.Count;
    }

    // 원본 리스트 내 "로컬 인덱스" 찾기 (참조 동등성 기준)
    private int GetLocalIndexByRef(Skill_Data skill, int tier)
    {
        if (tier == 0) return tier0Skills.IndexOf(skill);
        if (tier == 1) return tier1Skills.IndexOf(skill);
        if (tier == 2) return tier2Skills.IndexOf(skill);
        return -1;
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
}
