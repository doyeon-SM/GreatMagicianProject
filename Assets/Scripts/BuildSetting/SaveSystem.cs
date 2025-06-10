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
    public void SaveGameData()
    {
        CharacterSaveData data = new CharacterSaveData();

        data.Player_level = character.Character_Level;
        data.Player_currentEXP = character.Character_EXP;
        data.Player_HP = character.WallHP;
        data.Player_mana = character.Character_Mana;
        data.Player_gold = character.Character_Gold;
        data.Player_stat = character.Character_Stat;
        data.Player_int = character.Character_Int;
        data.Player_int_level = character.Character_Int_Level;

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
            //skill.UpdateDamage(); // 조합 스킬인 경우 재계산 필요 시
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
