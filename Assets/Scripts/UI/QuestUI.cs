using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    [Header("DB / Manager")]
    public QuestDatabase database;

    [Header("ScrollView")]
    public Transform contentParent;       // ScrollView/Viewport/Content
    public GameObject questItemPrefab;    // 행 프리팹(QuestItemUI 붙음)

    [Header("Popup")]
    public GameObject rewardPopupPrefab;  // RewardPopup 프리팹
    private GameObject _currentPopup;     // 현재 떠 있는 팝업 단일 관리

    private readonly List<QuestItemUI> _items = new();

    private void OnEnable()
    {
        BuildList();
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnProgressChanged += RefreshAll;
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnProgressChanged -= RefreshAll;
    }

    private void BuildList()
    {
        // 기존 제거
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        _items.Clear();

        if (database == null || database.quests == null) return;

        foreach (var def in database.quests)
        {
            if (def == null) continue;
            var go = Instantiate(questItemPrefab, contentParent);
            var item = go.GetComponent<QuestItemUI>();
            item.Init(def, this);
            _items.Add(item);
        }

        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (var it in _items)
            it.Refresh();
    }

    // 완료 버튼이 누르면 호출됨
    public void OnClickClaim(QuestDefinition def)
    {
        if (QuestManager.Instance == null) return;

        bool ok = QuestManager.Instance.Claim(def.questId, 1);
        if (!ok) return;

        // 기존 팝업이 있으면 닫고 새로 띄우기
        if (_currentPopup != null) Destroy(_currentPopup);

        _currentPopup = Instantiate(rewardPopupPrefab, transform);
        var popup = _currentPopup.GetComponent<QuestRewardPopup>();
        int times = 1;
        popup.Setup(def.title, def.reward, times, () =>
        {
            // 콜백: 팝업이 꺼질 때 핸들 필요하면 여기에
            _currentPopup = null;
        });

        // 목록 갱신
        RefreshAll();
    }
}
