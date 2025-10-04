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

    [Header("Parent Canvas (optional)")]
    [Tooltip("지정하면 해당 Canvas 아래로 붙습니다. 비워두면 씬의 첫 Canvas를 자동 검색합니다.")]
    public Canvas parentCanvas;

    private Image _blocker;
    private float _prevTimeScale = 1f;
    private bool _canClose = false;
    private Canvas _localCanvas;  
    private Camera _worldCam;

    private void Awake()
    {
        if (ConfirmButton != null) ConfirmButton.interactable = false;
        if (ConfirmButton != null) ConfirmButton.onClick.AddListener(OnClickConfirm);

        AttachToParentCanvas();  
        EnsureTopCanvasOnCamera();
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
    /// <summary>
    /// 부모 Canvas를 찾아 이 오브젝트를 그 아래로 붙인다.
    /// </summary>
    private void AttachToParentCanvas()
    {
        // 1) 인스펙터로 지정된 parentCanvas 우선
        if (parentCanvas == null)
        {
            // 2) 현재 트랜스폼 상위에서 Canvas 찾기
            parentCanvas = GetComponentInParent<Canvas>();
        }
        if (parentCanvas == null)
        {
            // 3) 씬 전체에서 제일 먼저 보이는 Canvas(가급적 ScreenSpaceCamera) 탐색
            var all = FindObjectsOfType<Canvas>(true);
            Canvas ssc = null;
            foreach (var c in all)
            {
                if (c.renderMode == RenderMode.ScreenSpaceCamera) { ssc = c; break; }
            }
            parentCanvas = ssc != null ? ssc : (all.Length > 0 ? all[0] : null);
        }

        if (parentCanvas == null)
        {
            Debug.LogError("[InformationCombinedSkillUI] 부모 Canvas를 찾지 못했습니다. 씬에 Canvas가 필요합니다.");
            return;
        }

        // 부모로 붙이기
        var prt = parentCanvas.transform;
        if (transform.parent != prt)
            transform.SetParent(prt, false);

        // 부모 카메라 캐시
        _worldCam = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
    }

    /// <summary>
    /// 최상단에 보이도록 이 객체에 ‘로컬 Canvas’를 설정(부모 Canvas 기준).
    /// Overlay를 새로 만들지 않고, ScreenSpace-Camera 모드 유지.
    /// </summary>
    private void EnsureTopCanvasOnCamera()
    {
        if (parentCanvas == null) return;

        // 프리팹에 Canvas가 붙어 있다면 그것을 재설정, 없으면 추가
        _localCanvas = GetComponent<Canvas>();
        if (_localCanvas == null) _localCanvas = gameObject.AddComponent<Canvas>();

        _localCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        _localCanvas.worldCamera = _worldCam != null ? _worldCam : Camera.main;
        _localCanvas.planeDistance = Mathf.Max(1f, parentCanvas.planeDistance + 0.01f); // 살짝 앞쪽
        _localCanvas.overrideSorting = true;
        _localCanvas.sortingLayerID = parentCanvas.sortingLayerID; // 부모와 동일 레이어 사용
        _localCanvas.sortingOrder = sortingOrder;                  // 원하는 최상단 순서

        // Raycaster 보장
        var ray = GetComponent<GraphicRaycaster>();
        if (ray == null) ray = gameObject.AddComponent<GraphicRaycaster>();
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
        rt.SetAsFirstSibling(); // 가장 아래에 두어 뒤 UI 전체 덮기

        _blocker = go.GetComponent<Image>();
        _blocker.color = new Color(0f, 0f, 0f, 0.0f); // 투명(딤 원하면 0.4f 정도)
        _blocker.raycastTarget = true;                // 뒤 UI 입력 차단
    }
}
