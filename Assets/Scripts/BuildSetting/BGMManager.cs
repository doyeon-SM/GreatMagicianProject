using UnityEngine;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource bgmSource;
    [Tooltip("현재 재생 중인 BGM 클립")]
    public AudioClip currentClip;

    // 전역 Mixer 연결 캐시
    private AudioMixerGroup _bgmGroup;

    // per-scene 볼륨(0~1): 페이드 인 타겟 & 평상시 소스 볼륨
    private float _sceneVolumeMultiplier = 1f;

    // 진행 중인 페이드 코루틴
    private Coroutine _fadeCo;

    public AudioSource Source => bgmSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 오디오소스 세팅
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D

        TryBindToMixer(); // Awake 시도 1차
    }

    private void Start()
    {
        // Awake 순서 이슈 대비, Start에서 재시도
        TryBindToMixer();
        // per-scene 기본값 보정
        ApplySceneVolumeInstant();
    }
    private void OnEnable()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied += TryBindToMixer;
    }

    private void OnDisable()
    {
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied -= TryBindToMixer;
    }

    public void TryBindToMixer()
    {
        if (SoundSettingsManager.Instance == null) return;
        var bgmGroup = SoundSettingsManager.Instance.GetBGMGroup();
        if (bgmGroup != null && _bgmGroup != bgmGroup)
        {
            _bgmGroup = bgmGroup;
            SoundSettingsManager.Instance.ConfigureSourceToGroup(bgmSource, _bgmGroup, is2D: true);
        }
    }

    /// <summary>씬별 볼륨(0~1) 보정값 설정. 페이드 타겟으로 사용.</summary>
    public void SetSceneVolumeMultiplier(float m, bool applyInstant = false)
    {
        _sceneVolumeMultiplier = Mathf.Clamp01(m);
        if (applyInstant) ApplySceneVolumeInstant();
    }

    private void ApplySceneVolumeInstant()
    {
        // 전역 Mixer 볼륨은 Mixer에서 처리되므로, 여기서는 per-scene 곱만 적용
        bgmSource.volume = _sceneVolumeMultiplier;
    }

    /// <summary>즉시 전환</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // 같은 곡이 이미 재생 중이면 무시
        if (currentClip == clip && bgmSource.isPlaying) return;

        currentClip = clip;
        bgmSource.Stop();
        bgmSource.clip = clip;
        ApplySceneVolumeInstant();
        bgmSource.Play();
    }

    /// <summary>페이드로 전환</summary>
    public void PlayBGMWithFade(AudioClip clip, float fadeTime = 1.5f)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFadeBGM(clip, Mathf.Max(0f, fadeTime)));
    }

    private System.Collections.IEnumerator CoFadeBGM(AudioClip newClip, float fadeTime)
    {
        // 페이드 아웃
        if (bgmSource.isPlaying && fadeTime > 0f)
        {
            float startVol = bgmSource.volume;             // 현재 소스 볼륨(= scene multiplier 반영)
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                float k = 1f - (t / fadeTime);
                bgmSource.volume = startVol * k;
                yield return null;
            }
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }
        else
        {
            bgmSource.Stop();
            bgmSource.volume = 0f;
        }

        // 클립 교체 & 페이드 인 (타겟은 per-scene multiplier)
        currentClip = newClip;
        bgmSource.clip = newClip;
        if (newClip != null) bgmSource.Play();

        float target = _sceneVolumeMultiplier;  // << 포인트: 1이 아니라 per-scene 값
        if (newClip != null && fadeTime > 0f)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                float k = t / fadeTime;
                bgmSource.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }
        }
        bgmSource.volume = target;
        _fadeCo = null;
    }

    public void StopBGM()
    {
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
        bgmSource.Stop();
        currentClip = null;
    }

    public bool IsPlaying => bgmSource.isPlaying;
}
