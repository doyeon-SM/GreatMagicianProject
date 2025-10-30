using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneBGMTrigger : MonoBehaviour
{
    [System.Serializable]
    public class SceneBGMEntry
    {
        [Tooltip("씬 이름 (Build Settings에 등록된 이름과 정확히 일치)")]
        public string sceneName;

        [Tooltip("이 씬에서 재생할 BGM")]
        public AudioClip clip;

        [Tooltip("이 씬으로 전환될 때 사용할 페이드 시간(초)")]
        [Min(0f)] public float fadeTime = 1.5f;

        [Tooltip("이 씬에서만 적용할 BGM 볼륨 보정(0~1). 1=기본")]
        [Range(0f, 1f)] public float volumeMultiplier = 1f;
    }

    [Header("BGM 매핑")]
    [SerializeField] private List<SceneBGMEntry> entries = new();

    [Header("기본값 (매핑 실패 시 동작)")]
    [Tooltip("매핑이 없을 때 재생할 기본 BGM(없으면 아무 것도 재생 안 함)")]
    [SerializeField] private AudioClip defaultBgm;
    [Tooltip("매핑이 없을 때 기본 페이드 시간")]
    [SerializeField] private float defaultFadeTime = 1.0f;
    [Tooltip("씬 매핑이 없으면 BGM을 멈출지 여부")]
    [SerializeField] private bool stopIfUnmapped = false;

    // 내부 캐시: 씬명 -> 엔트리
    private Dictionary<string, SceneBGMEntry> map;

    private void Awake()
    {
        // 맵 빌드
        map = new Dictionary<string, SceneBGMEntry>();
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.sceneName)) continue;
            map[e.sceneName] = e; // 중복 이름 시 마지막 값이 우선
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 이미 로드된 활성 씬에 대해 즉시 1회 적용 (부트 씬에서 유용)
        var active = SceneManager.GetActiveScene();
        if (active.IsValid())
        {
            ApplyForScene(active.name);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (BGMManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGMTrigger] BGMManager 인스턴스가 없습니다. 첫 씬에 배치했는지 확인하세요.");
            return;
        }

        if (map != null && map.TryGetValue(sceneName, out var entry) && entry.clip != null)
        {
            // 볼륨 보정(엔트리 개별 보정)
            var src = GetBGMAudioSource();
            float prev = 1f;
            if (src != null) prev = src.volume;

            // 페이드 교체
            BGMManager.Instance.PlayBGMWithFade(entry.clip, entry.fadeTime);

            // 페이드 인이 끝난 다음 최종 볼륨을 보정하고 싶다면 코루틴으로 딜레이 적용하는 방식도 가능.
            // 여기서는 간단히 즉시 보정:
            if (src != null) src.volume = Mathf.Clamp01(entry.volumeMultiplier);

            return;
        }

        // 매핑 실패
        if (stopIfUnmapped)
        {
            BGMManager.Instance.StopBGM();
        }
        else if (defaultBgm != null)
        {
            BGMManager.Instance.PlayBGMWithFade(defaultBgm, defaultFadeTime);
            var src = GetBGMAudioSource();
            if (src != null) src.volume = 1f;
        }
        // else: 아무 것도 하지 않음 (현재 재생 유지)
    }

    /// <summary>
    /// BGMManager의 내부 AudioSource를 얻는다. (볼륨 보정용)
    /// </summary>
    private AudioSource GetBGMAudioSource()
    {
        // BGMManager에 public 프로퍼티가 없다면, 아래처럼 Reflection 없이 간단한 Getter를 BGMManager에 추가해서 쓰는 걸 권장.
        // 여기서는 FindObjectsOfType로 예외적으로 접근(1개만 존재 가정).
        var mgr = BGMManager.Instance;
        var src = mgr != null ? mgr.GetComponent<AudioSource>() : null;
        return src;
    }
}
