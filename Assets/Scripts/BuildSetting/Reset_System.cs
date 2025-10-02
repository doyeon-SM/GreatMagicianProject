using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reset_System : MonoBehaviour
{
    // Character 스크립트가 부착된 오브젝트를 할당 (0Tier 스킬 배열 포함)
    public Character character;
    public SaveSystem save;

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
    /// 스토리 진행을 1-1로 초기화.
    /// (캐릭터/스킬 초기화와 별개로 호출 가능)
    /// </summary>
    public void StoryProgressReset()
    {
        if (StoryModeManager.Instance != null)
        {
            StoryModeManager.Instance.lastCheckpointStageId = "1-1";
            Debug.Log("[Reset] 스토리 진행을 1-1로 초기화했습니다.");
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
}
