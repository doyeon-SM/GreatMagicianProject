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

    private bool _showing = false;
    public bool IsShowing => _showing; 

    private Queue<string> _queue = new Queue<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem(); 
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(es);
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
        Time.timeScale = 0f;

        var popup = Instantiate(popupPrefab, GetTopCanvas());
        popup.Show(sprite, () =>
        {
            Time.timeScale = 1f;
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
        });
    }

    private Transform GetTopCanvas()
    {
        // 전용 튜토리얼 캔버스를 찾아 쓰고, 없으면 생성
        var existing = GameObject.Find("TutorialCanvas");
        Canvas c = null;

        if (existing == null)
        {
            var go = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 32760; // 매우 높은 순서
            DontDestroyOnLoad(go);
            return go.transform;
        }
        else
        {
            c = existing.GetComponent<Canvas>();
            if (c == null) c = existing.AddComponent<Canvas>();
            if (existing.GetComponent<GraphicRaycaster>() == null)
                existing.AddComponent<GraphicRaycaster>();

            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = Mathf.Max(c.sortingOrder, 32760); // 항상 제일 위로
            return existing.transform;
        }
    }
}
