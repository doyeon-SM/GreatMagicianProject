using UnityEngine;
using UnityEngine.UI;
using System;

public class SkillUpgradeUI : MonoBehaviour
{
    [Header("Wiring (assign in prefab)")]
    public Button closeButton;           // 닫기 버튼
    public Button upgradeButton;         // 강화 버튼
    public Button outsideCloseButton;    // 전체 화면 딤(배경) 버튼 - 바깥 클릭 닫기
    public Button combineButton;         // 조합 버튼
    public Button breakdownButton;       // 분해 버튼
    public Image skillIconImage;
    public Text skillNameText;
    public Text tierText;
    public Text levelText;
    public Text damageText;
    public Text needText;              // 다음 강화 필요 수량 (NeedLevelUP_Gold 해석)
    public Text haveText;              // 내 보유 수량(Character_HaveSkill[globalIndex])
    public Text descText;              // 설명
    public Image requiredskillIconImage1;
    public Image requiredskillIconImage2;
    public Text skilldustText;
    public Text requiredskillhaveText1;
    public Text requiredskillhaveText2;


    private Character _character;
    private int _tier;
    private int _localIndex;
    private int _globalIndex;
    private Func<int, int, Skill_Data> _getSkill; // Character의 최신 스킬 참조를 가져오기 위한 콜백
    private Action _onClosed;                    // 닫힘 콜백(도감 갱신/저장 등)
    private Action _onUpgraded;                  // 강화 성공 시 콜백(도감 갱신 등)

    // 외부에서 호출: 초기화
    public void Init(
        Character character,
        int tier,
        int localIndex,
        Func<int, int, Skill_Data> getSkillFunc,
        Action onClosed,
        Action onUpgraded)
    {
        _character = character;
        _tier = tier;
        _localIndex = localIndex;
        _getSkill = getSkillFunc;
        _onClosed = onClosed;
        _onUpgraded = onUpgraded;

        _globalIndex = GetGlobalIndex(_character, _tier, _localIndex);

        // 버튼 이벤트
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (outsideCloseButton) outsideCloseButton.onClick.AddListener(Close);
        if (upgradeButton) upgradeButton.onClick.AddListener(HandleUpgrade);
        if (combineButton) combineButton.onClick.AddListener(HandleCombine);
        if (breakdownButton) breakdownButton.onClick.AddListener(HandleBreakdown);

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (_character == null || _getSkill == null) return;

        var skill = _getSkill(_tier, _localIndex);
        if (skill == null) return;

        if (skillIconImage) skillIconImage.sprite = skill.skillIcon;
        if (_tier >= 1)
        {
            if (requiredskillIconImage1) requiredskillIconImage1.sprite = skill.requiredBaseSkills[0].skillIcon;
            if (requiredskillIconImage2) requiredskillIconImage2.sprite = skill.requiredBaseSkills[1].skillIcon;
            skill.UpdateDamage();
        }
        else
        {
            if (requiredskillIconImage1) requiredskillIconImage1.gameObject.SetActive(false);
            if (requiredskillIconImage2) requiredskillIconImage2.gameObject.SetActive(false);
        }
        if (skillNameText) skillNameText.text = skill.skillName;
        if (tierText) tierText.text = $"Tier {_tier}";
        if (levelText) levelText.text = $"Lv. {skill.level}";
        if (damageText) damageText.text = $"Damage: {skill.damage}";
        if (descText) descText.text = string.IsNullOrEmpty(skill.skillscript) ? "" : skill.skillscript;

        int need = Mathf.Max(1, skill.NeedLevelUP_Gold);
        int have = (_character.Character_HaveSkill != null &&
                    _globalIndex >= 0 &&
                    _globalIndex < _character.Character_HaveSkill.Length)
                   ? _character.Character_HaveSkill[_globalIndex]
                   : 0;

        if (needText) needText.text = $"Need: {need}";
        if (haveText) haveText.text = $"Have: {have}";
        if (skilldustText && _tier >= 1) skilldustText.text = $"Dust: {_character.Character_SkillDust}";
        else skilldustText.gameObject.SetActive(false);

        // 보유 부족 시 버튼 비활성/상태 처리
        if (upgradeButton) upgradeButton.interactable = (have >= need);

        if (combineButton) combineButton.gameObject.SetActive(_tier>=1);
        if (breakdownButton) breakdownButton.gameObject.SetActive(_tier >= 1);
        if (_tier >= 1 && requiredskillhaveText1 && requiredskillhaveText2)
        {
            int baseHave1 = 0;
            int baseHave2 = 0;

            if (skill.requiredBaseSkills != null)
            {
                // 첫 번째 베이스
                if (skill.requiredBaseSkills[0] != null &&
                    FindSkillIndex(skill.requiredBaseSkills[0], out int bTier0, out int bLocal0))
                {
                    int g0 = GetGlobalIndex(_character, bTier0, bLocal0);
                    baseHave1 = GetHave(g0);
                }

                // 두 번째 베이스
                if (skill.requiredBaseSkills[1] != null &&
                    FindSkillIndex(skill.requiredBaseSkills[1], out int bTier1, out int bLocal1))
                {
                    int g1 = GetGlobalIndex(_character, bTier1, bLocal1);
                    baseHave2 = GetHave(g1);
                }
            }

            requiredskillhaveText1.gameObject.SetActive(true);
            requiredskillhaveText2.gameObject.SetActive(true);
            requiredskillhaveText1.text = $"Have: {baseHave1}";
            requiredskillhaveText2.text = $"Have: {baseHave2}";
        }
        else
        {
            if (requiredskillhaveText1) requiredskillhaveText1.gameObject.SetActive(false);
            if (requiredskillhaveText2) requiredskillhaveText2.gameObject.SetActive(false);
        }

    }

    private void HandleUpgrade()
    {
        if (_character == null) return;

        // 요구사항: Character.cs 속 LevelUpSkill(tier, index) 사용
        _character.LevelUpSkill(_tier, _localIndex);

        // 강화 후 UI 갱신
        RefreshUI();

        // 도감 등 상위 뷰도 갱신하고 싶을 때
        _onUpgraded?.Invoke();
    }
    private void HandleCombine()
    {
        if (_character == null) return;

        // 1) 티어 체크
        if (_tier < 1)
        {
            Debug.LogWarning("[Combine] Tier 0 스킬은 조합 불가.");
            return;
        }

        var targetSkill = _getSkill?.Invoke(_tier, _localIndex);
        if (targetSkill == null)
        {
            Debug.LogWarning("[Combine] 타겟 스킬 없음.");
            return;
        }

        // 2) 하위 파츠 확인 (기대: 2개. 없으면 가능한 만큼만 처리)
        var bases = targetSkill.requiredBaseSkills;
        if (bases == null)
        {
            Debug.LogWarning("[Combine] requiredBaseSkills 비어있음. 조합 스펙이 설정되지 않았습니다.");
            return;
        }

        // 준비: 각 베이스 스킬의 위치/보유량/부족분에 대한 더스트 비용 계산
        int totalDustNeeded = 0;

        // 각 베이스의 글로벌 인덱스 및 티어/인덱스 캐시
        var baseInfos = new System.Collections.Generic.List<(Skill_Data sd, int baseTier, int baseLocal, int baseGlobal, int have, bool willUseDust, int dustCost)>();

        foreach (var baseSd in bases)
        {
            if (baseSd == null) continue;

            if (!FindSkillIndex(baseSd, out int baseTier, out int baseLocal))
            {
                Debug.LogWarning($"[Combine] 하위 스킬 인덱스 탐색 실패: {baseSd.name}");
                return;
            }
            int baseGlobal = GetGlobalIndex(_character, baseTier, baseLocal);
            int haveBase = GetHave(baseGlobal);

            bool needDust = haveBase <= 0;
            int dustCost = 0;
            if (needDust)
            {
                dustCost = GetDustCostForBaseTier(baseTier);
                totalDustNeeded += dustCost;
            }
            baseInfos.Add((baseSd, baseTier, baseLocal, baseGlobal, haveBase, needDust, dustCost));
        }

        // 3) 더스트 보유량 확인
        int currentDust = _character.Character_SkillDust;
        if (currentDust < totalDustNeeded)
        {
            Debug.LogWarning($"[Combine] 스킬더스트 부족: 필요 {totalDustNeeded}, 보유 {currentDust}");
            return;
        }

        // 4) 실행: 파츠 차감 또는 더스트 소모 → 타겟 스킬 Have +1
        foreach (var info in baseInfos)
        {
            if (info.willUseDust)
            {
                _character.Character_SkillDust -= info.dustCost;
                Debug.Log($"[Combine] 더스트 {info.dustCost} 소모 (baseTier={info.baseTier})");
            }
            else
            {
                AddHave(info.baseGlobal, -1);
                Debug.Log($"[Combine] 하위 스킬 1개 차감 (global={info.baseGlobal})");
            }
        }

        // 타겟 스킬 보유 +1
        AddHave(_globalIndex, +1);
        Debug.Log($"[Combine] 타겟 스킬(global={_globalIndex}) 보유 +1 완료.");

        TrySave();
        RefreshUI();
        _onUpgraded?.Invoke(); // 상위 목록 갱신에 재사용
    }
    private void HandleBreakdown()
    {
        if (_character == null) return;

        if (_tier < 1)
        {
            Debug.LogWarning("[Breakdown] Tier 0 스킬은 분해 불가.");
            return;
        }

        int have = GetHave(_globalIndex);
        if (have <= 0)
        {
            Debug.LogWarning("[Breakdown] 보유 수량 없음.");
            return;
        }

        // 환급량: 1티어=100, 2티어=500 (명시 스펙)
        int refund = (_tier == 1) ? 100 :
                     (_tier == 2) ? 500 : 500; // 3티어 이상 가정 시 500 유지

        AddHave(_globalIndex, -1);
        _character.Character_SkillDust += refund;

        Debug.Log($"[Breakdown] 스킬 분해: Have -1, Dust +{refund} (tier={_tier})");

        TrySave();
        RefreshUI();
        _onUpgraded?.Invoke();
    }

    private void Close()
    {
        TrySave();
        _onClosed?.Invoke();
        Destroy(gameObject);
    }

    // 안전 저장 래퍼
    private void TrySave()
    {
        try
        {
            SaveSystem.SaveGame();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SkillUpgradeUI] SaveGame 예외: {ex.Message}");
        }
    }

    private int GetGlobalIndex(Character c, int tier, int localIndex)
    {
        int offset = 0;
        if (tier > 0) offset += (c.tier0Skills?.Length ?? 0);
        if (tier > 1) offset += (c.tier1Skills?.Length ?? 0);
        return offset + localIndex;
    }
    private bool FindSkillIndex(Skill_Data target, out int tier, out int localIndex)
    {
        tier = -1; localIndex = -1;
        if (_character == null || target == null) return false;

        // 티어0
        var t0 = _character.tier0Skills;
        if (t0 != null)
        {
            for (int i = 0; i < t0.Length; i++)
            {
                if (t0[i] == target) { tier = 0; localIndex = i; return true; }
            }
        }

        // 티어1
        var t1 = _character.tier1Skills;
        if (t1 != null)
        {
            for (int i = 0; i < t1.Length; i++)
            {
                if (t1[i] == target) { tier = 1; localIndex = i; return true; }
            }
        }

        // 티어2
        var t2 = _character.tier2Skills;
        if (t2 != null)
        {
            for (int i = 0; i < t2.Length; i++)
            {
                if (t2[i] == target) { tier = 2; localIndex = i; return true; }
            }
        }

        return false;
    }

    // 더스트 비용 규칙
    private int GetDustCostForBaseTier(int baseTier)
    {
        // 스펙: 0티어=100, 1티어=500
        if (baseTier <= 0) return 100;
        if (baseTier == 1) return 500;
        // 그 외 티어가 생길 경우 보수적으로 500 유지
        return 500;
    }

    // 보유 수량 헬퍼
    private int GetHave(int globalIndex)
    {
        if (_character?.Character_HaveSkill == null) return 0;
        if (globalIndex < 0 || globalIndex >= _character.Character_HaveSkill.Length) return 0;
        return _character.Character_HaveSkill[globalIndex];
    }

    private void AddHave(int globalIndex, int delta)
    {
        if (_character?.Character_HaveSkill == null) return;
        if (globalIndex < 0 || globalIndex >= _character.Character_HaveSkill.Length) return;
        _character.Character_HaveSkill[globalIndex] = Mathf.Max(0, _character.Character_HaveSkill[globalIndex] + delta);
    }
}
