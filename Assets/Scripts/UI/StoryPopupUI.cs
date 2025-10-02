using UnityEngine;
using UnityEngine.UI;

public class StoryPopupUI : MonoBehaviour
{
    [Header("BG (Modal)")]
    public Image dimmer;            // 반투명 전체 배경(색상/알파로 암전)
    public CanvasGroup cgRoot;      // 팝업 루트 (blocksRaycasts = true)

    [Header("Contents")]
    public Text titleText;          // "Stage 1-1" 등
    public Text descText;           // StoryStageAsset.description
    public Text rewardText;         // StoryStageAsset.rewardNote (또는 stageScore/bouns 등 포맷)

    [Header("Buttons")]
    public Button startButton;
    public Button closeButton;

    private StoryStageAsset _stage;
    private System.Action<StoryStageAsset> _onStart;
    private System.Action _onCancel;

    private void Awake()
    {
        if (cgRoot) { cgRoot.blocksRaycasts = true; cgRoot.interactable = true; }
        if (dimmer)
        {
            // dimmer 자체에 RaycastTarget = true 설정되어 있어야 뒤 UI 클릭 차단됨
            dimmer.raycastTarget = true;
        }

        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                _onStart?.Invoke(_stage);
                Close();
            });
        }
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                _onCancel?.Invoke();
                Close();
            });
        }
    }

    public void Open(StoryStageAsset stage, System.Action<StoryStageAsset> onStart, System.Action onCancel)
    {
        _stage = stage;
        _onStart = onStart;
        _onCancel = onCancel;

        if (titleText) titleText.text = $"Stage {stage.stageId}";
        if (descText) descText.text = stage.description;
        if (rewardText) rewardText.text = BuildRewardText(stage);
        gameObject.SetActive(true);
    }

    private string BuildRewardText(StoryStageAsset s)
    {
        // 기본 포맷 + 추가 메모
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Score: {s.stageScore}");
        if (s.bonusGold > 0) sb.AppendLine($"+ Gold: {s.bonusGold}");
        if (s.bonusExp > 0) sb.AppendLine($"+ EXP: {s.bonusExp}");
        if (!string.IsNullOrEmpty(s.rewardNote)) sb.AppendLine(s.rewardNote);
        return sb.ToString().TrimEnd();
    }

    private void Close()
    {
        Destroy(gameObject);
    }
}
