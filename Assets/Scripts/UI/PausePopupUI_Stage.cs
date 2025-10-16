using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;

[RequireComponent(typeof(CanvasGroup))]
public class PausePopupUI_Stage : PausePopupUI
{
    [Header("Extra Wiring (Stage UI)")]
    [SerializeField] private Button giveUpButton;   // 포기하기
    [SerializeField] private Text scoreText;        // "점수: 000"
    [SerializeField] private Text infoText;         // "wave: 0" or "stage: 2-3"

    [Header("Game Refs")]
    [SerializeField] private Score_System scoreSystem;             // 점수
    [SerializeField] private Monster_Spawn monsterSpawn;           // 웨이브(일반)
    [SerializeField] private StoryModeManager storyModeManager;    // 스테이지(스토리)
    [SerializeField] private Wall_System_Base wallSystem;          // HP 소유(일반/스토리 공통)

    protected override void Awake()
    {
        base.Awake();
        if (giveUpButton) giveUpButton.onClick.AddListener(OnClickGiveUp);
    }

    // 팝업 열 때 텍스트 갱신 후 부모 Open() 호출
    public override void Open()
    {
        RefreshTexts();
        base.Open();
    }

    private void OnClickGiveUp()
    {
        // 퍼즈 UI 종료(이어하기와 동일: timeScale 복구 + 닫기)
        base.OnClickResume();

        // HP -1로 설정 → Wall_System_Base의 GameOver() 로직 실행
        var wall = wallSystem != null ? wallSystem : FindObjectOfType<Wall_System_Base>();
        if (wall != null)
        {
            wall.currentHealth = -1; // Update()에서 <=0 감지 → GameOver()
        }
        else
        {
            Debug.LogWarning("[PausePopupUI_Stage] Wall_System_Base를 찾지 못했습니다. 'wallSystem' 참조를 인스펙터에 연결하세요.");
        }

        var sm = StoryModeManager.Instance;
        if (sm != null && sm.isStoryRun)
            sm.MarkAborted();
    }

    private void RefreshTexts()
    {
        // 점수: "점수: 000"
        if (scoreText)
        {
            int score = scoreSystem ? scoreSystem.score : 0;
            scoreText.text = $"점수: {score}";
        }

        // 씬 이름으로 모드 판별
        string scene = SceneManager.GetActiveScene().name;
        bool isStory = scene == "StoryModeScene";

        if (infoText)
        {
            if (isStory)
            {
                string stage = "?";
                if (storyModeManager = FindObjectOfType<StoryModeManager>(true))
                {
                    // 프로젝트 구조에 맞춰 stageId(소문자) 먼저
                    if (storyModeManager.currentStage != null && !string.IsNullOrEmpty(storyModeManager.currentStage.stageId))
                        stage = storyModeManager.currentStage.stageId;
                    else if (!string.IsNullOrEmpty(storyModeManager.lastCheckpointStageId))
                        stage = storyModeManager.lastCheckpointStageId;
                }
                infoText.text = $"stage: {stage}";
            }
            else // 일반 모드 (SampleScene 등)
            {
                int wave = 0;
                if (monsterSpawn != null)
                {
                    // currentWave 필드 우선, 없으면 CurrentWave 프로퍼티도 시도
                    var t = monsterSpawn.GetType();
                    var f = t.GetField("currentWave", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) wave = (int)f.GetValue(monsterSpawn);
                    var p = t.GetProperty("CurrentWave", BindingFlags.Public | BindingFlags.Instance);
                    if (p != null) wave = (int)p.GetValue(monsterSpawn, null);
                }
                infoText.text = $"wave: {wave}";
            }
        }
    }
}
