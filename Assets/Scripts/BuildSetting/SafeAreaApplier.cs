using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaApplier : MonoBehaviour
{
    public ResolutionController rc; // 씬의 ResolutionController를 드래그해서 할당

    RectTransform rt;
    Rect lastSafe;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (lastSafe != Screen.safeArea) Apply();
    }

    void Apply()
    {
        Rect safe = Screen.safeArea;
        lastSafe = safe;

        // 카메라 레터박스(상/하) 픽셀 보정
        float scaleH = (rc != null) ? rc.LetterboxScaleHeight : 1f;
        float barPixels = (Screen.height * (1f - scaleH)) * 0.5f; // 위/아래 각각

        // 위아래 바 영역을 안전영역에서 제외
        float yMin = Mathf.Max(safe.yMin, barPixels);
        float yMax = Mathf.Min(safe.yMax, Screen.height - barPixels);

        Vector2 anchorMin = new Vector2(safe.xMin / Screen.width, yMin / Screen.height);
        Vector2 anchorMax = new Vector2(safe.xMax / Screen.width, yMax / Screen.height);

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
