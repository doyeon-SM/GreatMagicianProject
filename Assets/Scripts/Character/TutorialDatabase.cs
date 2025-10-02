using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TutorialDatabase", menuName = "Game/Tutorial Database")]
public class TutorialDatabase : ScriptableObject
{
    [Tooltip("[조건키: 이미지] 목록을 한 곳에서 관리")]
    public TutorialEntry[] entries;

    // 이미지 조회
    public bool TryGetSprite(string key, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(key) || entries == null) return false;

        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.key) && e.key == key)
            {
                sprite = e.image;
                return sprite != null;
            }
        }
        return false;
    }

    // 클리어 여부 확인
    public bool IsCleared(string key)
    {
        if (string.IsNullOrEmpty(key) || entries == null) return false;
        foreach (var e in entries)
        {
            if (e.key == key) return e.clear;
        }
        return false;
    }

    // 클리어 여부 설정
    public bool SetCleared(string key, bool value)
    {
        if (string.IsNullOrEmpty(key) || entries == null) return false;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].key == key)
            {
                entries[i].clear = value;
                return true;
            }
        }
        return false;
    }

    // 저장용: 클리어된 키 목록 뽑기
    public List<string> GetClearedKeys()
    {
        var list = new List<string>();
        if (entries == null) return list;
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.key) && e.clear)
                list.Add(e.key);
        }
        return list;
    }

    // 로드시: 클리어된 키 목록 반영
    public void ApplyClearedKeys(IEnumerable<string> keys)
    {
        // 전부 false로 초기화 후 keys만 true
        if (entries == null) return;
        var set = new HashSet<string>(keys ?? System.Array.Empty<string>());
        for (int i = 0; i < entries.Length; i++)
        {
            var k = entries[i].key;
            if (!string.IsNullOrEmpty(k))
                entries[i].clear = set.Contains(k);
        }
    }
}
