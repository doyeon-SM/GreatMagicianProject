using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIUtilities
{
    /// <summary>
    /// 씬에서 Screen Space - Camera 캔버스를 찾습니다.
    /// cam이 지정되면 해당 카메라를 사용하는 캔버스를 우선 반환합니다.
    /// </summary>
    public static Canvas FindCameraCanvas(Camera cam = null, bool includeInactive = true)
    {
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(c =>
            {
                if (!includeInactive && !c.gameObject.activeInHierarchy) return false;
                return c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != null;
            });

        if (cam == null) cam = Camera.main;

        // 1순위: 지정 카메라/메인카메라를 쓰는 캔버스
        var match = canvases.FirstOrDefault(c => c.worldCamera == cam);
        if (match != null) return match;

        // 2순위: 아무 카메라나 쓰는 Screen Space - Camera 캔버스
        return canvases.FirstOrDefault();
    }

    /// <summary>
    /// Screen Space - Camera 캔버스를 보장합니다. 없으면 새로 생성합니다.
    /// </summary>
    public static Canvas EnsureCameraCanvas(Camera cam = null, int sortingOrder = 0)
    {
        var found = FindCameraCanvas(cam);
        if (found != null) return found;

        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            // 메인 카메라가 없다면 씬의 임의 카메라라도 찾기
            cam = Object.FindObjectOfType<Camera>();
        }

        var go = new GameObject("UICanvas (Camera)");
        var canvas = go.AddComponent<Canvas>();
        var scaler = go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.sortingOrder = sortingOrder; // 필요 시 오더 조정

        // 보편적 스케일러 설정
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;     // 가로/세로 중간값
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        EnsureEventSystem();

        return canvas;
    }

    /// <summary>
    /// EventSystem이 없으면 생성합니다.
    /// </summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// UI 프리팹(GameObject)을 카메라 캔버스 아래에 인스턴스화합니다.
    /// parentOverride가 주어지면 해당 트랜스폼의 상위에서 캔버스를 찾고, 없으면 보장 생성합니다.
    /// </summary>
    public static GameObject SpawnUI(GameObject prefab, Transform parentOverride = null, Camera cam = null, int sortingOrder = 0)
    {
        Canvas canvas = null;

        if (parentOverride != null)
        {
            canvas = parentOverride.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
                canvas = EnsureCameraCanvas(cam, sortingOrder);
        }
        else
        {
            canvas = EnsureCameraCanvas(cam, sortingOrder);
        }

        var go = Object.Instantiate(prefab, canvas.transform, false);

        // RectTransform 초기화(앵커 중앙, 위치 0)
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        return go;
    }

    /// <summary>
    /// 컴포넌트 타입 프리팹을 카메라 캔버스 아래에 인스턴스화하고 해당 컴포넌트를 반환합니다.
    /// </summary>
    public static T SpawnUI<T>(T prefab, Transform parentOverride = null, Camera cam = null, int sortingOrder = 0) where T : Component
    {
        var go = SpawnUI(prefab.gameObject, parentOverride, cam, sortingOrder);
        return go.GetComponent<T>();
    }
}
