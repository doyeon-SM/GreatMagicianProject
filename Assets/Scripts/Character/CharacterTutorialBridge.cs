using System.Collections.Generic;

/// <summary>
/// Save/Load용으로 TutorialDatabase와 상호작용하는 얇은 브릿지.
/// 더 이상 내부 캐시를 갖지 않으며, 모든 상태는 TutorialDatabase(entries[i].clear)에 존재.
/// </summary>
public static class CharacterTutorialBridge
{
    /// <summary>
    /// 현재 클리어된 튜토리얼 키 전체를 가져온다.
    /// Save 시 사용.
    /// </summary>
    public static IEnumerable<string> GetAll()
    {
        if (TutorialManager.Instance == null || TutorialManager.Instance.database == null)
            return System.Array.Empty<string>();

        return TutorialManager.Instance.database.GetClearedKeys();
    }

    /// <summary>
    /// 클리어된 튜토리얼 키 목록을 DB에 반영한다.
    /// Load 시 사용.
    /// </summary>
    public static void SetAll(IEnumerable<string> keys)
    {
        if (TutorialManager.Instance == null || TutorialManager.Instance.database == null)
            return;

        TutorialManager.Instance.database.ApplyClearedKeys(keys);
    }

    /// <summary>
    /// 특정 튜토리얼 키를 클리어 처리한다.
    /// 런타임에서 팝업 종료 시 사용.
    /// </summary>
    public static void MarkAsSeen(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (TutorialManager.Instance == null || TutorialManager.Instance.database == null) return;

        TutorialManager.Instance.database.SetCleared(key, true);
    }

    /// <summary>
    /// 모든 튜토리얼 클리어 상태를 초기화한다(전부 미클리어).
    /// 옵션/디버그용.
    /// </summary>
    public static void ResetAll()
    {
        if (TutorialManager.Instance == null || TutorialManager.Instance.database == null) return;

        // 빈 목록을 적용하면 모든 entry.clear가 false가 되도록 구현되어 있음
        TutorialManager.Instance.database.ApplyClearedKeys(System.Array.Empty<string>());
    }
}
