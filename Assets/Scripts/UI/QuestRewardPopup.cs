using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestRewardPopup : MonoBehaviour
{
    public Text titleText;
    public Text rewardText;
    public float lifeSeconds = 1.0f;

    [Header("Canvas attach options")]
    [Tooltip("우선 검색할 Canvas 이름 힌트 (없어도 동작)")]
    public string canvasNameHint = "Canvas";
    [Tooltip("필요 시 팝업 RectTransform을 캔버스 중앙 기준으로 초기화")]
    public bool resetRectToCenter = true;

    private System.Action _onClosed;

    public void Setup(string questTitle, QuestReward reward, int times, System.Action onClosed)
    {
        EnsureCanvasParent();

        _onClosed = onClosed;

        if (titleText) titleText.text = $"퀘스트 완료: {questTitle}";
        if (rewardText)
        {
            int exp = reward.exp * times;
            int gold = reward.gold * times;
            int dust = reward.skillDust * times;
            rewardText.text = $"보상: EXP {exp}, GOLD {gold}, DUST {dust}";
        }

        StartCoroutine(AutoClose());
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(lifeSeconds);
        _onClosed?.Invoke();
        Destroy(gameObject);
    }

    // === 여기부터 추가 ===
    private void EnsureCanvasParent()
    {
        // 이미 Canvas 하위면 패스
        if (GetComponentInParent<Canvas>() != null)
            return;

        Canvas target = null;

        // 1) 이름 힌트로 먼저 시도
        if (!string.IsNullOrEmpty(canvasNameHint))
        {
            var hinted = GameObject.Find(canvasNameHint);
            if (hinted) target = hinted.GetComponent<Canvas>();
        }

        // 2) 씬의 Canvas들 검색 (Overlay/Camera 우선)
        if (target == null)
        {
            var canvases = FindObjectsOfType<Canvas>(true);
            // 활성 + 비-WorldSpace 우선
            foreach (var c in canvases)
            {
                if (c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace)
                {
                    target = c;
                    break;
                }
            }
            // 그래도 없으면 아무 Canvas나
            if (target == null && canvases.Length > 0)
                target = canvases[0];
        }

        // 3) 정말 없으면 생성
        if (target == null)
        {
            var root = new GameObject("Canvas");
            target = root.AddComponent<Canvas>();
            target.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            // EventSystem도 없으면 생성
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        // 4) 부모로 붙이고 RectTransform 정리
        transform.SetParent(target.transform, worldPositionStays: false);

        var rt = transform as RectTransform;
        if (rt != null && resetRectToCenter)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
