using UnityEngine;

[System.Serializable]
public class UnderUI_Slot_System : MonoBehaviour
{
    public int slotIndex;  // 슬롯의 위치 번호
    public int skillIndex; // 슬롯에 배정된 스킬의 인덱스
    public GameObject slotObject; // 해당 슬롯의 실제 GameObject

    public UnderUI_Slot_System(int slotIndex, int skillIndex, GameObject slotObject)
    {
        this.slotIndex = slotIndex;
        this.skillIndex = skillIndex;
        this.slotObject = slotObject;
    }

    // 스킬 인덱스를 반환하는 메서드
    public int GetSkillIndex()
    {
        return skillIndex;
    }
}

