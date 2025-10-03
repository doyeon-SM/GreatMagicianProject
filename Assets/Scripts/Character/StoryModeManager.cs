using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryModeManager : MonoBehaviour
{
    public static StoryModeManager Instance { get; private set; }

    [Header("레퍼런스")]
    public Monster_Spawn monsterSpawn;
    public Score_System scoreSystem;
    public SceneLoader sceneLoader;

    [Header("Stages")]
    public StoryStageAssetResolver resolver;

    [Header("진행 상태")]
    public bool isStoryRun = false;
    public StoryStageAsset currentStage;

    // 간단 진행도(마지막 도전지점). 프로젝트 SaveSystem와 연동 권장
    public string lastCheckpointStageId = "0-1"; // 초기 기본값

    [Header("Result UI")]
    public GameObject gameResultUIPrefab;

    private const string StorySceneName = "StoryModeScene";
    private bool _pendingNext;
    private StoryStageAsset _pendingStage;
    private bool _isStartingStage = false;
    private string _pendingNextStageId = null;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); LoadProgress(); }
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        TryAutoWire();
        StartCoroutine(EnsureLoadedStageId());
    }
    private IEnumerator EnsureLoadedStageId()
    {
        // SaveSystem.Start()의 LoadGameData가 먼저 실행되도록 한 프레임 대기
        yield return null;

        if (string.IsNullOrEmpty(lastCheckpointStageId))
            lastCheckpointStageId = "0-1";
    }
    private void TryAutoWire()
    {
        if (!monsterSpawn) monsterSpawn = FindObjectOfType<Monster_Spawn>(true);
        if (!scoreSystem) scoreSystem = FindObjectOfType<Score_System>(true);
        if (!sceneLoader) sceneLoader = FindObjectOfType<SceneLoader>(true);
    }

    // ===== 외부에서 호출: 스토리 모드 시작 =====
    public void StartStoryStage(StoryStageAsset stage)
    {
        // 중복/재진입 가드
        if (_isStartingStage)
        {
            Debug.LogWarning("[StoryMode] StartStoryStage ignored: already starting.");
            return;
        }
        _isStartingStage = true;

        TryAutoWire();
        if (!monsterSpawn || !scoreSystem || stage == null)
        {
            _isStartingStage = false;
            Debug.LogError("[StoryMode] 시작 실패: 레퍼런스 또는 StageAsset 누락");
            return;
        }

        // === 리셋들 ===
        var wall = FindObjectOfType<Wall_System_Base>(true);
        if (wall) wall.ResetForNextStage();

        var mana = FindObjectOfType<Mana_Base>(true);
        if (mana != null) Mana_Base.currentMana = 0f;

        var underUI = UnderUI_System_Base.Instance ?? FindObjectOfType<UnderUI_System_Base>(true);
        if (underUI != null) underUI.ResetForNextStage();

        isStoryRun = true;
        currentStage = stage;

        monsterSpawn.EnableScriptedWaves(stage.scriptedWaves, stage.waveDuration);

        scoreSystem.score = 0;
        Debug.Log($"[StoryMode] Start {stage.stageId} (waves={stage.waveCount})");

        // 시작 완료 후 플래그 해제
        _isStartingStage = false;
    }

    public void StartFromLastCheckpoint(StoryStageAssetResolver resolver)
    {
        // 대기 중(next 큐)이라면 부트스트랩 호출 무시
        if (_pendingNext)
        {
            Debug.Log("[StoryMode] StartFromLastCheckpoint ignored: pending queued next stage.");
            return;
        }
        // 이미 진행 중이면 무시
        if (_isStartingStage || (isStoryRun && currentStage != null))
        {
            Debug.Log("[StoryMode] StartFromLastCheckpoint ignored: stage already running/starting.");
            return;
        }

        var stage = resolver != null ? resolver.Resolve(lastCheckpointStageId) : null;
        if (stage == null)
        {
            Debug.LogError($"[StoryMode] {lastCheckpointStageId} 스테이지를 찾을 수 없습니다.");
            return;
        }
        StartStoryStage(stage);
    }



    // ===== Monster_Spawn가 최종 웨이브 종료 후 호출 =====
    public void OnAllScriptedWavesFinished()
    {
        if (!isStoryRun || currentStage == null) return;

        // 잔몹 소탕 감시 → 모두 사라지면 클리어 처리
        StartCoroutine(WaitAndClearRoutine());
    }

    private IEnumerator WaitAndClearRoutine()
    {
        // 잔몹 정리 대기
        while (GameObject.FindGameObjectsWithTag("Monster").Length > 0)
            yield return new WaitForSeconds(0.25f);

        HandleStageClear();
    }

    private void HandleStageClear()
    {
        if (!scoreSystem || currentStage == null) return;

        scoreSystem.score = currentStage.stageScore;

        if (currentStage.bonusGold > 0)
            scoreSystem.character.Character_Gold += currentStage.bonusGold;
        if (currentStage.bonusExp > 0)
            scoreSystem.character.CharacterLevelUP(currentStage.bonusExp);

        // 결과 기록 초기화 후 확정보상 먼저
        if (scoreSystem.LastAwarded == null) scoreSystem.LastAwarded = new List<Score_System.AwardedSkillInfo>();
        scoreSystem.LastAwarded.Clear();

        foreach (var idx in currentStage.guaranteedSkillIndices)
            scoreSystem.AddGuaranteedSkillByIndex(idx);

        // 다음 스테이지 "후보"만 계산해서 보관. 체크포인트는 아직 갱신하지 않음!
        _pendingNextStageId = GetNextStageIdByOrder(currentStage.stageId);

        ShowResultAndNext();
    }


    private void ShowResultAndNext()
    {
        // 결과창 찾기/생성 (기존 그대로)
        var resultUI = FindObjectOfType<GameResultUI>(true);
        if (!resultUI)
        {
            GameObject prefab = null;
            var wall = FindObjectOfType<Wall_System_Base>(true);
            if (wall && wall.gameResultUIPrefab) prefab = wall.gameResultUIPrefab;
            else if (gameResultUIPrefab) prefab = gameResultUIPrefab;

            if (prefab == null)
            {
                Debug.LogWarning("[StoryMode] GameResultUI 프리팹을 찾을 수 없습니다. 로비로 복귀합니다.");
                if (sceneLoader) sceneLoader.LoadLobyScene();
                return;
            }

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                Debug.LogWarning("[StoryMode] Canvas를 찾을 수 없습니다. 로비로 복귀합니다.");
                if (sceneLoader) sceneLoader.LoadLobyScene();
                return;
            }

            var uiInstance = Instantiate(prefab, canvasGO.transform);
            resultUI = uiInstance.GetComponent<GameResultUI>();
            if (!resultUI)
            {
                Debug.LogError("[StoryMode] 생성된 프리팹에 GameResultUI 컴포넌트가 없습니다.");
                if (sceneLoader) sceneLoader.LoadLobyScene();
                return;
            }
        }

        if (!resultUI.scoreSystem) resultUI.scoreSystem = scoreSystem;
        if (!resultUI.sceneLoader) resultUI.sceneLoader = sceneLoader;
        if (!resultUI.monsterSpawn) resultUI.monsterSpawn = monsterSpawn;

        Time.timeScale = 0f;
        resultUI.gameObject.SetActive(true);
        resultUI.ShowResult();
    }




    // Resolver 순서 기반 Next 계산
    private string GetNextStageIdByOrder(string cur)
    {
        if (resolver == null || resolver.stages == null || resolver.stages.Count == 0)
            return GetNextStageId_FallbackNumeric(cur);

        int idx = resolver.stages.FindIndex(s => s != null && s.stageId == cur);
        if (idx < 0) return GetNextStageId_FallbackNumeric(cur);

        if (idx + 1 < resolver.stages.Count)
            return resolver.stages[idx + 1].stageId;

        return cur; // 마지막이면 그대로
    }

    // 기존 숫자 증가식 폴백(예비용)
    private string GetNextStageId_FallbackNumeric(string cur)
    {
        var p = cur.Split('-');
        if (p.Length != 2) return cur;
        if (int.TryParse(p[0], out int a) && int.TryParse(p[1], out int b))
            return $"{a}-{b + 1}";
        return cur;
    }

    private void SaveCheckpoint()
    {
        // 프로젝트 SaveSystem에 "lastCheckpointStageId" 같은 필드 추가 권장
        // 여기서는 존재 가정 없이 로그만
        Debug.Log($"[StoryMode] Checkpoint saved: {lastCheckpointStageId}");
    }

    public void QueueStartNextStage(StoryStageAsset next)
    {
        _pendingNextStageId = null; // 메뉴에서 바로 시작할 땐 이전 보류값 무효화
        if (next == null) { Debug.LogWarning("[StoryMode] QueueStartNextStage: next=null"); return; }
        StartCoroutine(StartNextStageFlow(next));
    }

    private IEnumerator StartNextStageFlow(StoryStageAsset next)
    {
        // 결과창에서 TimeScale을 1로 풀었다고 가정. 한 프레임 양보
        yield return null;

        var cur = SceneManager.GetActiveScene().name;
        if (cur != StorySceneName)
        {
            _pendingNext = true;
            _pendingStage = next;

            SceneManager.sceneLoaded += OnSceneLoaded_RunPendingNext;
            SceneManager.LoadScene(StorySceneName);
            yield break; // 씬 바뀌면 여기서 종료, 로드 콜백에서 이어서 시작
        }

        // 이미 StoryModeScene이면 바로 시작
        StartStoryStage(next);
    }

    private void OnSceneLoaded_RunPendingNext(Scene scene, LoadSceneMode mode)
    {
        if (!_pendingNext) return;
        if (scene.name != StorySceneName) return;

        SceneManager.sceneLoaded -= OnSceneLoaded_RunPendingNext;

        var stage = _pendingStage;
        _pendingNext = false;
        _pendingStage = null;

        // 씬 로드 완료 프레임 한 번 양보 (모든 오브젝트 Awake/Start 보장)
        StartCoroutine(_StartAfterOneFrame(stage));
    }

    private IEnumerator _StartAfterOneFrame(StoryStageAsset stage)
    {
        yield return null;
        StartStoryStage(stage);
    }
    public bool TryPeekPendingNext(out StoryStageAsset next)
    {
        next = null;
        if (string.IsNullOrEmpty(_pendingNextStageId) || resolver == null) return false;
        next = resolver.Resolve(_pendingNextStageId);
        return next != null;
    }
    public void ConfirmAndStartNextStage()
    {
        if (string.IsNullOrEmpty(_pendingNextStageId) || resolver == null)
        {
            Debug.LogWarning("[StoryMode] ConfirmAndStartNextStage: no pending next.");
            return;
        }

        var next = resolver.Resolve(_pendingNextStageId);
        if (next == null)
        {
            Debug.LogWarning($"[StoryMode] Pending next '{_pendingNextStageId}' not found.");
            return;
        }

        // 여기서 '진짜'로 체크포인트 갱신 후 저장
        lastCheckpointStageId = _pendingNextStageId;
        SaveCheckpoint();

        _pendingNextStageId = null; // 소모

        // 다음 스테이지 시작(씬 보장 로직 포함)
        QueueStartNextStage(next);
    }
    public void CommitStageClear()
    {
        if (currentStage == null || string.IsNullOrEmpty(currentStage.stageId))
            return;

        // 현재가 더 뒤 스테이지면 갱신
        if (CompareStageId(currentStage.stageId, lastCheckpointStageId) > 0)
        {
            lastCheckpointStageId = currentStage.stageId;
            PersistProgress(); // 즉시 저장
            Debug.Log($"[Story] Cleared {lastCheckpointStageId} (progress saved)");
        }
    }
    public int CompareStageId(string a, string b)
    {
        (int am, int asub) = Parse(a);
        (int bm, int bsub) = Parse(b);
        if (am != bm) return am.CompareTo(bm);
        return asub.CompareTo(bsub);

        (int, int) Parse(string id)
        {
            if (string.IsNullOrEmpty(id)) return (0, 0);
            var parts = id.Split('-');
            if (parts.Length != 2) return (0, 0);
            int.TryParse(parts[0], out int major);
            int.TryParse(parts[1], out int minor);
            return (major, minor);
        }
    }
    // 진행도 저장/로드
    public void PersistProgress()
    {
        PlayerPrefs.SetString("LastCheckpointStageId", lastCheckpointStageId);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey("LastCheckpointStageId"))
            lastCheckpointStageId = PlayerPrefs.GetString("LastCheckpointStageId", lastCheckpointStageId);
    }
}
