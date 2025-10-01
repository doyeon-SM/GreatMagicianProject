using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

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
    public Text skillEffectText;
    public Text skillAreaTimeText;
    public Text skillElementText;
    public Button breakitemupButton;
    public Button breakitemdownButton;


    private Character _character;
    private int _tier;
    private int _localIndex;
    private int _globalIndex;
    private Func<int, int, Skill_Data> _getSkill; // Character의 최신 스킬 참조를 가져오기 위한 콜백
    private Action _onClosed;                    // 닫힘 콜백(도감 갱신/저장 등)
    private Action _onUpgraded;                  // 강화 성공 시 콜백(도감 갱신 등)
    private int _breakCount = 0;    // 선택한 분해 개수

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
        if (breakitemupButton) breakitemupButton.onClick.AddListener(() => ChangeBreakCount(+1));
        if (breakitemdownButton) breakitemdownButton.onClick.AddListener(() => ChangeBreakCount(-1));

        _breakCount = 0; // 초기화
        RefreshUI();
    }
    private int GetMaxLevelByTier(int tier)
    {
        return (tier == 0) ? 100 : 50;
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

        // 만렙 계산 및 표기
        int maxLevel = GetMaxLevelByTier(_tier);
        if (levelText) levelText.text = $"Lv. {skill.level}/{maxLevel}";

        if (damageText) 
        {
            string dam;
            if (skill.skillType == Skill_Data.SkillType.Create || skill.skillType == Skill_Data.SkillType.Summon) dam = "HP: ";
            else dam = "Damage: ";
            damageText.text = $"{dam}{skill.damage}"; 
        }
        if (descText) descText.text = string.IsNullOrEmpty(skill.skillscript) ? "" : skill.skillscript;

        if (skillEffectText && skill.Effect_Value > 0) skillEffectText.text = $"{skilleffecttext(skill.skillEffect)}{skill.Effect_Value}";
        else skillEffectText.gameObject.SetActive(false);
        if (skillAreaTimeText && skill.AreaTime > 0) skillAreaTimeText.text = $"지속시간: {skill.AreaTime}초";
        else skillAreaTimeText.gameObject.SetActive(false);
        if (skillElementText) skillElementText.text = $"Element: {skill.skillElement}";

        int need = Mathf.Max(1, skill.NeedLevelUP_Gold);
        int have = (_character.Character_HaveSkill != null &&
                    _globalIndex >= 0 &&
                    _globalIndex < _character.Character_HaveSkill.Length)
                   ? _character.Character_HaveSkill[_globalIndex]
                   : 0;

        // 만렙이면 필요표시를 MAX
        if (needText) 
        {
            needText.text = $"{have} / ";
            needText.text += (skill.level >= maxLevel) ? "MAX" : $"{need}"; 
        }
        if (haveText) haveText.text = $"Have: {have}";
        UpdateBreakdownUI(have);    //분해하기 버튼 text 업데이트

        if (skilldustText && _tier >= 1) skilldustText.text = $"Dust: {_character.Character_SkillDust}";
        else if (skilldustText) skilldustText.gameObject.SetActive(false);

        // 보유 부족/만렙 시 버튼 비활성화
        if (upgradeButton) upgradeButton.interactable = (skill.level < maxLevel) && (have >= need);

        if (combineButton) combineButton.gameObject.SetActive(_tier >= 1);
        if (breakdownButton) breakdownButton.gameObject.SetActive(_tier >= 1);
        if (breakitemupButton) breakitemupButton.gameObject.SetActive(_tier >= 1);
        if (breakitemdownButton) breakitemdownButton.gameObject.SetActive(_tier >= 1);

        if (_tier >= 1 && requiredskillhaveText1 && requiredskillhaveText2)
        {
            int baseHave1 = 0;
            int baseHave2 = 0;

            if (skill.requiredBaseSkills != null)
            {
                if (skill.requiredBaseSkills[0] != null &&
                    FindSkillIndex(skill.requiredBaseSkills[0], out int bTier0, out int bLocal0))
                {
                    int g0 = GetGlobalIndex(_character, bTier0, bLocal0);
                    baseHave1 = GetHave(g0);
                }

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

    private string skilleffecttext(Skill_Data.SkillEffect eff)
    {
        switch(eff)
        {
            case Skill_Data.SkillEffect.Knockback:
                return "넉백량: ";
            case Skill_Data.SkillEffect.Fear:
                return "공포 지속시간: ";
            case Skill_Data.SkillEffect.Burn:
                return "화상 지속시간: ";
            case Skill_Data.SkillEffect.Posion:
                return "독 지속시간: ";
            case Skill_Data.SkillEffect.Gravity:
                return "중력: ";
            default:
                return "???";
        }
    }

    private void HandleUpgrade()
    {
        if (_character == null) return;

        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null) return;

        int maxLevel = GetMaxLevelByTier(_tier);
        if (skill.level >= maxLevel)
        {
            Debug.Log("[Upgrade] 만렙입니다. 업그레이드 불가.");
            RefreshUI();
            return;
        }

        _character.LevelUpSkill(_tier, _localIndex);
        RefreshUI();
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

        var bases = targetSkill.requiredBaseSkills;
        if (bases == null || bases.Count == 0)
        {
            Debug.LogWarning("[Combine] requiredBaseSkills 비어있음. 조합 스펙이 설정되지 않았습니다.");
            return;
        }

        // 2) 요구 스킬을 "글로벌 인덱스" 단위로 묶어서 개수를 합산 (중복 처리 핵심)
        //    key: globalIndex, value: (requiredCount, baseTier, baseLocal, sample Skill_Data)
        var reqMap = new Dictionary<int, (int requiredCount, int baseTier, int baseLocal, Skill_Data sd)>();

        foreach (var baseSd in bases)
        {
            if (baseSd == null)
            {
                Debug.LogWarning("[Combine] requiredBaseSkills에 null 항목이 있습니다.");
                return;
            }

            if (!FindSkillIndex(baseSd, out int bTier, out int bLocal))
            {
                Debug.LogWarning($"[Combine] 하위 스킬 인덱스 탐색 실패: {baseSd.name}");
                return;
            }

            int g = GetGlobalIndex(_character, bTier, bLocal);
            if (!reqMap.TryGetValue(g, out var cur))
            {
                reqMap[g] = (1, bTier, bLocal, baseSd);
            }
            else
            {
                reqMap[g] = (cur.requiredCount + 1, cur.baseTier, cur.baseLocal, cur.sd);
            }
        }

        // 3) 더스트 필요량 계산: (부족 개수 × 티어별 비용)을 모두 합산
        int totalDustNeeded = 0;
        foreach (var kv in reqMap)
        {
            int g = kv.Key;
            var info = kv.Value;
            int have = GetHave(g);
            int shortage = Mathf.Max(0, info.requiredCount - have);
            if (shortage > 0)
            {
                int dustPerOne = GetDustCostForBaseTier(info.baseTier);
                totalDustNeeded += shortage * dustPerOne;
            }
        }

        // 4) 더스트 보유량 확인
        int currentDust = _character.Character_SkillDust;
        if (currentDust < totalDustNeeded)
        {
            Debug.LogWarning($"[Combine] 스킬더스트 부족: 필요 {totalDustNeeded}, 보유 {currentDust}");
            return;
        }

        // 5) 실제 차감 수행
        //    - 전역 인덱스별로: 보유 수량에서 가능한 만큼 차감
        //    - 남은 부족분은 더스트로 대체
        foreach (var kv in reqMap)
        {
            int g = kv.Key;
            var info = kv.Value;

            int haveNow = GetHave(g);
            int usePieces = Mathf.Min(haveNow, info.requiredCount);
            int shortage = info.requiredCount - usePieces;

            // 보유 스킬 조각 차감
            if (usePieces > 0)
            {
                AddHave(g, -usePieces);
                Debug.Log($"[Combine] 하위 스킬(global={g}) {usePieces}개 차감 (요구 {info.requiredCount}, 보유 차감 후 {GetHave(g)})");
            }

            // 부족분은 더스트로 대체
            if (shortage > 0)
            {
                int dustPerOne = GetDustCostForBaseTier(info.baseTier);
                int dustCost = shortage * dustPerOne;
                _character.Character_SkillDust -= dustCost;
                Debug.Log($"[Combine] 더스트 {dustCost} 소모 (부족 {shortage}개, tier={info.baseTier})");
            }
        }

        // 6) 타겟 스킬 보유 +1
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
            UpdateBreakdownUI(have);
            return;
        }

        // 분해 개수 결정:
        // - _breakCount > 0 이면 그 개수만큼 한 번에 분해
        // - _breakCount == 0 이면 편의 모드: 1개만 분해 (기존 동작 유지)
        int countToBreak = (_breakCount > 0) ? Mathf.Min(_breakCount, have) : 1;

        // 환급량: 1티어=100, 2티어=500 (스펙)
        int refundPer = (_tier == 1) ? 100 : 500;

        AddHave(_globalIndex, -countToBreak);
        _character.Character_SkillDust += refundPer * countToBreak;

        Debug.Log($"[Breakdown] 스킬 분해: Have -{countToBreak}, Dust +{refundPer * countToBreak} (tier={_tier})");

        // 분해 후 선택 개수 초기화 & UI 갱신
        _breakCount = 0;
        TrySave();
        RefreshUI();
        _onUpgraded?.Invoke();
    }


    // 버튼 텍스트 얻기 (UGUI Text 기준)
    private Text GetButtonLabel(Button btn)
    {
        return btn ? btn.GetComponentInChildren<Text>() : null;
    }

    // have(보유 수량)에 맞춰 분해 UI 상태 갱신
    private void UpdateBreakdownUI(int have)
    {
        // 현재 선택 개수 보정
        _breakCount = Mathf.Clamp(_breakCount, 0, Mathf.Max(0, have));

        // breakdown 버튼 텍스트
        var label = GetButtonLabel(breakdownButton);
        if (label != null)
        {
            label.text = (_breakCount > 0) ? $"분해하기: {_breakCount}개" : "분해하기";
        }

        // 버튼 활성화 조건
        if (breakdownButton) breakdownButton.interactable = (_tier >= 1) && (have > 0);
        if (breakitemupButton) breakitemupButton.interactable = (_tier >= 1) && (have > _breakCount);
        if (breakitemdownButton) breakitemdownButton.interactable = (_tier >= 1) && (_breakCount > 0);
    }

    // Up/Down 클릭 시 호출
    private void ChangeBreakCount(int delta)
    {
        if (_character == null) return;

        int have = GetHave(_globalIndex);
        int prev = _breakCount;
        _breakCount = Mathf.Clamp(_breakCount + delta, 0, Mathf.Max(0, have));

        // 상태가 바뀌었을 때만 UI 갱신
        if (_breakCount != prev)
            UpdateBreakdownUI(have);
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
