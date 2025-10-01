using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI")]
    public Text titleText;
    public Text progressText;
    public Button completeButton;
    public CanvasGroup completeButtonCanvasGroup; // 반투명 처리용(없으면 생략 가능)

    private QuestDefinition _def;
    private QuestUI _owner;

    public void Init(QuestDefinition def, QuestUI owner)
    {
        _def = def;
        _owner = owner;

        if (completeButton != null)
        {
            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(() => _owner.OnClickClaim(_def));
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_def == null) return;

        int have = 0;
        int target = Mathf.Max(1, _def.targetCount);

        if (QuestManager.Instance != null)
            have = QuestManager.Instance.GetSavedCount(_def.questId);

        titleText.text = _def.title;
        progressText.text = $"{have} / {target}";

        bool can = QuestManager.Instance != null && QuestManager.Instance.CanClaim(_def.questId);

        if (completeButton != null)
        {
            completeButton.interactable = can;

            // 비활성 시 반투명 처리
            if (completeButtonCanvasGroup != null)
            {
                completeButtonCanvasGroup.alpha = can ? 1f : 0.5f;
                completeButtonCanvasGroup.interactable = can;
                completeButtonCanvasGroup.blocksRaycasts = can;
            }
        }
    }
}
