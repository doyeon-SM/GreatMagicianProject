using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    void Awake()
    {
        // 화면이 꺼지지 않게 방지 (선택 사항)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // VSync 비활성화
        QualitySettings.vSyncCount = 0;

        // 프레임 고정
        Application.targetFrameRate = 60;
    }
}
