using UnityEngine;

/// <summary>
/// 안드로이드 뒤로가기/홈(백그라운드 전환) 등 라이프사이클에서
/// PausePopupUI(또는 PausePopupUI_Stage)를 자동으로 Open() 시켜주는 리스너.
/// 씬에 1개만 두고, DontDestroyOnLoad로 유지하는 것을 권장.
/// </summary>
public class PauseLifecycleCatcher : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("직접 참조해도 되고 비워두면 런타임에 탐색합니다.")]
    public PausePopupUI popup;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 우선 PausePopupUI_Stage가 있으면 그것을, 없으면 기본 PausePopupUI를 찾아서 사용
    /// (비활성화 객체까지 검색: FindObjectOfType(..., true))
    /// </summary>
    private PausePopupUI ResolvePopup()
    {
        if (popup == null)
        {
            // 자식 UI(스테이지 전용)를 우선
            popup = FindObjectOfType<PausePopupUI_Stage>(true);
            if (popup == null)
                popup = FindObjectOfType<PausePopupUI>(true);
        }
        return popup;
    }

    void Update()
    {
#if UNITY_ANDROID
        // 안드로이드 뒤로가기 키(Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var p = ResolvePopup();
            if (p != null)
                p.Open(); // 이미 열려있으면 내부에서 무시됨
        }
#endif
    }

    // 홈 버튼 등으로 앱이 백그라운드로 갈 때
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            var p = ResolvePopup();
            if (p != null)
                p.Open();
        }
    }

    // 포커스 잃을 때(예: 알림/다른 앱으로 전환 직전 등)도 안전하게 Pause
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            var p = ResolvePopup();
            if (p != null)
                p.Open();
        }
    }
}
