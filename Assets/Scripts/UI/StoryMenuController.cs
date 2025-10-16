using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StoryMenuController : MonoBehaviour
{
    [Header("Data")]
    public StoryStageAssetResolver resolver;   // stages 리스트 포함(모든 스테이지 등록)
    public StoryModeManager storyManager;      // 씬에 존재(없으면 런타임에 Find)
    public string firstStageId = "0-1";

    [Header("Scroll UI")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public GameObject itemButtonPrefab;        // [story 0-1] 텍스트 버튼 프리팹

    [Header("Popup")]
    public StoryPopupUI popupPrefab;           // 팝업 프리팹
    public Transform popupParent;              // 보통 Canvas 밑(없으면 Find("Canvas"))

    private void Awake()
    {
        if (!storyManager) storyManager = StoryModeManager.Instance ?? FindObjectOfType<StoryModeManager>(true);
        if (!popupParent)
        {
            var canvas = GameObject.Find("Canvas");
            popupParent = canvas ? canvas.transform : this.transform;
        }
    }

    private void Start()
    {
        BuildList();
    }

    private int StageOrder(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        var p = id.Split('-');
        int a = 0, b = 0;
        if (p.Length >= 1) int.TryParse(p[0], out a);
        if (p.Length >= 2) int.TryParse(p[1], out b);
        // 정렬 안정성을 위해 충분히 큰 배수 사용
        return a * 1000 + b;
    }

    private string GetMaxClearedStageId()
    {
        var sm = storyManager ?? StoryModeManager.Instance ?? FindObjectOfType<StoryModeManager>(true);
        if (sm != null)
        {
            var maxId = sm.maxClearedStageId;
            if (!string.IsNullOrEmpty(maxId) && maxId != "0-0")
                return maxId;
        }
        // 아직 아무 것도 클리어하지 않았다면 최초 스테이지만 허용
        return firstStageId;
    }

    private void BuildList()
    {
        if (!resolver || resolver.stages == null || resolver.stages.Count == 0)
        {
            Debug.LogWarning("[StoryMenu] Resolver 또는 stages 비어있음");
            return;
        }

        // 최대 클리어 기준으로 보여줄 범위를 결정
        string maxCleared = GetMaxClearedStageId();
        int limit = StageOrder(maxCleared);

        var nextId = storyManager?.GetNextStageIdByOrder(maxCleared);
        if (!string.IsNullOrEmpty(nextId)) limit = Mathf.Max(limit, StageOrder(nextId));

        var candidates = resolver.stages
            .Where(s => s != null)
            .Where(s => StageOrder(s.stageId) <= limit)        // 최대 클리어 이하만 표시
            .OrderByDescending(s => StageOrder(s.stageId))     // 최근 스테이지가 위에 오도록
            .ToList();

        // 기존 자식 정리
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // 아이템 생성
        foreach (var stage in candidates)
        {
            var go = Instantiate(itemButtonPrefab, content);
            var item = go.GetComponent<StoryItemButton>();
            if (!item) item = go.AddComponent<StoryItemButton>();

            item.Setup(stage, $"story {stage.stageId}", OnItemClicked);
        }

        // 스크롤 맨 위로
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnItemClicked(StoryStageAsset stage)
    {
        // 팝업 생성 (모달)
        var popup = Instantiate(popupPrefab, popupParent);
        popup.Open(stage, OnPopupStartClicked, OnPopupCancelClicked);
    }

    private void OnPopupStartClicked(StoryStageAsset stage)
    {
        // 팝업에서 "시작" 눌렀을 때: StoryModeScene 로드 후 해당 stage 시작
        var sceneLoader = FindObjectOfType<SceneLoader>(true);
        if (sceneLoader != null) sceneLoader.LoadStoryModeScene();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("StoryModeScene");

        // 다음 스테이지 큐 (Scene load 후 StoryModeManager가 실행)
        var sm = StoryModeManager.Instance ?? FindObjectOfType<StoryModeManager>(true);
        if (sm != null) sm.QueueStartNextStage(stage);
        else Debug.LogError("[StoryMenu] StoryModeManager not found after scene load queue.");
    }

    private void OnPopupCancelClicked()
    {
        // 팝업 닫기만
    }
}
