using UnityEngine;
using UnityEngine.UI;
using System;

public class SkillUpgradeUI : MonoBehaviour
{
    [Header("Wiring (assign in prefab)")]
    public Button closeButton;           // 닫기 버튼
    public Button upgradeButton;         // 강화 버튼
    public Button outsideCloseButton;    // 전체 화면 딤(배경) 버튼 - 바깥 클릭 닫기
    public Image skillIconImage;
    public Text skillNameText;
    public Text tierText;
    public Text levelText;
    public Text damageText;
    public Text needText;              // 다음 강화 필요 수량 (NeedLevelUP_Gold 해석)
    public Text haveText;              // 내 보유 수량(Character_HaveSkill[globalIndex])
    public Text descText;              // 설명(있다면)
    public Image requiredskillIconImage1;
    public Image requiredskillIconImage2;

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

        // 보유 부족 시 버튼 비활성/상태 처리
        if (upgradeButton) upgradeButton.interactable = (have >= need);
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

    private void Close()
    {
        // 닫을 때 저장
        try
        {
            // SaveSystem.cs 안의 저장 함수 (정적 메서드라 가정)
            SaveSystem.SaveGame();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SkillUpgradeUI] SaveGame 호출 중 예외: {ex.Message}");
        }

        _onClosed?.Invoke();
        Destroy(gameObject);
    }

    private int GetGlobalIndex(Character c, int tier, int localIndex)
    {
        int offset = 0;
        if (tier > 0) offset += (c.tier0Skills?.Length ?? 0);
        if (tier > 1) offset += (c.tier1Skills?.Length ?? 0);
        return offset + localIndex;
    }
}
