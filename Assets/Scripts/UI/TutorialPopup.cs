using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPopup : MonoBehaviour
{
    [Header("Wiring")]
    public Button fullscreenButton;
    public Image centerImage;

    private System.Action _onClosed;
    private bool _canClose = false;

    public void Show(Sprite sprite, System.Action onClosed)
    {
        _onClosed = onClosed;

        // 클릭은 버튼이 받게 하고, centerImage는 레이캐스트 차단 X
        if (centerImage != null)
        {
            centerImage.sprite = sprite;
            centerImage.raycastTarget = false;
        }

        // 버튼이 진짜로 화면을 덮도록 Rect 고정
        FixRects();

        if (fullscreenButton != null)
        {
            fullscreenButton.interactable = false;

            // 버튼 그래픽이 레이캐스트 받는지 보장
            if (fullscreenButton.targetGraphic != null)
                fullscreenButton.targetGraphic.raycastTarget = true;

            fullscreenButton.onClick.RemoveAllListeners();
            fullscreenButton.onClick.AddListener(OnClickClose);
        }

        // 버튼/부모가 레이캐스트를 막지 않도록 CanvasGroup 보정
        EnsureCanvasGroupAllowsRaycast();

        gameObject.SetActive(true);
        StartCoroutine(ForceWatchThenEnable());
    }

    private void FixRects()
    {
        // 루트/버튼 RectTransform 을 화면 풀스트레치로
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
        // 부모 어디든 CanvasGroup이 있으면 interactable/blocksRaycasts true로
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 혹시 상위에 잘못된 CanvasGroup이 있으면 보정
        var parents = GetComponentsInParent<CanvasGroup>(true);
        foreach (var p in parents)
        {
            p.interactable = true;
            p.blocksRaycasts = true;
        }
    }

    private IEnumerator ForceWatchThenEnable()
    {
        yield return new WaitForSecondsRealtime(1f); // TimeScale=0에서도 동작
        _canClose = true;
        if (fullscreenButton != null) fullscreenButton.interactable = true;
    }

    private void OnClickClose()
    {
        if (!_canClose) return;
        _onClosed?.Invoke();
        Destroy(gameObject);
    }
}
