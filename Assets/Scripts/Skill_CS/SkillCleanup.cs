using UnityEngine;

public static class SkillWorldCleanup
{
    /// <summary>
    /// 태그 "skill" / "create" 오브젝트를 전부 삭제한다.
    /// 어디서든 호출 가능: SkillWorldCleanup.Run();
    /// </summary>
    public static void Run()
    {
        int removed = 0;

        var skills = GameObject.FindGameObjectsWithTag("skill");
        foreach (var go in skills)
        {
            if (go) { Object.Destroy(go); removed++; }
        }

        var creates = GameObject.FindGameObjectsWithTag("create");
        foreach (var go in creates)
        {
            if (go) { Object.Destroy(go); removed++; }
        }

        Debug.Log($"[SkillWorldCleanup] Removed skill/create objects: {removed}");
    }
}
