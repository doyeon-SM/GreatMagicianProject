using UnityEngine;

public class PauseOpener : MonoBehaviour
{
    [SerializeField] private PausePopupUI pausePopup;

    public void OnClickPause()
    {
        if (pausePopup != null)
        {
            pausePopup.Open();
        }
        else
        {
            Debug.LogWarning("[PauseOpener] PausePopupUI 참조가 없습니다. 씬의 인스턴스를 연결하세요.");
        }
    }
}
