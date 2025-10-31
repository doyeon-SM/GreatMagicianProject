using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [Header("기본 설정")]
    public AudioClip defaultClip;   // 기본 버튼 클릭 사운드
    public AudioClip overrideClip;  // 일부 버튼만 다르게 설정할 때 사용
    [Range(0f, 1f)] public float volume = 1f;

    private Button button;
    private AudioSource _sfxSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);

        // 공통 SFX 오디오소스 가져오기 (SoundSettingsManager로부터)
        if (SoundSettingsManager.Instance != null)
        {
            _sfxSource = SoundSettingsManager.Instance.CreateSFXSource(gameObject);
        }
        else
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }

        _sfxSource.playOnAwake = false;
    }

    private void PlayClickSound()
    {
        AudioClip clipToPlay = overrideClip != null ? overrideClip : defaultClip;
        if (clipToPlay != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(clipToPlay, volume);
        }
    }
}
