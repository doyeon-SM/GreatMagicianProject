using UnityEngine;

[System.Serializable]
public class TutorialEntry
{
    [Tooltip("유니크한 조건 키. 예: FirstSkillCreated")]
    public string key;

    [Tooltip("팝업으로 띄울 이미지(Sprite)")]
    public Sprite image;

    [Tooltip("튜토리얼 클리어 여부")]
    public bool clear;
}
