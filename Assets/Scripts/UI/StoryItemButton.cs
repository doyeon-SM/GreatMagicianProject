using UnityEngine;
using UnityEngine.UI;

public class StoryItemButton : MonoBehaviour
{
    public Text label;                 // 텍스트(없으면 GetComponentInChildren로 찾아도 OK)
    public Button button;

    private StoryStageAsset _stage;
    private System.Action<StoryStageAsset> _onClick;

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!label) label = GetComponentInChildren<Text>();
    }

    public void Setup(StoryStageAsset stage, string text, System.Action<StoryStageAsset> onClick)
    {
        _stage = stage;
        _onClick = onClick;

        if (label) label.text = text;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_stage));
        }
    }
}
