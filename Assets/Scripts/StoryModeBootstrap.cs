using System.Collections;
using UnityEngine;

public class StoryModeBootstrap : MonoBehaviour
{
    public StoryStageAssetResolver resolver;
    [Tooltip("체크포인트 무시하고 이 스테이지로 강제 시작하고 싶을 때 할당")]
    public StoryStageAsset overrideStage;
    public bool startFromCheckpoint = true;

    private IEnumerator Start()
    {
        // 한 프레임 대기: 씬 내 오브젝트들이 Awake/Start 완료되도록
        yield return null;

        var sm = StoryModeManager.Instance;
        if (sm == null)
        {
            Debug.LogError("[StoryModeBootstrap] StoryModeManager 인스턴스를 찾을 수 없습니다. Loby에서 진입했나요?");
            yield break;
        }

        if (!startFromCheckpoint && overrideStage != null)
        {
            sm.StartStoryStage(overrideStage);
        }
        else
        {
            sm.StartFromLastCheckpoint(resolver); // 내부에서 resolver.Resolve(lastCheckpointStageId)
        }

        // 스토리 클리어 결과창은 StoryModeManager에서 띄움.
    }
}
