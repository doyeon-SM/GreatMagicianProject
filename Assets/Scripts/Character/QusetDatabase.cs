using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest Database", fileName = "QuestDatabase")]
public class QuestDatabase : ScriptableObject
{
    public List<QuestDefinition> quests = new List<QuestDefinition>();
}
