using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ResetConfirmPopup : MonoBehaviour
{
    public Button confirmButton;
    public Button cancelButton;

    private CanvasGroup _cg;
    private Action _onConfirm;
    private Action _onCancel;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (confirmButton) confirmButton.onClick.AddListener(OnClickConfirm);
        if (cancelButton) cancelButton.onClick.AddListener(OnClickCancel);
        HideImmediate();
    }

    public void Show(Action onConfirm, Action onCancel = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        gameObject.SetActive(true);
        _cg.alpha = 1f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true;
        Time.timeScale = 0f; // 모달 느낌(게임 일시 정지)
    }

    public void HideImmediate()
    {
        _cg = _cg ?? GetComponent<CanvasGroup>();
        _cg.alpha = 0f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;
        gameObject.SetActive(false);
        Time.timeScale = 1f; // 해제
    }

    private void OnClickConfirm()
    {
        HideImmediate();
        _onConfirm?.Invoke();
        _onConfirm = null;
        _onCancel = null;
    }

    private void OnClickCancel()
    {
        HideImmediate();
        _onCancel?.Invoke();
        _onConfirm = null;
        _onCancel = null;
    }
}
