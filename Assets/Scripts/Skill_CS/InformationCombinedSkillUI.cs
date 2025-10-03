using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class InformationCombinedSkillUI : MonoBehaviour,
    IPointerDownHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsModalPause = false;

    [Header("Wiring (assign in prefab)")]
    public Image SkillIconImage;
    public Text SkillNameText;
    public Button ConfirmButton;

    [Header("Canvas Sorting (optional)")]
    public int sortingOrder = 6000; // 다른 UI보다 위에 오도록

    private Image _blocker;
    private float _prevTimeScale = 1f;
    private bool _canClose = false;

    private void Awake()
    {
        if (ConfirmButton != null) ConfirmButton.interactable = false;
        if (ConfirmButton != null) ConfirmButton.onClick.AddListener(OnClickConfirm);

        EnsureTopCanvas();
        EnsureRaycastBlocker();
    }

    public void Setup(string skillName, Sprite skillIcon, float prevTimeScale)
    {
        _prevTimeScale = Mathf.Max(0f, prevTimeScale);

        if (SkillNameText != null)
            SkillNameText.text = string.IsNullOrEmpty(skillName) ? "Unknown Skill" : skillName;

        if (SkillIconImage != null)
            SkillIconImage.sprite = skillIcon;

        // 모달 일시정지 진입
        IsModalPause = true;
        Time.timeScale = 0f; // 게임 일시정지
        // 0.5초 ‘실시간’ 대기 후 버튼/닫기 허용
        StartCoroutine(EnableCloseAfterDelayRealtime(0.5f));
    }

    private IEnumerator EnableCloseAfterDelayRealtime(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime; // 일시정지 중이므로 unscaled
            yield return null;
        }
        _canClose = true;
        if (ConfirmButton != null) ConfirmButton.interactable = true;
    }

    private void Update()
    {
        // Android 백키 / ESC 로도 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TryClose();
        }
    }

    // 버튼 클릭도 허용 (기존 동작 유지)
    private void OnClickConfirm()
    {
        TryClose();
    }

    // === 여기부터 어떤 터치/드래그 입력이든 닫기 시도 ===
    public void OnPointerDown(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnDrag(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryClose();
    }
    // ===============================================

    private void TryClose()
    {
        if (!_canClose) return;              // 0.5초 보호시간
        IsModalPause = false;                // 모달 해제

        // 타임스케일 복구
        Time.timeScale = 1f;

        Destroy(gameObject);
    }
    private void EnsureTopCanvas()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (GetComponentInParent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureRaycastBlocker()
    {
        // 이미 있으면 패스
        var t = transform.Find("RaycastBlocker");
        if (t != null) { _blocker = t.GetComponent<Image>(); return; }

        var go = new GameObject("RaycastBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling(); //가장 아래 자식으로 두어 창 뒤 전체를 덮게

        _blocker = go.GetComponent<Image>();
        _blocker.color = new Color(0f, 0f, 0f, 0.0f);   // 완전 투명(딤 원하면 0.4f 정도)
        _blocker.raycastTarget = true;                  // 뒤 UI 입력 차단

        // 딤 클릭으로 닫고 싶지 않다면, 아무 핸들러도 추가하지 않으면 됩니다.
        // 만약 딤 클릭으로 닫고 싶다면:
        // go.AddComponent<CloseOnClick>().Init(this); // 아래 보조 클래스를 사용
    }
}
