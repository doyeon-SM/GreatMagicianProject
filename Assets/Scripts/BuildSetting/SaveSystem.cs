using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public Character character; // 씬에 있는 캐릭터 참조

    private string savePath;
    private void Start()
    {
        LoadGameData();  // 캐릭터와 스킬 배열이 초기화된 이후에 불러오기
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

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("저장 완료: " + savePath);
    }

    public void LoadGameData()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("세이브 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(savePath);
        CharacterSaveData data = JsonUtility.FromJson<CharacterSaveData>(json);

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

        Debug.Log("불러오기 완료");
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
}
