using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameResultUI : MonoBehaviour
{
    // UI 컴포넌트 (Inspector에서 할당)
    public Text scoreText;     // 점수를 표시할 텍스트

    public SceneLoader sceneLoader;
    public Score_System scoreSystem;

    [Header("스크롤 그리드")]
    public ScrollRect scrollRect;               // Scroll View
    public RectTransform contentRect;           // ScrollRect.content
    public GridLayoutGroup grid;                // Content에 붙은 GridLayoutGroup
    public GameObject skillIconPrefab;          // SkillIconItem 프리팹
    [Range(1, 10)]
    public int columns = 5;                     // 고정 5열

    /// <summary>
    /// GameOver 시 호출하여 결과 UI를 표시합니다.
    /// </summary>
    /// <param name="score">Score_System.cs의 score 값</param>
    public void ShowResult()
    {
        gameObject.SetActive(true);
        scoreSystem.ResultScore();

        if (scoreText != null)
            scoreText.text = "Score: " + scoreSystem.score.ToString();

        BuildSkillGrid();
    }

    /// <summary>
    /// 종료 버튼 클릭 시 실행되는 메서드
    /// </summary>
    public void OnExitButtonClicked()
    {
        //scoreSystem.ResultScore();

        // 저장
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGameData();
        }
        else
        {
            Debug.LogWarning("[GameResultUI] SaveSystem.Instance를 찾을 수 없습니다.");
        }

        // 점수 리셋 → 로비 → 시간 재개
        scoreSystem.score = 0;
        sceneLoader.LoadLobyScene();
        Time.timeScale = 1;
    }

    // ====== 내부: 스크롤 그리드 구성 ======

    private void BuildSkillGrid()
    {
        // 안전 체크
        if (contentRect == null || grid == null || skillIconPrefab == null)
        {
            Debug.LogWarning("[GameResultUI] Grid/Prefab 레퍼런스가 비어있습니다.");
            return;
        }

        // 기존 아이템 정리
        for (int i = contentRect.childCount - 1; i >= 0; i--)
            Destroy(contentRect.GetChild(i).gameObject);

        var awarded = scoreSystem.LastAwarded;
        if (awarded == null || awarded.Count == 0)
        {
            UpdateContentHeight(0);
            return;
        }

        // 1) 동일 스킬(티어+인덱스) 집계 + NEW 여부
        var grouped = GroupAndAggregate(awarded);

        // 2) 정렬: 티어 ASC → 인덱스 ASC
        var sorted = grouped
            .OrderBy(g => g.tier)
            .ThenBy(g => g.skillIndex)
            .ToList();

        // 3) 아이템 생성(모두 생성: 5 x n)
        foreach (var g in sorted)
        {
            var go = Instantiate(skillIconPrefab, contentRect);
            var item = go.GetComponent<ResultSkillIcon>();

            // 아이콘 필드명 프로젝트에 맞게 수정
            Sprite icon = g.sample != null ? g.sample.skillIcon : null;
            bool showNew = g.anyNew;
            int count = g.count;

            if (item != null)
                item.Setup(icon, count, showNew);
        }

        // 4) 컨텐츠 높이 계산/적용
        UpdateContentHeight(sorted.Count);

        // 수직 스크롤 시작 위치 맨 위로
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private class Grouped
    {
        public Skill_Data sample;
        public int skillIndex;
        public int tier;
        public int count;
        public bool anyNew;
    }

    private List<Grouped> GroupAndAggregate(List<Score_System.AwardedSkillInfo> awarded)
    {
        var dict = new Dictionary<(int tier, int idx), Grouped>();
        foreach (var a in awarded)
        {
            var key = (a.tier, a.skillIndex);
            if (!dict.TryGetValue(key, out var g))
            {
                g = new Grouped
                {
                    sample = a.skill,
                    skillIndex = a.skillIndex,
                    tier = a.tier,
                    count = 0,
                    anyNew = false
                };
                dict[key] = g;
            }
            g.count += 1;
            if (a.isNew) g.anyNew = true;
        }
        return dict.Values.ToList();
    }

    /// <summary>
    /// 아이템 개수로 행 수를 계산해 Content 높이를 갱신
    /// </summary>
    private void UpdateContentHeight(int itemCount)
    {
        if (grid == null || contentRect == null || columns <= 0) return;

        int rows = Mathf.CeilToInt(itemCount / (float)columns);

        // GridLayoutGroup 파라미터
        Vector2 cell = grid.cellSize;
        Vector2 spacing = grid.spacing;
        RectOffset pad = grid.padding ?? new RectOffset(0, 0, 0, 0);

        float totalHeight = 0f;
        if (rows > 0)
        {
            totalHeight =
                pad.top +
                rows * cell.y +
                (rows - 1) * spacing.y +
                pad.bottom;
        }
        else
        {
            totalHeight = pad.top + pad.bottom; // 아이템 없을 때 최소
        }

        // 현재 폭 유지, 높이만 조정
        var size = contentRect.sizeDelta;
        contentRect.sizeDelta = new Vector2(size.x, totalHeight);
    }
}
