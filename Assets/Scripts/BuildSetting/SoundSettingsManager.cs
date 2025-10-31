using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)] 
public class SoundSettingsManager : MonoBehaviour
{
    public static SoundSettingsManager Instance { get; private set; }

    public event System.Action VolumesApplied; 

    [Header("Mixer & Groups")]
    [SerializeField] private AudioMixer gameMixer;
    [SerializeField] private string masterParam = "Master_Volume";
    [SerializeField] private string bgmParam = "BGM_Volume";
    [SerializeField] private string sfxParam = "SFX_Volume";
    [SerializeField] private string skillParam = "SkillSFX_Volume";

    [Header("Group Names for routing (by name search)")]
    [SerializeField] private string bgmGroupName = "BGM";
    [SerializeField] private string sfxGroupName = "SFX";
    [SerializeField] private string skillGroupName = "SkillSFX";

    [Header("Mixer Groups (optional, auto-filled if empty)")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup skillSfxGroup;

    private const string KEY_MASTER = "Sound.Master";
    private const string KEY_BGM = "Sound.BGM";
    private const string KEY_SFX = "Sound.SFX";
    private const string KEY_SKILL = "Sound.SkillSFX";

    public float Master { get; private set; } = 1f;
    public float BGM { get; private set; } = 1f;
    public float SFX { get; private set; } = 1f;
    public float SKILL { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ForceRebindGroups();  
        Load();               
        ApplyAllToMixer();    
        VolumesApplied?.Invoke();
        StartCoroutine(CoReapplyVolumesNextFrame());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ForceRebindGroups();   // 혹시 새 씬에서 그룹이 끊기지 않게
        ApplyAllToMixer();     // 저장값 다시 Mixer에 재적용
        VolumesApplied?.Invoke();
    }
    private void OnAudioConfigChanged(bool deviceWasChanged)
    {
        
        ApplyAllToMixer();
        VolumesApplied?.Invoke();
        StartCoroutine(CoReapplyVolumesNextFrame());
    }
    private System.Collections.IEnumerator CoReapplyVolumesNextFrame()
    {
        yield return null; // 1프레임 대기
        ApplyAllToMixer();
        VolumesApplied?.Invoke();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ForceRebindGroups();
        ApplyAllToMixer();
        VolumesApplied?.Invoke();
    }

    // --- 퍼센트 API (0~100) ---
    public void SetMasterSoundPercent(float p) => SetMasterSound(Mathf.Clamp01(p / 100f));
    public void SetBGMPercent(float p) => SetBGM(Mathf.Clamp01(p / 100f));
    public void SetSFXPercent(float p) => SetSFX(Mathf.Clamp01(p / 100f));
    public void SetSkillPercent(float p) => SetSkill(Mathf.Clamp01(p / 100f));

    public float GetMasterSoundPercent() => Mathf.RoundToInt(Master * 100f);
    public float GetBGMPercent() => Mathf.RoundToInt(BGM * 100f);
    public float GetSFXPercent() => Mathf.RoundToInt(SFX * 100f);
    public float GetSkillPercent() => Mathf.RoundToInt(SKILL * 100f);

    // --- 내부 세터 (0~1) : 저장+즉시적용+알림 ---
    public void SetMasterSound(float v) { Master = Mathf.Clamp01(v); SetMixer(masterParam, Master); Save(); VolumesApplied?.Invoke(); }
    public void SetBGM(float v) { BGM = Mathf.Clamp01(v); SetMixer(bgmParam, BGM); Save(); VolumesApplied?.Invoke(); }
    public void SetSFX(float v) { SFX = Mathf.Clamp01(v); SetMixer(sfxParam, SFX); Save(); VolumesApplied?.Invoke(); }
    public void SetSkill(float v) { SKILL = Mathf.Clamp01(v); SetMixer(skillParam, SKILL); Save(); VolumesApplied?.Invoke(); }

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

    public AudioMixerGroup GetBGMGroup() => bgmGroup;
    public AudioMixerGroup GetSFXGroup() => sfxGroup;
    public AudioMixerGroup GetSkillSFXGroup() => skillSfxGroup;

    public void ConfigureSourceToGroup(AudioSource src, AudioMixerGroup group, bool is2D = true)
    {
        if (!src || !group) return;
        src.playOnAwake = false;
        src.outputAudioMixerGroup = group;
        src.spatialBlend = is2D ? 0f : 1f;
    }

    public void ForceRebindGroups()
    {
        if (gameMixer == null) return;

        if (bgmGroup == null)
        {
            var g = gameMixer.FindMatchingGroups(string.IsNullOrEmpty(bgmGroupName) ? "BGM" : bgmGroupName);
            if (g != null && g.Length > 0) bgmGroup = g[0];
        }
        if (sfxGroup == null)
        {
            var g = gameMixer.FindMatchingGroups(string.IsNullOrEmpty(sfxGroupName) ? "SFX" : sfxGroupName);
            if (g != null && g.Length > 0) sfxGroup = g[0];
        }
        if (skillSfxGroup == null)
        {
            var g = gameMixer.FindMatchingGroups(string.IsNullOrEmpty(skillGroupName) ? "SkillSFX" : skillGroupName);
            if (g != null && g.Length > 0) skillSfxGroup = g[0];
        }
    }

    private void SetMixer(string exposedParam, float linear01)
    {
        if (gameMixer == null || string.IsNullOrEmpty(exposedParam)) return;
        float dB = (linear01 <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;
        gameMixer.SetFloat(exposedParam, dB);
    }
}
