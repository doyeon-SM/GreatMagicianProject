using UnityEngine;
using UnityEngine.UI;

public class PauseSoundSliders : MonoBehaviour
{
    [Header("Assign in Inspector (0~100%)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider skillSlider;

    [Header("Optional % Texts (UnityEngine.UI.Text)")]
    [SerializeField] private Text masterPercentText;
    [SerializeField] private Text bgmPercentText;
    [SerializeField] private Text sfxPercentText;
    [SerializeField] private Text skillPercentText;

    private bool _bound;
    private Coroutine _waitCo;

    private void Awake()
    {
        if(masterSlider) { masterSlider.minValue = 0; masterSlider.maxValue = 100; masterSlider.wholeNumbers = true; }
        if (bgmSlider) { bgmSlider.minValue = 0; bgmSlider.maxValue = 100; bgmSlider.wholeNumbers = true; }
        if (sfxSlider) { sfxSlider.minValue = 0; sfxSlider.maxValue = 100; sfxSlider.wholeNumbers = true; }
        if (skillSlider) { skillSlider.minValue = 0; skillSlider.maxValue = 100; skillSlider.wholeNumbers = true; }
    }

    private void Start()
    {
        EnsureBound();
        PushSavedValuesToUI();
        UpdateTexts();
    }

    private void OnEnable()
    {
        EnsureBound();

        if (SoundSettingsManager.Instance != null)
        {
            SoundSettingsManager.Instance.VolumesApplied += PushSavedValuesToUI;
            PushSavedValuesToUI(); 
            UpdateTexts();
        }
        else
        {
            _waitCo = StartCoroutine(CoWaitAndSync());
        }
    }

    private void OnDisable()
    {
        RemoveListeners();
        _bound = false;

        if (_waitCo != null) { StopCoroutine(_waitCo); _waitCo = null; }
        if (SoundSettingsManager.Instance != null)
            SoundSettingsManager.Instance.VolumesApplied -= PushSavedValuesToUI;
    }
    private System.Collections.IEnumerator CoWaitAndSync()
    {
        while (SoundSettingsManager.Instance == null) yield return null;
        SoundSettingsManager.Instance.VolumesApplied += PushSavedValuesToUI;
        PushSavedValuesToUI();
        UpdateTexts();
        _waitCo = null;
    }

    private void Update()
    {
        UpdateTexts();
    }

    private void EnsureBound()
    {
        if (_bound) return;

        RemoveListeners();

        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        if (skillSlider) skillSlider.onValueChanged.AddListener(OnSkillChanged);

        _bound = true;
    }

    private void RemoveListeners()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (bgmSlider) bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        if (sfxSlider) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
        if (skillSlider) skillSlider.onValueChanged.RemoveListener(OnSkillChanged);
    }

    private void PushSavedValuesToUI()
    {
        var sm = SoundSettingsManager.Instance;
        if (sm == null) return;

        if (masterSlider) masterSlider.SetValueWithoutNotify(sm.GetMasterSoundPercent());
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(sm.GetBGMPercent());
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sm.GetSFXPercent());
        if (skillSlider) skillSlider.SetValueWithoutNotify(sm.GetSkillPercent());

        UpdateTexts();
    }

    private void OnMasterChanged(float v)
    {
        SoundSettingsManager.Instance?.SetMasterSoundPercent(v);
        UpdateTexts();
    }

    private void OnBGMChanged(float v)
    {
        SoundSettingsManager.Instance?.SetBGMPercent(v);
        UpdateTexts();
    }

    private void OnSFXChanged(float v)
    {
        SoundSettingsManager.Instance?.SetSFXPercent(v);
        UpdateTexts();
    }

    private void OnSkillChanged(float v)
    {
        SoundSettingsManager.Instance?.SetSkillPercent(v);
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if(masterPercentText && masterSlider)
            masterPercentText.text = $"{Mathf.RoundToInt(masterSlider.value)}%";

        if (bgmPercentText && bgmSlider)
            bgmPercentText.text = $"{Mathf.RoundToInt(bgmSlider.value)}%";

        if (sfxPercentText && sfxSlider)
            sfxPercentText.text = $"{Mathf.RoundToInt(sfxSlider.value)}%";

        if (skillPercentText && skillSlider)
            skillPercentText.text = $"{Mathf.RoundToInt(skillSlider.value)}%";
    }
}
