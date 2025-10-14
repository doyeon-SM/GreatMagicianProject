using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PausePopupUI : MonoBehaviour
{
    [Header("Wire in Prefab")]
    [SerializeField] private GameObject popupRoot;   // 전체 팝업 루트(비주얼 루트)
    [SerializeField] private Button resumeButton;     // 이어하기 버튼
    [SerializeField] private Image dimBackground;     // 전체화면 딤(이미지) - Raycast Target = true

    private CanvasGroup _cg;
    private bool _isOpen = false;
    private float _prevTimeScale = 1f;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        // 초기 비가시화
        HideImmediate();

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnClickResume);

        // 딤 배경은 클릭만 먹고 아무것도 안 함 (다른 UI 클릭 차단)
        if (dimBackground != null)
        {
            dimBackground.raycastTarget = true;
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        _prevTimeScale = Mathf.Max(0f, Time.timeScale);
        Time.timeScale = 0f;

        Show();
        // 최상단 보장 (같은 Canvas 내에서)
        //transform.SetAsLastSibling();
    }

    public void OnClickResume()
    {
        // timeScale 복구
        Time.timeScale = (_prevTimeScale <= 0f) ? 1f : _prevTimeScale;
        _prevTimeScale = 1f;

        _isOpen = false;
        HideImmediate();
    }

    private void Show()
    {
        if (popupRoot) popupRoot.SetActive(true);
        _cg.alpha = 1f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true; // 팝업 영역 내 Raycast 허용
    }

    private void HideImmediate()
    {
        if (popupRoot) popupRoot.SetActive(false);
        _cg.alpha = 0f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;
    }
}
