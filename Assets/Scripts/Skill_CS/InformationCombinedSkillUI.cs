using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class InformationCombinedSkillUI : MonoBehaviour,
    IPointerDownHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsModalPause = false;

    [Header("Wiring (assign in prefab)")]
    public Image SkillIconImage;
    public Text SkillNameText;
    public Button ConfirmButton;

    private float _prevTimeScale = 1f;
    private bool _canClose = false;

    private void Awake()
    {
        if (ConfirmButton != null) ConfirmButton.interactable = false;
        if (ConfirmButton != null) ConfirmButton.onClick.AddListener(OnClickConfirm);
    }

    public void Setup(string skillName, Sprite skillIcon, float prevTimeScale)
    {
        _prevTimeScale = Mathf.Max(0f, prevTimeScale);

        if (SkillNameText != null)
            SkillNameText.text = string.IsNullOrEmpty(skillName) ? "Unknown Skill" : skillName;

        if (SkillIconImage != null)
            SkillIconImage.sprite = skillIcon;

        // 모달 일시정지 진입
        IsModalPause = true;

        // 0.5초 ‘실시간’ 대기 후 버튼/닫기 허용
        StartCoroutine(EnableCloseAfterDelayRealtime(0.5f));
    }

    private IEnumerator EnableCloseAfterDelayRealtime(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime; // 일시정지 중이므로 unscaled
            yield return null;
        }
        _canClose = true;
        if (ConfirmButton != null) ConfirmButton.interactable = true;
    }

    private void Update()
    {
        // Android 백키 / ESC 로도 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TryClose();
        }
    }

    // 버튼 클릭도 허용 (기존 동작 유지)
    private void OnClickConfirm()
    {
        TryClose();
    }

    // === 여기부터 어떤 터치/드래그 입력이든 닫기 시도 ===
    public void OnPointerDown(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnDrag(PointerEventData eventData)
    {
        TryClose();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryClose();
    }
    // ===============================================

    private void TryClose()
    {
        if (!_canClose) return;              // 0.5초 보호시간
        IsModalPause = false;                // 모달 해제

        // 타임스케일 복구
        Time.timeScale = 1f;

        Destroy(gameObject);
    }
}
