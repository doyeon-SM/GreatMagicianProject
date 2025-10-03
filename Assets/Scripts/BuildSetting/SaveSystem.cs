using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public Character character; // 씬에 있는 캐릭터 참조

    private string savePath;
    private bool _wasFirstRun = false;

    private void Start()
    {
        LoadGameData();  // 캐릭터와 스킬 배열이 초기화된 이후에 불러오기

        // 첫 실행이면 튜토리얼 예약 트리거
        if (_wasFirstRun)
            StartCoroutine(CoTriggerFirstStartTutorialNextFrame());
    }
    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 어디서든 쉽게 저장을 호출하게 하는 정적 헬퍼
    public static void SaveGame()
    {
        if (Instance != null)
            Instance.SaveGameData();
        else
            Debug.LogWarning("[SaveSystem] Instance가 없습니다. 씬에 SaveSystem가 배치되어 있는지 확인하세요.");
    }

    public void SaveGameData()
    {
        if (character == null)
        {
            Debug.LogWarning("[SaveSystem] Character가 연결되지 않았습니다.");
            return;
        }

        CharacterSaveData data = new CharacterSaveData();

        data.Player_level = character.Character_Level;
        data.Player_currentEXP = character.Character_EXP;
        data.Player_HP = character.WallHP;
        data.Player_mana = character.Character_Mana;
        data.Player_gold = character.Character_Gold;
        data.Player_stat = character.Character_Stat;
        data.Player_int = character.Character_Int;
        data.Player_int_level = character.Character_Int_Level;
        data.Player_dust = character.Character_SkillDust;

        data.learnedSkills = new List<SkillSaveData>();
        foreach (var skill in character.tier0Skills)
        {
            data.learnedSkills.Add(new SkillSaveData
            {
                skillName = skill.skillName,
                damage = skill.damage,
                level = skill.level,
                Need_gold = skill.NeedLevelUP_Gold,
                Player_know = skill.isKnow
            }) ;
        }
        foreach (var skill in character.tier1Skills)
        {
            data.learnedSkills.Add(new SkillSaveData
            {
                skillName = skill.skillName,
                damage = skill.damage,
                level = skill.level,
                Need_gold = skill.NeedLevelUP_Gold,
                Player_know = skill.isKnow
            });
        }
        foreach (var skill in character.tier2Skills)
        {
            data.learnedSkills.Add(new SkillSaveData
            {
                skillName = skill.skillName,
                damage = skill.damage,
                level = skill.level,
                Need_gold = skill.NeedLevelUP_Gold,
                Player_know = skill.isKnow
            });
        }
        // Character_HaveSkill 저장
        int totalSkillCount =
            (character.tier0Skills?.Length ?? 0) +
            (character.tier1Skills?.Length ?? 0) +
            (character.tier2Skills?.Length ?? 0);

        if (character.Character_HaveSkill == null || character.Character_HaveSkill.Length < totalSkillCount)
        {
            // 길이가 부족하면 0으로 채워 저장
            data.Player_haveSkills = new int[totalSkillCount];
            if (character.Character_HaveSkill != null)
            {
                System.Array.Copy(character.Character_HaveSkill, data.Player_haveSkills,
                    Mathf.Min(character.Character_HaveSkill.Length, data.Player_haveSkills.Length));
            }
        }
        else
        {
            // 길이가 충분하면 그대로 복사하여 저장
            data.Player_haveSkills = new int[totalSkillCount];
            System.Array.Copy(character.Character_HaveSkill, data.Player_haveSkills, totalSkillCount);
        }
        // 스토리 진행 저장
        data.story_lastStageId = StoryModeManager.Instance != null
            ? StoryModeManager.Instance.lastCheckpointStageId
            : "0-1";

        //퀘스트 저장
        if (QuestManager.Instance != null)
            data.questSO = QuestManager.Instance.ExportSave();

        // 튜토리얼 키 저장
        data.SeenTutorialKeys.Clear();
        if (TutorialManager.Instance != null && TutorialManager.Instance.database != null)
        {
            data.SeenTutorialKeys = TutorialManager.Instance.database.GetClearedKeys();
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("저장 완료: " + savePath);
    }

    public void LoadGameData()
    {
        bool hasSaveFile = File.Exists(savePath);

        CharacterSaveData data = null;

        if (hasSaveFile)
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<CharacterSaveData>(json);
        }
        else
        {
            Debug.LogWarning("세이브 파일이 없습니다. 기본값으로 시작합니다.");
            _wasFirstRun = true;                 // 첫 실행 플래그 세움
            data = new CharacterSaveData();      // 기본 데이터(빈 값) 생성
        }


        // 캐릭터 정보 복원
        character.Character_Level = data.Player_level;
        character.Character_EXP = data.Player_currentEXP;
        character.WallHP = data.Player_HP;
        character.Character_Mana = data.Player_mana;
        character.Character_Gold = data.Player_gold;
        character.Character_Stat = data.Player_stat;
        character.Character_Int = data.Player_int;
        character.Character_Int_Level = data.Player_int_level;
        character.Character_SkillDust = data.Player_dust;

        // 스킬 정보 복원
        int skillIndex = 0;
        foreach (var skill in character.tier0Skills)
        {
            if (skillIndex >= data.learnedSkills.Count) break;
            ApplySkillData(skill, data.learnedSkills[skillIndex++]);
        }
        foreach (var skill in character.tier1Skills)
        {
            if (skillIndex >= data.learnedSkills.Count) break;
            ApplySkillData(skill, data.learnedSkills[skillIndex++]);
        }
        foreach (var skill in character.tier2Skills)
        {
            if (skillIndex >= data.learnedSkills.Count) break;
            ApplySkillData(skill, data.learnedSkills[skillIndex++]);
        }
        // Character_HaveSkill 복원
        int expectedLen =
            (character.tier0Skills?.Length ?? 0) +
            (character.tier1Skills?.Length ?? 0) +
            (character.tier2Skills?.Length ?? 0);

        if (data.Player_haveSkills != null && data.Player_haveSkills.Length > 0)
        {
            // 세이브 데이터 길이와 현재 기대 길이가 다를 수 있으니 안전하게 복사
            character.Character_HaveSkill = new int[expectedLen];
            int copyLen = Mathf.Min(expectedLen, data.Player_haveSkills.Length);
            System.Array.Copy(data.Player_haveSkills, character.Character_HaveSkill, copyLen);

            // 남는 구간이 있으면 0으로 초기화(신규 스킬이 늘어난 경우 등)
            for (int i = copyLen; i < expectedLen; i++)
                character.Character_HaveSkill[i] = 0;
        }
        else
        {
            // 세이브에 없으면(구버전) 현재 길이에 맞춰 0으로 초기화
            character.Character_HaveSkill = new int[expectedLen];
        }
        // 스토리 진행 복원 (필드가 없거나 빈 경우 기본값 "0-1")
        string restoredStageId = string.IsNullOrEmpty(data.story_lastStageId) ? "0-1" : data.story_lastStageId;
        if (StoryModeManager.Instance != null)
        {
            StoryModeManager.Instance.lastCheckpointStageId = restoredStageId;
            Debug.Log($"[SaveSystem] 스토리 진행 복원: {restoredStageId}");
        }
        else
        {
            Debug.Log($"[SaveSystem] StoryModeManager 미존재. 진행도({restoredStageId})는 매니저 생성 후 반영 필요.");
        }

        //퀘스트 진행 복원
        if (QuestManager.Instance != null)
            QuestManager.Instance.ImportSave(data.questSO);
        // 튜토리얼 키 적용
        if (TutorialManager.Instance != null && TutorialManager.Instance.database != null)
            TutorialManager.Instance.database.ApplyClearedKeys(data.SeenTutorialKeys);
        else
            StartCoroutine(CoApplyTutorialKeysWhenReady(data.SeenTutorialKeys));

        Debug.Log("불러오기 완료");
        // 첫 실행이면 초기 파일을 즉시 저장해 두면 다음부터 hasSaveFile=true로 안전
        if (_wasFirstRun)
            SaveGameData();

    }

    // 헬퍼 함수: SkillSaveData → Skill_Data에 적용
    private void ApplySkillData(Skill_Data skill, SkillSaveData savedData)
    {
        if (skill.skillName == savedData.skillName)
        {
            skill.damage = savedData.damage;
            skill.level = savedData.level;
            skill.NeedLevelUP_Gold = savedData.Need_gold;
            skill.isKnow = savedData.Player_know;
            skill.UpdateDamage(); // 조합 스킬인 경우 재계산 필요 시
        }
        else
        {
            Debug.LogWarning($"스킬 이름 불일치: 저장된 스킬 {savedData.skillName} ↔ 현재 {skill.skillName}");
        }
    }
    private void OnApplicationQuit()
    {
        SaveGameData(); // 게임이 꺼질 때 자동 저장
    }
    private IEnumerator CoApplyTutorialKeysWhenReady(System.Collections.Generic.List<string> keys)
    {
        // TutorialManager가 살아날 때까지 대기 (씬 전환 등 고려)
        while (TutorialManager.Instance == null || TutorialManager.Instance.database == null)
            yield return null;

        TutorialManager.Instance.database.ApplyClearedKeys(keys);
    }

    // 첫 시작 안내문 
    private IEnumerator CoTriggerFirstStartTutorialNextFrame()
    {
        // TutorialManager 준비 + 한 프레임 대기 후 호출 (UI/Canvas 준비 시간 확보)
        while (TutorialManager.Instance == null || TutorialManager.Instance.database == null)
            yield return null;
        yield return null;

        TutorialManager.Instance.TryTrigger("FirstStart");
    }
}
