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
    public Button combineitemupButton;
    public Button combineitemdownButton;
    public Button UpgradeitemupButton;
    public Button UpgradeitemdownButton;

    private Character _character;
    private int _tier;
    private int _localIndex;
    private int _globalIndex;
    private Func<int, int, Skill_Data> _getSkill; // Character의 최신 스킬 참조를 가져오기 위한 콜백
    private Action _onClosed;                    // 닫힘 콜백(도감 갱신/저장 등)
    private Action _onUpgraded;                  // 강화 성공 시 콜백(도감 갱신 등)
    private int _breakCount = 0;    // 선택한 분해 개수
    private int _combineCount = 0;  // 선택한 조합 개수
    private int _upgradeCount = 0;  // 선택한 업그레이드 개수

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
        if (combineitemupButton) combineitemupButton.onClick.AddListener(() => ChangeCombineCount(+1));
        if (combineitemdownButton) combineitemdownButton.onClick.AddListener(() => ChangeCombineCount(-1));
        if (UpgradeitemupButton) UpgradeitemupButton.onClick.AddListener(() => ChangeUpgradeCount(+1));
        if (UpgradeitemdownButton) UpgradeitemdownButton.onClick.AddListener(() => ChangeUpgradeCount(-1));

        _upgradeCount = 0;
        _combineCount = 0;
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
        if (levelText) levelText.text = $"Lv. {skill.level} / {maxLevel}";

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
        int maxUpPreview = ComputeMaxUpgradeCount();
        int desiredUp = (_upgradeCount > 0) ? Mathf.Min(_upgradeCount, maxUpPreview) : 0;
        int costPreview = (desiredUp > 0) ? ComputeUpgradeCostFor(desiredUp) : 0;

        if (haveText)
        {
            if (costPreview > 0)
                haveText.text = $"Have: {have} (-{costPreview})";
            else
                haveText.text = $"Have: {have}";
        }

        UpdateBreakdownUI(have);    //분해하기 버튼 text 업데이트
        UpdateCombineUI();
        UpdateUpgradeUI();

        if (skilldustText && _tier >= 1) skilldustText.text = $"Dust: {_character.Character_SkillDust}";
        else if (skilldustText) skilldustText.gameObject.SetActive(false);

        // 보유 부족/만렙 시 버튼 비활성화
        if (upgradeButton) upgradeButton.interactable = (skill.level < maxLevel) && (have >= need);

        if (combineButton) combineButton.gameObject.SetActive(_tier >= 1);
        if (breakdownButton) breakdownButton.gameObject.SetActive(_tier >= 1);
        if (breakitemupButton) breakitemupButton.gameObject.SetActive(_tier >= 1);
        if (breakitemdownButton) breakitemdownButton.gameObject.SetActive(_tier >= 1);
        if (combineitemupButton) combineitemupButton.gameObject.SetActive(_tier >= 1);
        if (combineitemdownButton) combineitemdownButton.gameObject.SetActive(_tier >= 1);
        if (UpgradeitemupButton) UpgradeitemupButton.gameObject.SetActive(true);
        if (UpgradeitemdownButton) UpgradeitemdownButton.gameObject.SetActive(true);

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
            string fmt1 = (_combineCount > 0) ? $"Have: {baseHave1} (-{_combineCount})" : $"Have: {baseHave1}";
            string fmt2 = (_combineCount > 0) ? $"Have: {baseHave2} (-{_combineCount})" : $"Have: {baseHave2}";

            requiredskillhaveText1.text = fmt1;
            requiredskillhaveText2.text = fmt2;
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

        // 요청 강화 횟수: 선택값이 0이면 편의적으로 1회
        int maxUp = ComputeMaxUpgradeCount();
        int req = (_upgradeCount > 0) ? _upgradeCount : 1;
        int doUp = Mathf.Min(req, maxUp);

        if (doUp <= 0)
        {
            Debug.Log("[Upgrade] 재료 부족으로 강화 불가.");
            RefreshUI();
            return;
        }

        // 실제 N회 강화(매 레벨마다 요구치/능력치 갱신은 Character.LevelUpSkill에서 처리)
        for (int i = 0; i < doUp; i++)
        {
            _character.LevelUpSkill(_tier, _localIndex);
            // 혹시 중간에 만렙 도달하면 중단
            var cur = _getSkill?.Invoke(_tier, _localIndex);
            if (cur == null || cur.level >= maxLevel) break;
        }

        _upgradeCount = 0; // 초기화
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

        // 조합 개수 결정: 설정하지 않았다면 1개 유지(편의성)
        int countToCombine = (_combineCount > 0) ? _combineCount : 1;

        // 2) 요구 스킬을 "글로벌 인덱스" 단위로 묶어서 개수 합산 (중복 포함)
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
                // 슬롯 1개당 1개 필요 → countToCombine 배수로 요구
                reqMap[g] = (countToCombine, bTier, bLocal, baseSd);
            }
            else
            {
                // 중복 슬롯이면 추가로 countToCombine 만큼 더 필요
                reqMap[g] = (cur.requiredCount + countToCombine, cur.baseTier, cur.baseLocal, cur.sd);
            }
        }

        // 3) 더스트 필요량 계산
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

        // 5) 실제 차감: 보유 먼저, 부족분은 더스트로
        foreach (var kv in reqMap)
        {
            int g = kv.Key;
            var info = kv.Value;

            int haveNow = GetHave(g);
            int usePieces = Mathf.Min(haveNow, info.requiredCount);
            int shortage = info.requiredCount - usePieces;

            if (usePieces > 0)
            {
                AddHave(g, -usePieces);
                Debug.Log($"[Combine] 하위 스킬(global={g}) {usePieces}개 차감 (요구 {info.requiredCount}, 잔여 {GetHave(g)})");
            }

            if (shortage > 0)
            {
                int dustPerOne = GetDustCostForBaseTier(info.baseTier);
                int dustCost = shortage * dustPerOne;
                _character.Character_SkillDust -= dustCost;
                Debug.Log($"[Combine] 더스트 {dustCost} 소모 (부족 {shortage}개, tier={info.baseTier})");
            }
        }

        // 6) 타겟 스킬 보유 + countToCombine
        AddHave(_globalIndex, +countToCombine);
        Debug.Log($"[Combine] 타겟 스킬(global={_globalIndex}) 보유 +{countToCombine} 완료.");

        // 7) 후처리
        _combineCount = 0; // 초기화
        TrySave();
        RefreshUI();
        _onUpgraded?.Invoke();
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

    // 현재 상태(보유 조각/더스트/레시피)에 따라 가능한 최대 조합 수 계산
    private int ComputeMaxCombineCount()
    {
        if (_character == null || _tier < 1) return 0;

        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null || skill.requiredBaseSkills == null || skill.requiredBaseSkills.Count == 0)
            return 0;

        // 각 슬롯(보통 2개) 기준으로 티어/글로벌/보유/더스트비용을 수집
        // slots: 최대 2개로 가정 (필요시 일반화 가능)
        var slots = new List<(int g, int tier, int have, int dustPer)>();
        foreach (var baseSd in skill.requiredBaseSkills)
        {
            if (baseSd == null) return 0;
            if (!FindSkillIndex(baseSd, out int bTier, out int bLocal)) return 0;
            int g = GetGlobalIndex(_character, bTier, bLocal);
            int have = GetHave(g);
            int dustPer = GetDustCostForBaseTier(bTier);
            slots.Add((g, bTier, have, dustPer));
        }

        // 동일 파츠인지 체크 (0/1 슬롯만 사용한다고 가정)
        bool same = (slots.Count >= 2) && (slots[0].g == slots[1].g);

        int dust = _character.Character_SkillDust;

        if (same)
        {
            // 한 번 조합에 같은 파츠가 2개 필요
            int have = slots[0].have;
            int dustPer = slots[0].dustPer;
            int dustUnits = dustPer > 0 ? (dust / dustPer) : 0;
            // 총 조합 가능 수 = floor( (have + dustUnits) / 2 )
            return Mathf.Max(0, (have + dustUnits) / 2);
        }
        else
        {
            // 서로 다른 파츠가 1개씩 필요 → 각 파츠의 (have + dustUnits) 중 최소
            int max0 = slots[0].have + (slots[0].dustPer > 0 ? (dust / slots[0].dustPer) : 0);
            int max1 = slots[1].have + (slots[1].dustPer > 0 ? (dust / slots[1].dustPer) : 0);
            return Mathf.Max(0, Mathf.Min(max0, max1));
        }
    }

    // 조합 UI 상태 갱신(버튼 텍스트/활성)
    private void UpdateCombineUI()
    {
        // 조합 가능 최대치 산출(보유 조각+현재 더스트 기준)
        int maxComb = ComputeMaxCombineCount();

        // 선택 개수 보정
        _combineCount = Mathf.Clamp(_combineCount, 0, maxComb);

        // 미리보기 개수: 선택값이 0이면 1개 기준으로 표시(편의성)
        int previewCount = (_combineCount > 0) ? _combineCount : 1;

        // 예상 더스트(현재 보유 조각 우선 사용, 부족분만 더스트로)
        int dustPreview = ComputeDustNeededFor(previewCount);

        // 버튼 텍스트: 두 줄로 표시
        var label = GetButtonLabel(combineButton);
        if (label != null)
        {
            if (_combineCount > 0)
                label.text = $"조합하기: {_combineCount}개\n추가 가루: {dustPreview}개";
            else
                label.text = $"조합하기\n추가 가루: {dustPreview}개";
        }

        // 버튼 활성화
        if (combineButton) combineButton.interactable = (_tier >= 1) && (maxComb > 0);
        if (combineitemupButton) combineitemupButton.interactable = (_tier >= 1) && (_combineCount < maxComb);
        if (combineitemdownButton) combineitemdownButton.interactable = (_tier >= 1) && (_combineCount > 0);
    }


    // Up/Down 클릭 시 호출
    private void ChangeCombineCount(int delta)
    {
        int maxComb = ComputeMaxCombineCount();
        int prev = _combineCount;
        _combineCount = Mathf.Clamp(_combineCount + delta, 0, maxComb);

        if (_combineCount != prev)
        {
            // UI 텍스트 및 버튼 상태 갱신
            UpdateCombineUI();
            // 필요 파츠 Have: n(-m) 다시 표시
            RefreshUI(); // 또는 필요한 부분만 다시 그려도 OK
        }
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
    // count개 조합 시 필요한 더스트 총합 계산
    private int ComputeDustNeededFor(int count)
    {
        if (_character == null || _tier < 1 || count <= 0) return 0;

        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null || skill.requiredBaseSkills == null || skill.requiredBaseSkills.Count == 0)
            return 0;

        // 레시피를 전역 인덱스 기준으로 묶어서 "필요 개수" 누적
        var reqMap = BuildReqMapForCount(skill.requiredBaseSkills, count);

        // 보유 조각과 비교해 부족분 × 티어별 더스트 비용 합산
        int totalDustNeeded = 0;
        foreach (var kv in reqMap)
        {
            int g = kv.Key;
            var info = kv.Value; // (requiredCount, baseTier, baseLocal, sd)
            int have = GetHave(g);
            int shortage = Mathf.Max(0, info.requiredCount - have);
            if (shortage > 0)
            {
                int dustPerOne = GetDustCostForBaseTier(info.baseTier);
                totalDustNeeded += shortage * dustPerOne;
            }
        }
        return totalDustNeeded;
    }

    // 레시피 × count 를 전역 인덱스별 요구량으로 변환
    private Dictionary<int, (int requiredCount, int baseTier, int baseLocal, Skill_Data sd)>
        BuildReqMapForCount(List<Skill_Data> bases, int count)
    {
        var reqMap = new Dictionary<int, (int requiredCount, int baseTier, int baseLocal, Skill_Data sd)>();
        foreach (var baseSd in bases)
        {
            if (baseSd == null) continue;
            if (!FindSkillIndex(baseSd, out int bTier, out int bLocal)) continue;

            int g = GetGlobalIndex(_character, bTier, bLocal);
            if (!reqMap.TryGetValue(g, out var cur))
            {
                reqMap[g] = (count, bTier, bLocal, baseSd);
            }
            else
            {
                reqMap[g] = (cur.requiredCount + count, cur.baseTier, cur.baseLocal, cur.sd);
            }
        }
        return reqMap;
    }

    // 현재 상태(레벨/조각/요구치)에 따라 가능한 최대 강화 횟수 계산(시뮬레이션)
    private int ComputeMaxUpgradeCount()
    {
        if (_character == null) return 0;

        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null) return 0;

        int maxLv = GetMaxLevelByTier(_tier);
        int level = skill.level;
        int need = Mathf.Max(1, skill.NeedLevelUP_Gold);
        int have = GetHave(_globalIndex);
        int count = 0;

        while (level < maxLv && have >= need)
        {
            // 이 단계의 강화 가능 → 소비
            have -= need;
            int prevLevel = level;
            level++;

            // 다음 레벨 요구치 규칙( Character.LevelUpSkill 과 동일 로직 )
            bool crossingTen = (prevLevel % 10 == 9);
            if (_tier == 0)
            {
                need += 2;
                if (crossingTen) need = Mathf.Max(1, need * 2);
            }
            else
            {
                need += 5;
                if (crossingTen) need += 10;
            }

            count++;
            // 안전 가드
            if (count > 1000) break;
        }

        return count;
    }

    // 강화 UI 상태 갱신(버튼 텍스트/활성)
    private void UpdateUpgradeUI()
    {
        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null) return;

        int maxUp = ComputeMaxUpgradeCount();
        _upgradeCount = Mathf.Clamp(_upgradeCount, 0, maxUp);

        // 버튼 텍스트: 0이면 "강화하기", >0이면 "강화하기(+n)"
        var label = GetButtonLabel(upgradeButton);
        if (label != null)
            label.text = (_upgradeCount > 0) ? $"강화하기(+{_upgradeCount})" : "강화하기";

        // 인터랙션
        if (upgradeButton) upgradeButton.interactable = maxUp > 0;
        if (UpgradeitemupButton) UpgradeitemupButton.interactable = _upgradeCount < maxUp;
        if (UpgradeitemdownButton) UpgradeitemdownButton.interactable = _upgradeCount > 0;
    }

    // Up/Down 클릭 시 호출
    private void ChangeUpgradeCount(int delta)
    {
        int maxUp = ComputeMaxUpgradeCount();
        int prev = _upgradeCount;
        _upgradeCount = Mathf.Clamp(_upgradeCount + delta, 0, maxUp);
        if (_upgradeCount != prev)
        {
            UpdateUpgradeUI();
            RefreshUI(); 
        }
    }
    // 원하는 횟수(count) 강화 시 소모되는 총 조각 수(미리보기)
    // - 현재 보유 조각/만렙/요구치 증가 규칙을 모두 반영
    // - count가 현재 가능한 최대 강화 횟수를 초과해도, 가능한 범위까지 계산해 반환
    private int ComputeUpgradeCostFor(int count)
    {
        if (_character == null || count <= 0) return 0;

        var skill = _getSkill?.Invoke(_tier, _localIndex);
        if (skill == null) return 0;

        int maxLv = GetMaxLevelByTier(_tier);
        int level = skill.level;
        int need = Mathf.Max(1, skill.NeedLevelUP_Gold);
        int have = GetHave(_globalIndex);
        int used = 0;
        int steps = 0;

        while (steps < count && level < maxLv && have >= need)
        {
            // 이번 단계에 필요한 조각을 소모(미리보기 합산)
            used += need;
            have -= need;

            int prevLevel = level;
            level++;

            // 다음 단계 요구치 갱신( Character.LevelUpSkill 과 동일 규칙 )
            bool crossingTen = (prevLevel % 10 == 9);
            if (_tier == 0)
            {
                need += 2;
                if (crossingTen) need = Mathf.Max(1, need * 2);
            }
            else
            {
                need += 5;
                if (crossingTen) need += 10;
            }

            steps++;
            if (steps > 1000) break; // 안전 가드
        }

        return used;
    }

}
