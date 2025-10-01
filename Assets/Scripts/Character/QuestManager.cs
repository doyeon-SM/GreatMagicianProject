using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("DB / 캐릭터")]
    public QuestDatabase database;
    public Character character;

    // 소비형 진행 카운트(퀘스트별 저장)
    [SerializeField] private Dictionary<string, int> _savedCounts = new(); // questId -> 카운트

    public event Action OnProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (character == null) character = FindObjectOfType<Character>();
        EnsureAllQuestsPrepared();
    }

    private void EnsureAllQuestsPrepared()
    {
        if (_savedCounts == null) _savedCounts = new Dictionary<string, int>();
        if (database == null || database.quests == null) return;
        foreach (var q in database.quests)
        {
            if (q == null) continue;
            if (!_savedCounts.ContainsKey(q.questId))
                _savedCounts[q.questId] = 0;
        }
    }

    // === 조회 유틸 ===
    public QuestDefinition GetDefinition(string questId)
    {
        if (database == null) return null;
        foreach (var q in database.quests)
            if (q != null && q.questId == questId) return q;
        return null;
    }

    public int GetSavedCount(string questId)
    {
        return _savedCounts != null && _savedCounts.TryGetValue(questId, out var v) ? v : 0;
    }

    public int GetClaimableTimes(string questId)
    {
        var def = GetDefinition(questId);
        if (def == null || def.targetCount <= 0) return 0;
        int have = GetSavedCount(questId);
        int times = have / def.targetCount;
        if (!def.repeatable) times = Mathf.Clamp(times, 0, 1);
        return times;
    }

    public bool CanClaim(string questId) => GetClaimableTimes(questId) > 0;

    // === 리포트: 자동 보상 없음(씬에서 수동 완료만) ===
    public void ReportMonsterKill(Monster_Base.MonsterElement element)
    {
        if (database == null) return;
        Element4 e4 = ConvertElement(element);

        foreach (var q in database.quests)
        {
            if (q == null) continue;

            switch (q.kind)
            {
                case QuestKind.Kill_Any100:
                    AddProgress(q.questId, 1);
                    break;
                case QuestKind.Kill_Ignis100:
                    if (e4 == Element4.Ignis) AddProgress(q.questId, 1);
                    break;
                case QuestKind.Kill_Aqua100:
                    if (e4 == Element4.Aqua) AddProgress(q.questId, 1);
                    break;
                case QuestKind.Kill_Ventus100:
                    if (e4 == Element4.Ventus) AddProgress(q.questId, 1);
                    break;
                case QuestKind.Kill_Terra100:
                    if (e4 == Element4.Terra) AddProgress(q.questId, 1);
                    break;
            }
        }

        OnProgressChanged?.Invoke();
    }

    public void ReportSkillUse()
    {
        if (database == null) return;

        foreach (var q in database.quests)
        {
            if (q == null) continue;
            if (q.kind == QuestKind.UseSkill_10)
                AddProgress(q.questId, 1);
        }

        OnProgressChanged?.Invoke();
    }

    // === 단일 퀘스트 완료(씬 UI에서 호출) ===
    public bool Claim(string questId, int times = 1)
    {
        var def = GetDefinition(questId);
        if (def == null || def.targetCount <= 0) return false;

        int possible = GetClaimableTimes(questId);
        if (possible <= 0) return false;

        int doTimes = Mathf.Clamp(times, 1, possible);

        // 소비
        _savedCounts[questId] = GetSavedCount(questId) - (doTimes * def.targetCount);

        // 보상
        Grant(def.reward, doTimes);

        Debug.Log($"[Quest] Claim '{def.title}' x{doTimes}. Consumed:{doTimes * def.targetCount}, Left:{_savedCounts[questId]}");

        OnProgressChanged?.Invoke();
        return true;
    }

    private void Grant(QuestReward reward, int times)
    {
        if (character == null) character = FindObjectOfType<Character>();
        if (character == null)
        {
            Debug.LogWarning("[Quest] Character 없음. 보상 실패");
            return;
        }

        int exp = reward.exp * times;
        int gold = reward.gold * times;
        int dust = reward.skillDust * times;

        if (exp > 0) character.CharacterLevelUP(exp);
        if (gold > 0) character.Character_Gold += gold;
        if (dust > 0) character.Character_SkillDust += dust;
    }

    private void AddProgress(string questId, int delta)
    {
        if (!_savedCounts.ContainsKey(questId)) _savedCounts[questId] = 0;
        _savedCounts[questId] += Mathf.Max(0, delta);
    }

    private static Element4 ConvertElement(Monster_Base.MonsterElement e)
    {
        return e switch
        {
            Monster_Base.MonsterElement.Ignis => Element4.Ignis,
            Monster_Base.MonsterElement.Aqua => Element4.Aqua,
            Monster_Base.MonsterElement.Ventus => Element4.Ventus,
            Monster_Base.MonsterElement.Terra => Element4.Terra,
            _ => Element4.None
        };
    }

    // === 세이브/로드 ===
    [Serializable]
    public class QuestSOProgressSave
    {
        public List<string> ids = new();
        public List<int> counts = new();
    }

    public QuestSOProgressSave ExportSave()
    {
        var s = new QuestSOProgressSave();
        foreach (var kv in _savedCounts) { s.ids.Add(kv.Key); s.counts.Add(kv.Value); }
        return s;
    }

    public void ImportSave(QuestSOProgressSave s)
    {
        _savedCounts = new Dictionary<string, int>();
        if (s != null && s.ids != null && s.counts != null)
        {
            for (int i = 0; i < Mathf.Min(s.ids.Count, s.counts.Count); i++)
                _savedCounts[s.ids[i]] = Mathf.Max(0, s.counts[i]);
        }
        EnsureAllQuestsPrepared();
        OnProgressChanged?.Invoke();
    }

    // === 리셋 ===
    public void ResetAllProgress()
    {
        EnsureAllQuestsPrepared();
        var keys = new List<string>(_savedCounts.Keys);
        foreach (var k in keys) _savedCounts[k] = 0;
        OnProgressChanged?.Invoke();
    }
}
