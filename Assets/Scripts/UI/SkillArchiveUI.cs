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

        // 페이지 단위로 스킬 아이콘 출력 (4x4 그리드 = 16개씩)
        int startIndex = currentPage * skillsPerPage;
        for (int i = startIndex; i < startIndex + skillsPerPage && i < filteredSkills.Count; i++)
        {
            // 스킬 데이터로 아이콘 프리팹 인스턴스 생성
            GameObject icon = Instantiate(skillIconPrefab, gridParent);
            // icon의 Image 컴포넌트에 스킬 아이콘 할당 (skillData.skillIcon)
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = (filteredSkills[i].isKnow) ? filteredSkills[i].skillIcon : UnknowSkillIcon;
            }
        }

        // 페이지 이동 버튼 활성화/비활성화 처리
        previousButton.interactable = (currentPage > 0);
        nextButton.interactable = (startIndex + skillsPerPage) < filteredSkills.Count;
    }
}
