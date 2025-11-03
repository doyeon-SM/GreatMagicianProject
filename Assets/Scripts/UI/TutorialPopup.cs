using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPopup : MonoBehaviour
{
    [Header("Wiring")]
    public Button fullscreenButton;
    public Image centerImage;

    [Header("Safety")]
    [Tooltip("예외 발생 등으로 닫기 실패 시 자동 닫힘까지의 시간(Realtime)")]
    public float autoCloseSeconds = 1.0f;   // 요구사항: 1초 뒤 자동 종료

    private System.Action _onClosed;
    private bool _canClose = false;
    private bool _closed = false;
    private Coroutine _autoCloseRoutine;
    private Coroutine _watchRoutine;

    public void Show(Sprite sprite, System.Action onClosed)
    {
        _onClosed = onClosed;

        if (centerImage != null)
        {
            centerImage.sprite = sprite;
            centerImage.raycastTarget = false;
            centerImage.preserveAspect = true;
            centerImage.color = Color.white;
        }

        FixRects();

        var rtRoot = GetComponent<RectTransform>();
        if (rtRoot != null)
        {
            rtRoot.localScale = Vector3.one;
            rtRoot.localRotation = Quaternion.identity;
            rtRoot.anchoredPosition3D = Vector3.zero;
        }

        if (fullscreenButton != null)
        {
            fullscreenButton.interactable = false;
            if (fullscreenButton.targetGraphic != null)
                fullscreenButton.targetGraphic.raycastTarget = true;
            fullscreenButton.onClick.RemoveAllListeners();
            fullscreenButton.onClick.AddListener(OnClickClose);
        }

        EnsureCanvasGroupAllowsRaycast();

        gameObject.SetActive(true);

        // 1) 시청 강제 후 닫기 허용
        _watchRoutine = StartCoroutine(ForceWatchThenEnable());

        // 2) 안전장치: 어떤 이유로든 닫히지 않으면 autoCloseSeconds 뒤 자동 종료
        _autoCloseRoutine = StartCoroutine(AutoCloseSafety());
    }

    private void FixRects()
    {
        var rtRoot = GetComponent<RectTransform>();
        if (rtRoot != null)
        {
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.offsetMin = Vector2.zero;
            rtRoot.offsetMax = Vector2.zero;
        }

        if (fullscreenButton != null)
        {
            var rtBtn = fullscreenButton.GetComponent<RectTransform>();
            if (rtBtn != null)
            {
                rtBtn.anchorMin = Vector2.zero;
                rtBtn.anchorMax = Vector2.one;
                rtBtn.offsetMin = Vector2.zero;
                rtBtn.offsetMax = Vector2.zero;
            }
        }
    }

    private void EnsureCanvasGroupAllowsRaycast()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 상위 CanvasGroup을 건드리면 사고 시 입력이 막힐 수 있어,
        // 필요 없다면 굳이 모두 켜지 않도록 이 줄들은 주석 처리해도 됩니다.
        // var parents = GetComponentsInParent<CanvasGroup>(true);
        // foreach (var p in parents) { p.interactable = true; p.blocksRaycasts = true; }
    }

    private IEnumerator ForceWatchThenEnable()
    {
        yield return new WaitForSecondsRealtime(1f); // Time.timeScale=0에서도 동작
        _canClose = true;
        if (fullscreenButton != null) fullscreenButton.interactable = true;
    }

    private IEnumerator AutoCloseSafety()
    {
        // 지정된 시간 뒤에도 닫히지 않았다면 강제 닫기
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, autoCloseSeconds));
        SafeClose();
    }

    private void OnClickClose()
    {
        if (!_canClose) return;
        SafeClose();
    }

    private void SafeClose()
    {
        if (_closed) return;
        _closed = true;

        // 더 이상 입력을 막지 않도록 즉시 레이캐스트 해제
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.alpha = 0f; // 혹시 파괴 지연이 있어도 보이지 않게
        }

        // 혹시 상위에 켜둔 CanvasGroup이 있다면 여기서 비활성화(선택)
        // var parents = GetComponentsInParent<CanvasGroup>(true);
        // foreach (var p in parents) { p.interactable = false; p.blocksRaycasts = false; }

        // 걸려있는 코루틴 정리
        if (_watchRoutine != null) StopCoroutine(_watchRoutine);
        if (_autoCloseRoutine != null) StopCoroutine(_autoCloseRoutine);

        // 콜백은 예외 안전하게
        try { _onClosed?.Invoke(); }
        catch { /* 콜백 내부 예외 무시해도 안전하게 닫히도록 */ }

        // 최종 파괴
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 비활성화가 되었는데 아직 _closed 플래그가 아니라면 안전하게 닫기
        // (예: 부모가 꺼져서 보이지 않지만 레이캐스트가 남는 상황 방지)
        if (!_closed)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }
        }
    }
}
