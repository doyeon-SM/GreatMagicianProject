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

    [Header("진행 상태")]
    public bool isStoryRun = false;
    public StoryStageAsset currentStage;

    // 간단 진행도(마지막 도전지점). 프로젝트 SaveSystem와 연동 권장
    public string lastCheckpointStageId = "1-1"; // 초기 기본값

    [Header("Result UI")]
    public GameObject gameResultUIPrefab;

    private const string StorySceneName = "StoryModeScene";
    private bool _pendingNext;
    private StoryStageAsset _pendingStage;
    private bool _isStartingStage = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
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
            lastCheckpointStageId = "1-1";
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

        // 결과 기록 초기화 후 보장 보상 먼저 누적
        if (scoreSystem.LastAwarded == null) scoreSystem.LastAwarded = new List<Score_System.AwardedSkillInfo>();
        scoreSystem.LastAwarded.Clear();

        foreach (var idx in currentStage.guaranteedSkillIndices)
            scoreSystem.AddGuaranteedSkillByIndex(idx);

        // 다음 도전 지점 갱신을 먼저 하고
        lastCheckpointStageId = GetNextStageId(currentStage.stageId);
        SaveCheckpoint();

        ShowResultAndNext();  // ShowResult에서 ResultScore() 호출 → 랜덤 보상 '추가'
    }


    private void ShowResultAndNext()
    {
        // 먼저 다음 도전 지점으로 갱신 (버튼 노출 판단이 '다음' 기준으로 되도록)
        lastCheckpointStageId = GetNextStageId(currentStage.stageId);
        SaveCheckpoint();

        // 결과창 찾기/생성
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

        // 주입
        if (!resultUI.scoreSystem) resultUI.scoreSystem = scoreSystem;
        if (!resultUI.sceneLoader) resultUI.sceneLoader = sceneLoader;
        if (!resultUI.monsterSpawn) resultUI.monsterSpawn = monsterSpawn;

        // 결과창 표시 전에 일시정지
        Time.timeScale = 0f;

        resultUI.gameObject.SetActive(true);
        resultUI.ShowResult();
    }



    // "1-1" → "1-2" 같은 증가. 실제 규칙은 프로젝트에 맞게 교체
    private string GetNextStageId(string cur)
    {
        // "A-B" 포맷 가정
        var p = cur.Split('-');
        if (p.Length != 2) return cur;

        if (int.TryParse(p[0], out int a) && int.TryParse(p[1], out int b))
        {
            return $"{a}-{b + 1}";
        }
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
}
