using System.Collections.Generic;
using UnityEngine;

public class UnderUI_System_Base : MonoBehaviour
{
    public static UnderUI_System_Base Instance { get; private set; }

    public GameObject skillIconPrefab;  // 스킬 아이콘 프리팹
    public Skill_Data[] skillDataArray;  // 0티어 스킬 데이터 배열
    public List<Skill_Data> tier1SkillDataList = new List<Skill_Data>();    // 1티어 스킬 데이터 배열
    public List<Skill_Data> tier2SkillDataLais = new List<Skill_Data>();    // 2티어 스킬 데이터 배열

    public Vector2 skillSlotSize = new Vector2(1, 1);
    float skillSlotSpacingx = 2f;
    float skillSlotSpacingy = 1f;

    float startX = -4f;
    float startY = -7.5f;

    public List<UnderUI_Slot_System> slotDataList = new List<UnderUI_Slot_System>();
    private int totalSlots = 10;
    private int slotsPerRow = 5;

    private bool _firstAddSkillTriggered = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeSkillSlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Mana_Base.currentMana >= Mana_Base.maxMana)
        {
            AddSkill();
            Mana_Base.currentMana = 0;
        }
    }

    private void InitializeSkillSlots()
    {
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject skillSlot = Instantiate(skillIconPrefab);

            skillSlot.transform.localScale = new Vector3(skillSlotSize.x, skillSlotSize.y, 1);

            int row = i / slotsPerRow;
            int column = i % slotsPerRow;

            float xPos = startX + column * skillSlotSpacingx;
            float yPos = startY - row * skillSlotSpacingy;

            skillSlot.transform.position = new Vector3(xPos, yPos, 0);

            SpriteRenderer slotSpriteRenderer = skillSlot.GetComponent<SpriteRenderer>();
            if (slotSpriteRenderer != null)
            {
                slotSpriteRenderer.sprite = null;  // 초기 상태는 빈 슬롯
            }

            UnderUI_Slot_System slotData = skillSlot.AddComponent<UnderUI_Slot_System>();
            slotData.slotIndex = i;
            slotData.skillIndex = -1;
            slotData.slotObject = skillSlot;

            slotDataList.Add(slotData);
        }
    }
    // 빈 슬롯 확인용 함수
    public bool HasEmptySkillSlot()
    {
        //Debug.Log("HasEmptySkillSlot() 호출 / 슬롯 확인" + slotDataList.Count);
        for (int i = 0; i < slotDataList.Count; i++)
        {
            if(slotDataList[i].skillIndex == -1)
            {
                //Debug.Log("빈 슬롯 발견: 슬롯 인덱스 " + slotDataList[i].slotIndex);
                return true;
            }
        }
        Debug.Log("빈 슬롯 없음");
        return false;
    }
    public int GetSlotCount()
    {
        return slotDataList.Count;
    }
    public void AddSkill()
    {
        Debug.Log("AddSkill() 호출됨");

        if (!_firstAddSkillTriggered)
        {
            _firstAddSkillTriggered = true;

            // 이미 clear된 경우 TutorialManager가 내부에서 무시
            TutorialManager.Instance?.TryTrigger("FirstSkillCreated");
        }

        for (int i = 0; i < slotDataList.Count; i++)
        {
            if (slotDataList[i].skillIndex == -1)
            {
                int randomIndex = Random.Range(0, skillDataArray.Length);
                Skill_Data randomSkillData = skillDataArray[randomIndex];

                //Debug.Log("빈 슬롯 발견: 슬롯 인덱스 " + slotDataList[i].slotIndex + "에 스킬 추가. randomIndex: " + randomIndex);

                // 슬롯에 스킬 할당
                slotDataList[i].skillIndex = randomIndex;
                SpriteRenderer sr = slotDataList[i].slotObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = randomSkillData.skillIcon;
                    //Debug.Log("슬롯 " + slotDataList[i].slotIndex + "의 스프라이트가 업데이트되었습니다.");
                }
                else
                {
                    Debug.LogError("슬롯 " + slotDataList[i].slotIndex + "에 SpriteRenderer가 없습니다.");
                }
                break;
            }
        }
    }

    public void Tier1AddSkill()
    {
        Debug.Log("Tier1AddSkill() 호출됨");

        if (tier1SkillDataList == null || tier1SkillDataList.Count == 0)
        {
            Debug.LogError("티어 1 스킬 데이터 리스트가 비어 있습니다.");
            return;
        }

        // 빈 슬롯을 찾아 1티어 스킬 추가
        for (int i = 0; i < slotDataList.Count; i++)
        {
            if (slotDataList[i].skillIndex == -1)
            {
                int randomIndex = Random.Range(0, tier1SkillDataList.Count);
                Skill_Data randomSkillData = tier1SkillDataList[randomIndex];

                //Debug.Log("빈 슬롯 발견: 슬롯 인덱스 " + slotDataList[i].slotIndex + "에 티어 1 스킬 추가. randomIndex: " + randomIndex);

                // 빈 슬롯에 1티어 스킬 할당 (여기서는 임의로 tier1 스킬의 인덱스를 저장)
                slotDataList[i].skillIndex = randomIndex + 4;       //+4는 0티어 스킬 인덱스 수
                SpriteRenderer sr = slotDataList[i].slotObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = randomSkillData.skillIcon;
                    //Debug.Log("슬롯 " + slotDataList[i].slotIndex + "의 스프라이트가 티어 1 스킬로 업데이트되었습니다.");
                }
                else
                {
                    Debug.LogError("슬롯 " + slotDataList[i].slotIndex + "에 SpriteRenderer가 없습니다.");
                }
                break;
            }
        }
    }

    /// <summary>
    /// 다음 스테이지 시작 전, UI 슬롯과 월드에 남아있는 스킬/생성체를 모두 초기화한다.
    /// </summary>
    public void ResetForNextStage()
    {
        ResetAllSlots();
        CleanupSpawnedSkillsInWorld();
        Debug.Log("[UnderUI] ResetForNextStage: slots cleared & world skills cleaned");
    }

    /// <summary>
    /// 슬롯 초기화: 모든 슬롯을 비우고 스프라이트 제거
    /// </summary>
    public void ResetAllSlots()
    {
        if (slotDataList == null) return;

        for (int i = 0; i < slotDataList.Count; i++)
        {
            var slot = slotDataList[i];
            if (slot == null || slot.slotObject == null) continue;

            slot.skillIndex = -1;

            var sr = slot.slotObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = null;
        }
    }

    /// <summary>
    /// 맵에 남아있는 스킬/생성체(태그: "skill", "create")를 제거
    /// </summary>
    public void CleanupSpawnedSkillsInWorld()
    {
        int removed = 0;

        // 안전 태그 체크 유틸(미등록 태그 접근 시 UnityException 방지)
        bool IsTagDefined(string t)
        {
            try { GameObject.FindGameObjectsWithTag(t); return true; }
            catch (UnityException) { return false; }
        }

        if (IsTagDefined("SkillArea"))
        {
            var skills = GameObject.FindGameObjectsWithTag("SkillArea");
            foreach (var go in skills) { if (go) { Destroy(go); removed++; } }
        }
        else
        {
            Debug.LogWarning("[UnderUI] Tag 'skill' not defined. (무시)");
        }

        if (IsTagDefined("Create"))
        {
            var creates = GameObject.FindGameObjectsWithTag("Create");
            foreach (var go in creates) { if (go) { Destroy(go); removed++; } }
        }
        else
        {
            Debug.LogWarning("[UnderUI] Tag 'create' not defined. (무시)");
        }

        Debug.Log($"[UnderUI] Cleaned spawned skill objects: {removed}");
    }

}
