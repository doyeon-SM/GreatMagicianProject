using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class ResolutionController : MonoBehaviour
{
    [Tooltip("세로 기준 목표 비율 (예: 9:16 = 0.5625)")]
    public float targetAspect = 9f / 16f;   // 0.5625

    [Tooltip("기본 Orthographic Size (디자인 기준값)")]
    public float baseOrthoSize = 5f;

    [Tooltip("레터박스 색상")]
    public Color letterboxColor = Color.black;

    public float LetterboxScaleHeight { get; private set; } = 1f; // 1이면 레터박스 없음

    Camera cam;
    int lastW, lastH;

    void Awake()
    {
        cam = Camera.main;
        if (!cam) return;

        // 2D 기준(Orthographic). 3D면 Perspective 분기 추가 필요.
        cam.orthographic = true;

        if (baseOrthoSize <= 0f) baseOrthoSize = cam.orthographicSize;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = letterboxColor;
    }

    void Start() => ApplyViewport();

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            ApplyViewport();
    }

    void ApplyViewport()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        float currentAspect = (float)Screen.width / Screen.height;
        if (currentAspect <= 0f) return;

        Rect rect;

        if (currentAspect < targetAspect)
        {
            // 기기가 더 세로로 길쭉함(좁음): 좌우는 절대 안잘리게 -> 줌아웃
            cam.orthographicSize = baseOrthoSize * (targetAspect / currentAspect);
            rect = new Rect(0f, 0f, 1f, 1f); // 레터박스 없음
            LetterboxScaleHeight = 1f;
        }
        else
        {
            // 기기가 더 가로로 넓음: 상/하 레터박스
            cam.orthographicSize = baseOrthoSize;
            float scaleHeight = targetAspect / currentAspect; // <= 1
            float y = (1f - scaleHeight) * 0.5f;
            rect = new Rect(0f, y, 1f, scaleHeight);
            LetterboxScaleHeight = scaleHeight;
        }

        cam.rect = rect;
        lastW = Screen.width;
        lastH = Screen.height;
    }
}
