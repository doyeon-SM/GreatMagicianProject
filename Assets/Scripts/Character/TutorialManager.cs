using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Database & Prefab")]
    public TutorialDatabase database;
    public TutorialPopup popupPrefab;

    [Header("UI")]
    public Camera uiCamera;           // 비워두면 MainCamera 사용
    [Tooltip("튜토리얼 전용 레이어 정렬 순서(높을수록 위)")]
    public int sortingOrder = 32760;

    private bool _showing = false;
    public bool IsShowing => _showing;

    private Queue<string> _queue = new Queue<string>();
    private Transform _parent;        // 튜토리얼 팝업들이 붙을 부모(카메라 캔버스)

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var canvas = UIUtilities.EnsureCameraCanvas(uiCamera, sortingOrder);
        _parent = canvas.transform;

        // 카메라/캔버스 보이기 보장
        var cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        if (cam != null)
        {
            // Canvas 레이어를 UI로, 카메라 CullingMask에 UI 포함
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1)
            {
                canvas.gameObject.layer = uiLayer;
                cam.cullingMask |= (1 << uiLayer);
            }
            else
            {
                // UI 레이어가 없다면, 캔버스 레이어를 카메라가 이미 그리는 레이어로 맞추세요(예: Default)
                // canvas.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            // Plane Distance를 카메라 클립 안에 맞춤
            float safePD = Mathf.Min(10f, cam.farClipPlane - 1f);           
            safePD = Mathf.Max(cam.nearClipPlane + 0.01f, safePD);          
            canvas.planeDistance = safePD;
        }

        //  Raycaster 보장(혹시 프리팹에 없을 수 있으니)
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        UIUtilities.EnsureEventSystem();
        Debug.Log($"[TutorialManager] cam={cam?.name}, near={cam?.nearClipPlane}, far={cam?.farClipPlane}, planeDistance={canvas.planeDistance}, canvasLayer={LayerMask.LayerToName(canvas.gameObject.layer)}, camMask={(cam != null ? cam.cullingMask.ToString() : "null")}");

    }
    private Transform ResolveParent()
    {
        // 씬이 바뀌었거나 기존 캔버스가 파괴되었을 수 있으니 매번 보장
        var canvas = UIUtilities.EnsureCameraCanvas(uiCamera, sortingOrder);
        return canvas.transform;
    }

    public void TryTrigger(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (_showing)
        {
            if (!_queue.Contains(key) && !database.IsCleared(key))
                _queue.Enqueue(key);
            return;
        }

        if (database.IsCleared(key)) return;

        if (database.TryGetSprite(key, out var sprite))
        {
            ShowInternal(key, sprite);
        }
    }

    private void ShowInternal(string key, Sprite sprite)
    {
        _showing = true;

        // 일시정지 관리: 첫 진입/마지막 종료로 다루고 싶다면 depth로 관리하세요.
        Time.timeScale = 0f;

        // 부모를 매번 재해결 + UIUtilities로 안전 생성
        var parent = ResolveParent();
        var popup = UIUtilities.SpawnUI(popupPrefab, parent, uiCamera, sortingOrder);

        popup.Show(sprite, () =>
        {
            database.SetCleared(key, true);
            _showing = false;

            while (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                if (!database.IsCleared(next) && database.TryGetSprite(next, out var sp))
                {
                    ShowInternal(next, sp);
                    return;
                }
            }

            // 큐 없으면 일시정지 해제
            Time.timeScale = 1f;
        });
    }
}
