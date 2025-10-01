using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest Definition", fileName = "Q_NewDefinition")]
public class QuestDefinition : ScriptableObject
{
    [Header("기본")]
    public string questId = System.Guid.NewGuid().ToString();
    public string title;
    [TextArea] public string description;

    [Header("종류/목표")]
    public QuestKind kind;
    [Tooltip("목표 카운트(예: 100마리, 10회 등)")]
    public int targetCount = 100;
    [Tooltip("반복 클리어 가능 여부(소비형)")]
    public bool repeatable = true;

    [Header("보상")]
    public QuestReward reward;

    // (옵션) UI에서 정렬/표시용
    public Sprite icon;
}
