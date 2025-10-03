using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GameResultUI : MonoBehaviour
{
    // UI 컴포넌트 (Inspector에서 할당)
    public Text scoreText;     // 점수를 표시할 텍스트
    public Text waveText;       // 웨이브 표시할 텍스트
    public Text moneyText;      // 돈 표시할 텍스트
    public Text EXPText;        // 경험치 표시할 텍스트

    public SceneLoader sceneLoader;
    public Score_System scoreSystem;
    public Monster_Spawn monsterSpawn;

    [Header("스크롤 그리드")]
    public ScrollRect scrollRect;               // Scroll View
    public RectTransform contentRect;           // ScrollRect.content
    public GridLayoutGroup grid;                // Content에 붙은 GridLayoutGroup
    public GameObject skillIconPrefab;          // SkillIconItem 프리팹
    [Range(1, 10)]
    public int columns = 5;                     // 고정 5열

    [Header("확정 보상(Guaranteed) 그리드")]
    public RectTransform guaranteedContentRect;   // 별도 Content
    public GridLayoutGroup guaranteedGrid;        // 별도 Grid
    public GameObject guaranteedSkillIconPrefab;  // (없으면 skillIconPrefab 재사용)
    public Text guaranteedHeaderText;             // "Guaranteed Rewards" 같은 헤더 텍스트(선택)


    [Header("스토리 전용 UI")]
    public Button nextStageButton; 

    public void Awake()
    {
        // Score_System/SceneLoader가 비어있으면 여기서도 한 번 안전하게 잡아도 됩니다.
        if (scoreSystem == null) scoreSystem = FindObjectOfType<Score_System>(true);
        if (sceneLoader == null) sceneLoader = FindObjectOfType<SceneLoader>(true);

        // ===== Monster_Spawn 자동 할당 =====
        if (monsterSpawn == null)
        {
            // 씬 전체에서 컴포넌트 탐색(비활성 포함)
            monsterSpawn = FindObjectOfType<Monster_Spawn>(true);
        }

        if (monsterSpawn == null)
        {
            // 태그로 재시도: 스포너 오브젝트에 "MonsterSpawner" 태그를 달아두면 확실
            GameObject tagged = GameObject.FindWithTag("MonsterSpawner");
            if (tagged != null) monsterSpawn = tagged.GetComponent<Monster_Spawn>();
        }

        if (monsterSpawn == null)
        {
            Debug.LogWarning("[GameResultUI] Monster_Spawn을 씬에서 찾지 못했습니다. Wave 표시는 0으로 대체됩니다.");
        }

        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();   //인스펙터에 남은 리스너 제거
            nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
        }
    }

    /// <summary>
    /// GameOver 시 호출하여 결과 UI를 표시합니다.
    /// </summary>
    /// <param name="score">Score_System.cs의 score 값</param>
    public void ShowResult()
    {
        gameObject.SetActive(true);
        scoreSystem.ResultScore();

        if (scoreText != null) scoreText.text = "Score: " + scoreSystem.score.ToString();
        if (waveText != null) waveText.text = "Wave: " + (monsterSpawn ? monsterSpawn.currentWave.ToString() : "0");
        if (moneyText != null) moneyText.text = "Money: " + scoreSystem.score.ToString();
        if (EXPText != null) EXPText.text = "EXP: " + (scoreSystem.score / 100).ToString();

        // 랜덤 보상 그리드
        BuildSkillGrid();

        // 확정 보상 그리드
        BuildGuaranteedGrid();

        // 다음 스테이지 버튼 처리: '다음' 존재할 때만 켜고, 없으면 완전 끔
        bool hasNext = false;
        var sm = StoryModeManager.Instance;
        if (sm != null && sm.isStoryRun)
        {
            StoryStageAsset next;
            hasNext = sm.TryPeekPendingNext(out next) && next != null;
        }
        if (nextStageButton != null)
        {
            nextStageButton.interactable = hasNext;
            // 필요하면 nextStageButton.gameObject.SetActive(hasNext);
        }
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
        if (contentRect == null || grid == null || skillIconPrefab == null)
        {
            Debug.LogWarning("[GameResultUI] Grid/Prefab 레퍼런스가 비어있습니다.");
            return;
        }

        // 기존 아이템 정리
        for (int i = contentRect.childCount - 1; i >= 0; i--)
            Destroy(contentRect.GetChild(i).gameObject);

        // 랜덤 보상만 필터
        var awardedAll = scoreSystem.LastAwarded;
        var awarded = (awardedAll == null)
            ? null
            : awardedAll
                .Where(a => a.source == Score_System.AwardedSkillInfo.AwardSource.Random)
                .ToList();

        if (awarded == null || awarded.Count == 0)
        {
            UpdateContentHeight(0);
            return;
        }

        var grouped = GroupAndAggregate(awarded)
            .OrderBy(g => g.tier)
            .ThenBy(g => g.skillIndex)
            .ToList();

        foreach (var g in grouped)
        {
            var go = Instantiate(skillIconPrefab, contentRect);
            var item = go.GetComponent<ResultSkillIcon>();

            Sprite icon = g.sample != null ? g.sample.skillIcon : null;
            bool showNew = g.anyNew;
            int count = g.count;

            if (item != null)
                item.Setup(icon, count, showNew);
        }

        UpdateContentHeight(grouped.Count);

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
    public void OnNextStageButtonClicked()
    {
        Time.timeScale = 1f;

        var sm = StoryModeManager.Instance;
        if (sm == null) return;

        sm.ConfirmAndStartNextStage();

        gameObject.SetActive(false);
    }


    private void BuildGuaranteedGrid()
    {
        if (guaranteedContentRect == null || (guaranteedGrid == null))
        {
            // 선택 섹션이 없다면 스킵(프로젝트에 따라 랜더링 안 해도 OK)
            return;
        }

        // 기존 아이템 정리
        for (int i = guaranteedContentRect.childCount - 1; i >= 0; i--)
            Destroy(guaranteedContentRect.GetChild(i).gameObject);

        var awarded = scoreSystem.LastAwarded;
        if (awarded == null || awarded.Count == 0)
        {
            if (guaranteedHeaderText) guaranteedHeaderText.gameObject.SetActive(false);
            UpdateContentHeightForGuaranteed(0);
            return;
        }

        // 확정 보상만 필터
        var guaranteedOnly = awarded
            .Where(a => a.source == Score_System.AwardedSkillInfo.AwardSource.Guaranteed)
            .ToList();

        if (guaranteedOnly.Count == 0)
        {
            if (guaranteedHeaderText) guaranteedHeaderText.gameObject.SetActive(false);
            UpdateContentHeightForGuaranteed(0);
            return;
        }

        // 헤더 노출
        if (guaranteedHeaderText) guaranteedHeaderText.gameObject.SetActive(true);

        // 동일 스킬(티어+인덱스) 집계 + NEW 여부
        var grouped = GroupAndAggregate(guaranteedOnly);

        // 정렬
        var sorted = grouped.OrderBy(g => g.tier).ThenBy(g => g.skillIndex).ToList();

        // 아이템 생성
        var prefab = guaranteedSkillIconPrefab != null ? guaranteedSkillIconPrefab : skillIconPrefab;
        foreach (var g in sorted)
        {
            var go = Instantiate(prefab, guaranteedContentRect);
            var item = go.GetComponent<ResultSkillIcon>();

            Sprite icon = g.sample != null ? g.sample.skillIcon : null;
            bool showNew = g.anyNew;
            int count = g.count;

            if (item != null)
                item.Setup(icon, count, showNew);
        }

        UpdateContentHeightForGuaranteed(sorted.Count);
    }
    private void UpdateContentHeightForGuaranteed(int itemCount)
    {
        if (guaranteedGrid == null || guaranteedContentRect == null || columns <= 0) return;

        int rows = Mathf.CeilToInt(itemCount / (float)columns);
        Vector2 cell = guaranteedGrid.cellSize;
        Vector2 spacing = guaranteedGrid.spacing;
        RectOffset pad = guaranteedGrid.padding ?? new RectOffset(0, 0, 0, 0);

        float totalHeight = (rows > 0)
            ? pad.top + rows * cell.y + (rows - 1) * spacing.y + pad.bottom
            : pad.top + pad.bottom;

        var size = guaranteedContentRect.sizeDelta;
        guaranteedContentRect.sizeDelta = new Vector2(size.x, totalHeight);
    }

}
