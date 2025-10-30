using UnityEngine;
using UnityEngine.Audio;

public class SoundSettingsManager : MonoBehaviour
{
    public static SoundSettingsManager Instance { get; private set; }

    [Header("Mixer & Groups")]
    [SerializeField] private AudioMixer gameMixer; // GameMixer
    [SerializeField] private string masterParam = "Master_Volume";
    [SerializeField] private string bgmParam = "BGM_Volume";
    [SerializeField] private string sfxParam = "SFX_Volume";
    [SerializeField] private string skillParam = "SkillSFX_Volume";

    [Header("Mixer Groups (assign in Inspector)")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup skillSfxGroup;

    // PlayerPrefs 키
    private const string KEY_MASTER = "Sound.Master";
    private const string KEY_BGM = "Sound.BGM";
    private const string KEY_SFX = "Sound.SFX";
    private const string KEY_SKILL = "Sound.SkillSFX";

    // 0~1 (내부 저장은 0~1), 기본값 1.0f (== 100%)
    public float Master { get; private set; } = 1f;
    public float BGM { get; private set; } = 1f;
    public float SFX { get; private set; } = 1f;
    public float SKILL { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyAllToMixer();
    }

    // ===== Public API (퍼센트 단위 0~100 입력 편의) =====
    public void SetMasterSoundPercent(float percent) { SetMasterSound(Mathf.Clamp01(percent / 100f)); }
    public void SetBGMPercent(float percent) { SetBGM(Mathf.Clamp01(percent / 100f)); }
    public void SetSFXPercent(float percent) { SetSFX(Mathf.Clamp01(percent / 100f)); }
    public void SetSkillPercent(float percent) { SetSkill(Mathf.Clamp01(percent / 100f)); }

    public float GetMasterSoundPercent() => Mathf.RoundToInt(Master * 100f);
    public float GetBGMPercent() => Mathf.RoundToInt(BGM * 100f);
    public float GetSFXPercent() => Mathf.RoundToInt(SFX * 100f);
    public float GetSkillPercent() => Mathf.RoundToInt(SKILL * 100f);

    // ===== 내부 실제 세터 (0~1) =====
    public void SetMasterSound(float v) { Master = Mathf.Clamp01(v); SetMixer(masterParam, Master); Save(); }
    public void SetBGM(float v) { BGM = Mathf.Clamp01(v); SetMixer(bgmParam, BGM); Save(); }
    public void SetSFX(float v) { SFX = Mathf.Clamp01(v); SetMixer(sfxParam, SFX); Save(); }
    public void SetSkill(float v) { SKILL = Mathf.Clamp01(v); SetMixer(skillParam, SKILL); Save(); }

    public void SetSkillSFXVolume(float linear01) => SetSkill(linear01);

    // ===== 저장/불러오기 =====
    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, Master);
        PlayerPrefs.SetFloat(KEY_BGM, BGM);
        PlayerPrefs.SetFloat(KEY_SFX, SFX);
        PlayerPrefs.SetFloat(KEY_SKILL, SKILL);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        // 존재하지 않으면 기본 1.0 (=100%)
        Master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        BGM = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        SFX = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        SKILL = PlayerPrefs.GetFloat(KEY_SKILL, 1f);
    }

    public void ApplyAllToMixer()
    {
        SetMixer(masterParam, Master);
        SetMixer(bgmParam, BGM);
        SetMixer(sfxParam, SFX);
        SetMixer(skillParam, SKILL);
    }

    // ===== Group Getters =====
    public AudioMixerGroup GetBGMGroup() => bgmGroup;
    public AudioMixerGroup GetSFXGroup() => sfxGroup;
    public AudioMixerGroup GetSkillSFXGroup() => skillSfxGroup;

    // ===== Source 라우팅 유틸 =====
    public void ConfigureSourceToGroup(AudioSource src, AudioMixerGroup group, bool is2D = true)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.outputAudioMixerGroup = group;
        src.spatialBlend = is2D ? 0f : 1f;
    }

    // ===== 유틸 =====
    private void SetMixer(string exposedParam, float linear01)
    {
        if (gameMixer == null || string.IsNullOrEmpty(exposedParam)) return;

        // 0은 -80dB로 취급 (Mute에 가까운 값), 그 외는 20*log10
        float dB = (linear01 <= 0.0001f) ? -80f : Mathf.Log10(linear01) * 20f;
        gameMixer.SetFloat(exposedParam, dB);
    }
}
