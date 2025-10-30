using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    private AudioSource bgmSource;
    [Tooltip("현재 재생 중인 BGM 클립")]
    public AudioClip currentClip;

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

        // SoundSettingsManager에서 MixerGroup 연결
        if (SoundSettingsManager.Instance != null)
        {
            var bgmGroup = SoundSettingsManager.Instance.GetBGMGroup();
            SoundSettingsManager.Instance.ConfigureSourceToGroup(bgmSource, bgmGroup, is2D: true);
        }
    }

    /// <summary>
    /// 지정된 BGM으로 전환 (즉시)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (currentClip == clip && bgmSource.isPlaying) return; // 같은 곡이면 무시
        currentClip = clip;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>
    /// 페이드로 전환
    /// </summary>
    public void PlayBGMWithFade(AudioClip clip, float fadeTime = 1.5f)
    {
        StartCoroutine(CoFadeBGM(clip, fadeTime));
    }

    private System.Collections.IEnumerator CoFadeBGM(AudioClip newClip, float fadeTime)
    {
        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }
            bgmSource.Stop();
        }

        bgmSource.clip = newClip;
        bgmSource.Play();

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }

        bgmSource.volume = 1f;
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        currentClip = null;
    }

    public bool IsPlaying => bgmSource.isPlaying;
}
