using UnityEngine;
using UnityEngine.UI;

public class LobyUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button gameStartButton;          // 시작 버튼
    [SerializeField] private CanvasGroup gameStartButtonCG;   // 반투명 제어용(없으면 자동 추가)
    [Tooltip("이 스테이지를 '넘었을 때' 활성화됩니다. (예: 1-1을 넘으면 활성)")]
    [SerializeField] private string thresholdStageId = "0-4";

    private void Awake()
    {
        if (gameStartButton == null)
        {
            gameStartButton = GetComponentInChildren<Button>(true);
        }
        if (gameStartButtonCG == null && gameStartButton != null)
        {
            gameStartButtonCG = gameStartButton.GetComponent<CanvasGroup>();
            if (gameStartButtonCG == null)
                gameStartButtonCG = gameStartButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        // 혹시 Start 타이밍에 StoryModeManager가 준비되는 프로젝트면 Start에서도 한 번 더
        Refresh();
    }

    public void Refresh()
    {
        string maxId = GetMaxClearedStageId();    
        bool enable = IsBeyond(maxId, thresholdStageId);
        ApplyButtonState(enable);
    }

    private void ApplyButtonState(bool enable)
    {
        if (gameStartButton != null)
        {
            gameStartButton.interactable = enable;
        }
        if (gameStartButtonCG != null)
        {
            gameStartButtonCG.alpha = enable ? 1f : 0.5f;   // 활성: 불투명, 비활성: 반투명
            gameStartButtonCG.blocksRaycasts = true;        // 버튼은 항상 레이캐스트 받게(상태는 interactable로 제어)
        }
    }

    // StoryModeManager에서 마지막 체크포인트 스테이지 ID 가져오기
    private string GetMaxClearedStageId()
    {
        var sm = StoryModeManager.Instance;
        if (sm != null)
        {
            // 신규 필드가 비었거나 초기값이면 최근 체크포인트로 폴백
            string maxId = sm.maxClearedStageId;
            if (string.IsNullOrEmpty(maxId) || maxId == "0-0")
                maxId = sm.lastCheckpointStageId;

            return string.IsNullOrEmpty(maxId) ? "0-1" : maxId;
        }
        // 매니저가 아직 없으면 안전 기본값
        return "0-1";
    }

    /// <summary>
    /// a가 b를 '넘었는지' 판정 (예: a=1-2, b=1-1 => true / a=1-1 => false)
    /// 포맷: "chapter-stage" 형태 가정. Resolver 순서가 숫자 순서와 동일하다는 전제.
    /// </summary>
    private bool IsBeyond(string a, string b)
    {
        (int am, int asub) = ParseStageId(a);
        (int bm, int bsub) = ParseStageId(b);

        if (am != bm) return am > bm;
        return asub >= bsub;
    }

    private (int major, int minor) ParseStageId(string id)
    {
        if (string.IsNullOrEmpty(id)) return (0, 0);

        // 허용 포맷: "1-1" / "01-02" 등
        var parts = id.Split('-');
        if (parts.Length != 2) return (0, 0);

        int major = 0, minor = 0;
        int.TryParse(parts[0], out major);
        int.TryParse(parts[1], out minor);
        return (major, minor);
    }
}
