using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset_System : MonoBehaviour
{
    // Character 스크립트가 부착된 오브젝트를 할당 (0Tier 스킬 배열 포함)
    public Character character;
    public SaveSystem save;

    [Header("UI")]
    [Tooltip("확인용 팝업(ResetConfirmPopup) 프리팹 또는 씬 배치 오브젝트 참조")]
    public ResetConfirmPopup resetConfirmPopup;

    [Tooltip("재시작할 초기 씬 이름 (예: Loby). 비우면 BuildSettings의 0번 씬로 이동")]
    public string bootSceneName = "Loby";

    // 각 스킬의 데미지를 저장할 배열 (0티어 전용 초기값)
    [Tooltip("0티어 스킬의 초기 데미지 값. tier0Skills 길이와 맞춰주세요.")]
    public int[] initialDamages = { 10, 10, 5, 5 };

    // 공통 기본 요구량(레벨업 필요 수량). 명시 없으면 1로 초기화
    [Tooltip("레벨업 필요 수량 초기값(모든 티어 공통). 보통 1")]
    public int defaultNeedLevelUp = 1;

    /// <summary>
    /// 0Tier 스킬의 레벨/데미지/요구량 초기화
    /// </summary>
    public void SkillReset()
    {
        if (character == null)
        {
            Debug.LogError("[Reset] character가 할당되지 않았습니다.");
            return;
        }

        if (character.tier0Skills == null)
        {
            Debug.LogError("[Reset] character.tier0Skills가 null 입니다.");
            return;
        }

        if (initialDamages == null || initialDamages.Length != character.tier0Skills.Length)
        {
            Debug.LogWarning($"[Reset] initialDamages 길이({initialDamages?.Length ?? -1})가 tier0Skills 길이({character.tier0Skills.Length})와 다릅니다. 가능한 항목까지만 초기화합니다.");
        }

        for (int i = 0; i < character.tier0Skills.Length; i++)
        {
            var sd = character.tier0Skills[i];
            if (sd == null) continue;

            sd.level = 1;
            // initialDamages 길이 체크 후 적용
            if (initialDamages != null && i < initialDamages.Length)
                sd.damage = initialDamages[i];

            sd.NeedLevelUP_Gold = Mathf.Max(1, defaultNeedLevelUp);
        }

        Debug.Log("[Reset] 모든 0티어 스킬이 초기화되었습니다.");
        if (save) save.SaveGameData();
    }

    /// <summary>
    /// 캐릭터 주요 스탯 및 보유 스킬 배열 초기화
    /// </summary>
    public void CharacterReset()
    {
        if (character == null)
        {
            Debug.LogError("[Reset] character가 할당되지 않았습니다.");
            return;
        }

        character.Character_Level = 1;
        character.Character_EXP = 0;
        character.Character_NextEXP = 50;
        character.Character_Mana = 2.0f;
        character.WallHP = 30;
        character.Character_Gold = 0;
        character.Character_Stat = 0;
        character.Character_Int = 10;
        character.Character_Int_Level = 1;
        character.Character_SkillDust = 0;

        // === 보유 스킬 배열 안전 초기화 ===
        int totalSkillCount = GetTotalSkillCount();
        if (totalSkillCount <= 0)
        {
            // 최소 1칸은 만들어 두되 0으로 채움
            character.Character_HaveSkill = new int[1];
            Debug.LogWarning("[Reset] 스킬 총합이 0으로 계산되었습니다. Character_HaveSkill을 길이 1, 값 0으로 초기화했습니다.");
        }
        else
        {
            character.Character_HaveSkill = new int[totalSkillCount]; // 자동으로 모두 0으로 초기화
        }

        Debug.Log($"[Reset] 캐릭터 설정 초기화 완료. Character_HaveSkill 길이={character.Character_HaveSkill.Length}");
        if (save) save.SaveGameData();
    }

    /// <summary>
    /// 1,2티어 스킬의 'isKnow'를 false로 초기화하고, 추가로
    /// 1,2티어 스킬의 레벨/요구량(NeedLevelUP_Gold)도 초기화합니다.
    /// </summary>
    public void SkillknowReset()
    {
        if (character == null)
        {
            Debug.LogError("[Reset] character가 할당되지 않았습니다.");
            return;
        }

        // --- 1티어 ---
        if (character.tier1Skills != null)
        {
            for (int i = 0; i < character.tier1Skills.Length; i++)
            {
                var sd = character.tier1Skills[i];
                if (sd == null) continue;

                sd.isKnow = false;
                sd.level = 1;                                    // 레벨 초기화
                sd.NeedLevelUP_Gold = Mathf.Max(1, defaultNeedLevelUp); // 요구량 초기화
            }
        }

        // --- 2티어 ---
        if (character.tier2Skills != null)
        {
            for (int i = 0; i < character.tier2Skills.Length; i++)
            {
                var sd = character.tier2Skills[i];
                if (sd == null) continue;

                sd.isKnow = false;
                sd.level = 1;                                    // 레벨 초기화
                sd.NeedLevelUP_Gold = Mathf.Max(1, defaultNeedLevelUp); // 요구량 초기화
            }
        }

        Debug.Log("[Reset] 1·2티어 스킬의 isKnow/레벨/요구량 초기화 완료.");
        if (save) save.SaveGameData();
    }

    // =========================
    // 내부 유틸
    // =========================

    private int GetTotalSkillCount()
    {
        int c0 = character.tier0Skills != null ? character.tier0Skills.Length : 0;
        int c1 = character.tier1Skills != null ? character.tier1Skills.Length : 0;
        int c2 = character.tier2Skills != null ? character.tier2Skills.Length : 0;
        return c0 + c1 + c2;
    }

    /// <summary>
    /// 스토리 진행을 0-1로 초기화.
    /// (캐릭터/스킬 초기화와 별개로 호출 가능)
    /// </summary>
    public void StoryProgressReset()
    {
        if (StoryModeManager.Instance != null)
        {
            StoryModeManager.Instance.lastCheckpointStageId = "0-0";
            StoryModeManager.Instance.maxClearedStageId = "0-0";
            Debug.Log("[Reset] 스토리 진행을 0-1로 초기화했습니다.");
        }
        else
        {
            Debug.LogWarning("[Reset] StoryModeManager 인스턴스를 찾지 못했습니다. Loby에서 시작했는지 확인하세요.");
        }

        if (save) save.SaveGameData();
    }

    /// <summary>
    /// 퀘스트 진행도 초기화
    /// </summary>
    public void QuestReset()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetAllProgress();
            Debug.Log("[Reset] 퀘스트 진행 카운트 전부 초기화");
        }
        else
        {
            Debug.LogWarning("[Reset] QuestManager_SO 인스턴스가 없습니다. 씬에 배치/DB 할당 확인");
        }

        if (save) save.SaveGameData();
    }

    public void ResetAllTutorials()
    {
        CharacterTutorialBridge.ResetAll();
        // 저장 데이터도 즉시 반영하고 싶다면 SaveGameData() 호출
    }

    public void SoundReset(bool alsoClearPrefs = true)
    {
        if(SoundSettingsManager.Instance != null)
        {
            var sm = SoundSettingsManager.Instance;

            sm.SetMasterSound(1f);
            sm.SetBGM(1f);
            sm.SetSFX(1f);
            sm.SetSkill(1f);

            sm.Save();
            sm.ApplyAllToMixer();
        }

        if(alsoClearPrefs)
        {
            PlayerPrefs.DeleteKey("Sound.Master");
            PlayerPrefs.DeleteKey("Sound.BGM");
            PlayerPrefs.DeleteKey("Sound.SFX");
            PlayerPrefs.DeleteKey("sound.SkillSFX");
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 로비의 "초기화" 버튼에 연결: 팝업을 띄운다.
    /// </summary>
    public void OnClickOpenResetPopup()
    {
        if (resetConfirmPopup == null)
        {
            Debug.LogError("[Reset] resetConfirmPopup이 할당되어 있지 않습니다.");
            return;
        }

        resetConfirmPopup.Show(
            onConfirm: DoFullResetAndRestart,  // 확인 -> 초기화 & 재시작
            onCancel: () => Debug.Log("[Reset] 초기화가 취소되었습니다.")
        );
    }

    /// <summary>
    /// 실제 전체 초기화 + 저장파일 삭제 + 게임 재시작
    /// </summary>
    private void DoFullResetAndRestart()
    {
        // 세이브 파일 삭제
        DeleteSaveFile();

        // 게임 데이터 초기화(순서는 필요에 따라 조정 가능)
        CharacterReset();
        SkillReset();
        SkillknowReset();
        StoryProgressReset();
        QuestReset();
        ResetAllTutorials();
        SoundReset(alsoClearPrefs: true);

        if (save) save.SaveGameData(); // 초기화된 상태로 즉시 저장(선택)

        // 게임 재시작 (모바일 포함 공통)
        RestartGame();
    }

    /// <summary>
    /// SaveSystem이 사용하는 save.json 파일 삭제
    /// </summary>
    private void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.json");
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[Reset] 저장 파일 삭제 완료: {path}");
            }
            else
            {
                Debug.Log($"[Reset] 저장 파일이 없습니다: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Reset] 저장 파일 삭제 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 초기 부트 씬으로 재시작. bootSceneName이 비어있으면 BuildSettings의 0번 씬으로.
    /// </summary>
    private void RestartGame()
    {
        // DontDestroyOnLoad 오브젝트로 인해 상태가 남는 경우가 있다면
        // 여기서 싱글톤 해제/클리어 코드를 추가하세요(예: AudioManager.Instance?.Reset() 등).

        if (!string.IsNullOrEmpty(bootSceneName))
        {
            SceneManager.LoadScene(bootSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        // 메모리 정리(선택)
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
