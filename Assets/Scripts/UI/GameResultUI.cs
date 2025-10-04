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

    private bool _clearCommitted = false;

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


    public void ShowResult()
    {
        _clearCommitted = false;
        gameObject.SetActive(true);

        // 보상 계산
        scoreSystem.ResultScore();

        if (scoreText != null) scoreText.text = "Score: " + scoreSystem.score.ToString();
        if (waveText != null) waveText.text = "Wave: " + (monsterSpawn ? monsterSpawn.currentWave.ToString() : "0");
        if (moneyText != null) moneyText.text = "Money: " + scoreSystem.score.ToString();
        if (EXPText != null) EXPText.text = "EXP: " + (scoreSystem.score / 100).ToString();

        BuildSkillGrid();
        BuildGuaranteedGrid();

        // 스토리 진행: "커밋 먼저 → 다음 스테이지 미리보기 → 저장"
        bool hasNext = false;
        var sm = StoryModeManager.Instance;
        if (sm != null && sm.isStoryRun)
        {
            if (!_clearCommitted)
            {
                sm.CommitStageClear();     
                _clearCommitted = true;

                // 커밋 직후 진행 저장 (중요!)
                if (SaveSystem.Instance != null)
                    SaveSystem.Instance.SaveGameData();
                sm.PersistProgress();
            }

            StoryStageAsset next;
            hasNext = sm.TryPeekPendingNext(out next) && next != null;
        }

        if (nextStageButton != null)
            nextStageButton.interactable = hasNext;

        // 결과가 나온 시점에서 한 번 더 저장: 보상 반영 상태를 고정
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveGameData();
    }


    /// <summary>
    /// 종료 버튼 클릭 시 실행되는 메서드
    /// </summary>
    public void OnExitButtonClicked()
    {
        var sm = StoryModeManager.Instance;
        if (sm != null && sm.isStoryRun)
        {
            // 다음 스테이지가 있으면 체크포인트 갱신 (마지막이면 TryPeek 실패)
            StoryStageAsset peek;
            if (sm.TryPeekPendingNext(out peek) && peek != null)
            {
                sm.lastCheckpointStageId = peek.stageId;
                sm.PersistProgress();
            }

            // 스토리 런 상태 완전 정리
            sm.ClearRunState();
        }
        if (scoreSystem != null)
        {
            scoreSystem.BeginStageRun(); 
        }
        if (SaveSystem.Instance != null) SaveSystem.Instance.SaveGameData();
        if (StoryModeManager.Instance != null) StoryModeManager.Instance.PersistProgress();

        // 혹시라도 UI가 남아있지 않게 비활성화
        gameObject.SetActive(false);

        // 점수 리셋 + 로비 이동
        scoreSystem.score = 0;
        sceneLoader.LoadLobyScene();
        Time.timeScale = 1f;
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
            var go = InstantiateUnder(skillIconPrefab, contentRect);
            var item = go ? go.GetComponent<ResultSkillIcon>() : null;

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

        // 보상/진행을 한 번 더 안전 저장
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.SaveGameData();
        if (StoryModeManager.Instance != null)
            StoryModeManager.Instance.PersistProgress();

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
            var go = InstantiateUnder(prefab, guaranteedContentRect);
            var item = go ? go.GetComponent<ResultSkillIcon>() : null;

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
   

    private GameObject InstantiateUnder(GameObject prefab, RectTransform parent)
    {
        if (prefab == null || parent == null)
        {
            Debug.LogWarning("[GameResultUI] InstantiateUnder: prefab 혹은 parent가 null");
            return null;
        }

        // 1) 우선 임시로 인스턴스 생성(부모는 parent로 바로)
        GameObject inst = Instantiate(prefab, parent);
        var instRT = inst.GetComponent<RectTransform>();

        // 2) 만약 프리팹 루트가 Canvas를 갖고 있다면, 내부 실제 UI를 parent로 승격
        //    (Canvas/Scaler/Raycaster는 제거, 자식(첫 RectTransform)을 올려 붙인다)
        var innerCanvas = inst.GetComponentInChildren<Canvas>(true);
        if (innerCanvas != null && innerCanvas.gameObject == inst) // 루트가 Canvas인 경우
        {
            // 내부에서 "실제 UI 루트" 후보 찾기 (Canvas 바로 아래 첫 RectTransform)
            RectTransform realRoot = null;
            for (int i = 0; i < instRT.childCount; i++)
            {
                var childRT = instRT.GetChild(i) as RectTransform;
                if (childRT != null)
                {
                    realRoot = childRT;
                    break;
                }
            }

            if (realRoot != null)
            {
                // realRoot를 parent 밑으로 이동
                realRoot.SetParent(parent, false);

                // 불필요 컴포넌트 제거
                var scaler = inst.GetComponent<CanvasScaler>();
                var ray = inst.GetComponent<GraphicRaycaster>();
                if (ray) Destroy(ray);
                if (scaler) Destroy(scaler);
                Destroy(innerCanvas);

                // 빈 껍데기 제거
                Destroy(inst);
                inst = realRoot.gameObject;
                instRT = realRoot;
            }
            else
            {
                // 자식이 없다면 그냥 Canvas만 제거
                var scaler = inst.GetComponent<CanvasScaler>();
                var ray = inst.GetComponent<GraphicRaycaster>();
                if (ray) Destroy(ray);
                if (scaler) Destroy(scaler);
                Destroy(innerCanvas);
            }
        }

        // 3) RectTransform 정리(스케일/회전/오프셋)
        if (instRT != null)
        {
            instRT.anchorMin = instRT.anchorMin; // (앵커는 프리팹 설계대로 유지)
            instRT.anchorMax = instRT.anchorMax;
            instRT.pivot = instRT.pivot;

            instRT.anchoredPosition3D = Vector3.zero;
            instRT.localScale = Vector3.one;
            instRT.localRotation = Quaternion.identity;
        }

        return inst;
    }
}
