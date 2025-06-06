using System.Collections.Generic;
using UnityEngine;
using System.Linq; // LINQ 사용

public class Skill_Drag_System : MonoBehaviour
{
    public Character character;
    public SpriteRenderer underUISprite;  // 하단 UI를 나타내는 스프라이트 렌더러
    public Sprite blankSkillIconSprite;   // 스킬이 사용된 후 빈 아이콘으로 변경할 스프라이트
    public Sprite UnknowImage;          //처음 배우는 스킬 아이콘
    public List<Skill_Data> skillDataArray;  // 각 스킬에 대한 SkillData 배열
    public List<Skill_Combination_Data> skillCombinations; // 스킬 조합 배열

    public GameObject combinationDescriptionUIPrefab;   //설명 UI 프리팹

    private Vector3 startPosition;  // 아이콘 드래그 시작 위치
    private Vector3 offset;  // 마우스 클릭 시 아이콘과 마우스 포인터 간의 거리
    private float zCoordinate;  // 아이콘의 z축 고정 값
    private SpriteRenderer spriteRenderer;  // 아이콘의 스프라이트 렌더러
    private GameObject instantiatedRange;  // 드래그 중 생성된 스킬 범위(Range) 프리팹
    private GameObject instantiatedUseableRange; // 드래그 중 생성된 스킬 사거리(Useable_Range) 프리팹

    private GameObject combinationDescriptionUIInstance;

    private Rigidbody2D rb2d;  // 아이콘의 Rigidbody2D
    private int originalSortingOrder;  // 아이콘의 원래 정렬 순서
    private Skill_Combination_Data pendingCombination;
    private UnderUI_Slot_System pendingOtherSlot;
    private bool isDragging = false;  // 드래그 상태를 나타내는 플래그
    private UnderUI_Slot_System currentSlotData;  // 현재 슬롯 데이터 참조

    void Start()
    {
        skillDataArray.AddRange(character.tier0Skills);
        skillDataArray.AddRange(character.tier1Skills);
        skillDataArray.AddRange(character.tier2Skills);

        skillCombinations.AddRange(character.tier1SkillsCombination);
        skillCombinations.AddRange(character.tier2SkillsCombination);

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // 하단 UI의 SpriteRenderer 설정
        if (underUISprite == null)
        {
            underUISprite = GameObject.Find("UnderUI")?.GetComponent<SpriteRenderer>();
            if (underUISprite == null)
            {
                Debug.LogError("UnderUI SpriteRenderer not found. Please assign it in the Inspector.");
            }
        }

        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        originalSortingOrder = spriteRenderer.sortingOrder;

        if (rb2d != null)
        {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void OnMouseDown()
    {
        if (spriteRenderer.sprite.name != "Blank_Skill_Icon")
        {
            InitializeDrag();
            currentSlotData = FindSlotDataFromPosition();
        }
    }

    private void InitializeDrag()
    {
        zCoordinate = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPosition();
        startPosition = transform.position;
        spriteRenderer.sortingOrder = 100;
        isDragging = true;

        DestroyRangeAndUseableRange(); // 드래그 시작 시 초기화
    }

    private void OnMouseDrag()
    {
        if (isDragging && spriteRenderer.sprite.name != "Blank_Skill_Icon")
        {
            Vector3 mousePosition = GetClampedMousePosition();
            transform.position = mousePosition;

            // 겹치는 슬롯 검색
            GameObject overlappingSkill = FindOverlappingSkill();
            if (overlappingSkill != null)
            {
                // 현재 슬롯과 겹친 슬롯의 데이터 확보
                Skill_Data currentSkillData = GetSkillDataForCurrentSkill();
                UnderUI_Slot_System otherSlotData = overlappingSkill.GetComponent<UnderUI_Slot_System>();
                if (otherSlotData != null && otherSlotData.GetSkillIndex() != -1)
                {
                    Skill_Data otherSkillData = skillDataArray[otherSlotData.GetSkillIndex()];
                    Skill_Combination_Data combination = FindCombinationForSkills(currentSkillData, otherSkillData);
                    if (combination != null)
                    {
                        // 후보 조합 저장
                        pendingCombination = combination;
                        pendingOtherSlot = otherSlotData;
                        ShowCombinationDescriptionUI(currentSkillData, otherSkillData, combination.resultSkill);
                    }
                    else
                    {
                        HideCombinationDescriptionUI();
                        pendingCombination = null;
                        pendingOtherSlot = null;
                    }
                }
            }
            else
            {
                HideCombinationDescriptionUI();
                pendingCombination = null;
                pendingOtherSlot = null;
            }

            if (!IsMouseWithinUnderUI())
            {
                spriteRenderer.enabled = false;
                CreateRangeAndUseableRange(mousePosition);
            }
            else
            {
                spriteRenderer.enabled = true;
                DestroyRangeAndUseableRange();
            }
        }
    }
    private void OnMouseUp()
    {
        if (isDragging && spriteRenderer.sprite.name != "Blank_Skill_Icon")
        {
            isDragging = false;
            HideCombinationDescriptionUI();  // 드래그 종료 시 설명 UI 제거

            // 후보 조합이 있으면 그 결과를 사용하여 합성 처리
            if (pendingCombination != null && pendingOtherSlot != null)
            {
                ApplyCombinedSkill(pendingCombination.resultSkill, pendingOtherSlot);
                ResetIconToOriginalPosition();
            }
            else if (instantiatedRange != null)
            {
                ApplySkillEffect();
                ResetIconToOriginalPosition();
            }
            else
            {
                ReturnIconToOriginalPosition();
            }

            // 후보 변수 초기화
            pendingCombination = null;
            pendingOtherSlot = null;
        }
    }

    private bool IsCollidingWithAnotherSkill()
    {
        GameObject[] slotObjects = GameObject.FindGameObjectsWithTag("Skill");
        foreach (GameObject slotObject in slotObjects)
        {
            if (slotObject != gameObject && Vector3.Distance(slotObject.transform.position, transform.position) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }

    private void ApplySkillEffect()
    {
        Skill_Range_System rangeHandler = instantiatedRange.GetComponent<Skill_Range_System>();
        if (rangeHandler != null)
        {
            rangeHandler.skillData = GetSkillDataForCurrentSkill();
            rangeHandler.ApplySkillEffect(GetMouseWorldPosition());
        }

        DestroyRangeAndUseableRange(); // 범위와 사거리 오브젝트 제거
    }

    private Vector3 GetClampedMousePosition()
    {
        Vector3 mousePosition = GetMouseWorldPosition() + offset;

        if (instantiatedUseableRange != null)
        {
            Vector3 rangeCenter = instantiatedUseableRange.transform.position;
            float rangeRadius = instantiatedUseableRange.GetComponent<CircleCollider2D>().radius * instantiatedUseableRange.transform.localScale.x;

            Vector3 directionToMouse = mousePosition - rangeCenter;

            if (directionToMouse.magnitude > rangeRadius)
            {
                directionToMouse = directionToMouse.normalized * rangeRadius;
                mousePosition = rangeCenter + directionToMouse;
            }
        }

        return mousePosition;
    }

    private void CreateRangeAndUseableRange(Vector3 mousePosition)
    {
        Skill_Data skillData = GetSkillDataForCurrentSkill();
        if (instantiatedUseableRange == null && currentSlotData != null)
        {  
            if (skillData != null && skillData.useableRangePrefab != null)
            {
                instantiatedUseableRange = Instantiate(skillData.useableRangePrefab, new Vector3(0, -8, 0), Quaternion.identity);
                ConfigureRangeObject(instantiatedUseableRange);

                if (skillData.rangePrefab != null)
                {
                    if(skillData.skillType == Skill_Data.SkillType.StraightLine)
                    {
                        // 직사각형 범위 생성
                        if (instantiatedRange == null)
                        {
                            instantiatedRange = Instantiate(skillData.rangePrefab, new Vector3(0, -8, 0), Quaternion.identity);
                            instantiatedRange.transform.localScale = new Vector3(25f, 1f, 1f);  // 1x10 크기 설정
                            ConfigureRangeObject(instantiatedRange);
                        }

                        // 직사각형의 아랫변 중앙 고정
                        Vector3 bottomCenter = new Vector3(0, -8, 0);

                        // 직사각형 중앙선의 방향 계산 (부채꼴 회전)
                        Vector3 direction = (mousePosition - bottomCenter).normalized;
                        float distance = 5f; // 직사각형 높이의 절반 (중앙선 길이의 절반)

                        // 중앙선을 따라 마우스가 위치할 좌표를 계산
                        Vector3 correctedMousePosition = bottomCenter + direction * distance;

                        // 직사각형 회전 (아랫변 고정)
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        instantiatedRange.transform.rotation = Quaternion.Euler(0, 0, angle);

                        // 직사각형의 아랫변 중앙을 고정한 상태에서 마우스를 중앙선 위에 고정
                        instantiatedRange.transform.position = bottomCenter;
                    }
                    else if (skillData.skillType == Skill_Data.SkillType.Around)
                    {
                        Vector3 fixedCenter = new Vector3(0, -8, 0); // 중심

                        if (instantiatedUseableRange == null)
                        {
                            instantiatedUseableRange = Instantiate(skillData.useableRangePrefab, fixedCenter, Quaternion.identity);
                            ConfigureRangeObject(instantiatedUseableRange);
                        }

                        if (skillData.rangePrefab != null && instantiatedRange == null)
                        {
                            instantiatedRange = Instantiate(skillData.rangePrefab, fixedCenter, Quaternion.identity);
                            ConfigureRangeObject(instantiatedRange);
                        }

                        return; // Around 처리 종료
                    }
                    else 
                    { 
                        instantiatedRange = Instantiate(skillData.rangePrefab, mousePosition, Quaternion.identity);
                        ConfigureRangeObject(instantiatedRange);
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid skill data or useableRangePrefab not set properly.");
            }
        }
        else if (instantiatedRange != null)
        {
            // 직사각형 범위가 이미 생성된 경우, 마우스 위치에 따라 회전 및 이동 조정
            if (skillData != null && skillData.skillType == Skill_Data.SkillType.StraightLine)
            {
                // 아랫변 중앙 고정: (0, -8, 0)
                Vector3 bottomCenter = new Vector3(0, -8, 0);

                // 마우스를 직사각형의 중앙선 위에 위치시키기 위해 방향을 계산
                Vector3 direction = (mousePosition - bottomCenter).normalized;
                float distance = 5f; // 직사각형 높이의 절반 (중앙선 길이의 절반)

                // 중앙선을 따라 마우스가 위치할 좌표를 계산
                Vector3 correctedMousePosition = bottomCenter + direction * distance;

                // 직사각형 회전 (아랫변 고정)
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                instantiatedRange.transform.rotation = Quaternion.Euler(0, 0, angle);

                // 직사각형의 아랫변 중앙을 고정한 상태에서 마우스를 중앙선 위에 고정
                instantiatedRange.transform.position = bottomCenter;
            }
            else if(skillData != null && skillData.skillType != Skill_Data.SkillType.Around)
            {
                instantiatedRange.transform.position = mousePosition;
            }
        }
    }

    private void DestroyRangeAndUseableRange()
    {
        if (instantiatedUseableRange != null)
        {
            Destroy(instantiatedUseableRange);
            instantiatedUseableRange = null;
        }

        if (instantiatedRange != null)
        {
            Destroy(instantiatedRange);
            instantiatedRange = null;
        }
    }

    private void ResetIconToOriginalPosition()
    {
        transform.position = startPosition;
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = originalSortingOrder;
        spriteRenderer.sprite = blankSkillIconSprite;

        if (currentSlotData != null)
        {
            Debug.Log("ResetIconToOriginalPosition: Before reset, skillIndex = " + currentSlotData.skillIndex);
            currentSlotData.skillIndex = -1;  // 스킬 인덱스 초기화
            currentSlotData.slotObject.GetComponent<SpriteRenderer>().sprite = blankSkillIconSprite;
            Debug.Log("ResetIconToOriginalPosition: After reset, skillIndex = " + currentSlotData.skillIndex);
        }
    }

    private void ReturnIconToOriginalPosition()
    {
        // 스킬 아이콘을 드래그 시작 위치로 되돌림
        transform.position = startPosition;
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = originalSortingOrder;

        // 현재 슬롯 데이터가 유효한지 확인 후, 아이콘 스프라이트를 복원
        if (currentSlotData != null && currentSlotData.skillIndex != -1)
        {
            //Debug.Log("ReturnIconToOriginalPosition: Slot " + currentSlotData.slotIndex + " has skillIndex = " + currentSlotData.skillIndex);
            spriteRenderer.sprite = currentSlotData.slotObject.GetComponent<SpriteRenderer>().sprite;
        }
        else
        {
            //Debug.Log("ReturnIconToOriginalPosition: No valid skill, setting blank sprite.");
            spriteRenderer.sprite = blankSkillIconSprite;
        }
    }

    private UnderUI_Slot_System FindSlotDataFromPosition()
    {
        GameObject[] slotObjects = GameObject.FindGameObjectsWithTag("Skill");
        foreach (GameObject slotObject in slotObjects)
        {
            if (Vector3.Distance(slotObject.transform.position, startPosition) < 0.1f)
            {
                UnderUI_Slot_System slotData = slotObject.GetComponent<UnderUI_Slot_System>();
                if (slotData != null)
                {
                    return slotData;
                }
            }
        }
        return null;
    }

    private Skill_Data GetSkillDataForCurrentSkill()
    {
        if (currentSlotData != null)
        {
            int skillIndex = currentSlotData.GetSkillIndex();
            if (skillIndex >= 0 && skillIndex < skillDataArray.Count)
            {
                return skillDataArray[skillIndex];
            }
        }
        Debug.LogError("Invalid skill index or currentSlotData is null.");
        return null;
    }

    private void ConfigureRangeObject(GameObject rangeObject)
    {
        Rigidbody2D rangeRb2d = rangeObject.GetComponent<Rigidbody2D>();
        if (rangeRb2d != null)
        {
            rangeRb2d.bodyType = RigidbodyType2D.Kinematic;
            rangeRb2d.gravityScale = 0;
        }

        Collider2D rangeCollider = rangeObject.GetComponent<Collider2D>();
        if (rangeCollider != null)
        {
            rangeCollider.isTrigger = true;
        }

        SpriteRenderer rangeSpriteRenderer = rangeObject.GetComponent<SpriteRenderer>();
        if (rangeSpriteRenderer != null)
        {
            rangeSpriteRenderer.sortingLayerName = "Skill_Range";
            rangeSpriteRenderer.sortingOrder = 100;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = zCoordinate;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private bool IsMouseWithinUnderUI()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        return underUISprite.bounds.Contains(mouseWorldPosition);
    }
    private bool IsWithinUnderUI()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        return underUISprite.bounds.Contains(mouseWorldPosition);
    }

    /*private Skill_Data TryCombineSkills()
    {
        if (currentSlotData == null || currentSlotData.GetSkillIndex() == -1)
        {
            Debug.LogError("Invalid skill index for current slot.");
            return null;
        }

        Skill_Data currentSkillData = skillDataArray[currentSlotData.GetSkillIndex()];

        if (currentSkillData == null)
        {
            Debug.LogError("No skill data found for current slot.");
            return null;
        }

        foreach (var combination in skillCombinations)
        {
            GameObject[] slotObjects = GameObject.FindGameObjectsWithTag("Skill");
            foreach (GameObject slotObject in slotObjects)
            {
                UnderUI_Slot_System otherSlotData = slotObject.GetComponent<UnderUI_Slot_System>();
                if (otherSlotData != null && otherSlotData != currentSlotData && otherSlotData.GetSkillIndex() != -1)
                {
                    Skill_Data otherSkillData = skillDataArray[otherSlotData.GetSkillIndex()];

                    // 두 스킬이 겹쳤는지 확인하고 UnderUI 안에 있는지 확인
                    if (combination.IsCombination(currentSkillData, otherSkillData) && IsCollidingWithAnotherSkill() && IsWithinUnderUI())
                    {
                        otherSlotData.skillIndex = skillDataArray.IndexOf(combination.resultSkill);
                        otherSlotData.slotObject.GetComponent<SpriteRenderer>().sprite = combination.resultSkill.skillIcon;

                        currentSlotData.skillIndex = -1; // 현재 슬롯은 빈 슬롯으로
                        currentSlotData.slotObject.GetComponent<SpriteRenderer>().sprite = blankSkillIconSprite;

                        return combination.resultSkill;
                    }
                }
            }
        }

        ReturnIconToOriginalPosition();
        return null;
    }*/

    private void ApplyCombinedSkill(Skill_Data combinedSkill, UnderUI_Slot_System otherSlot)
    {
        if (currentSlotData != null && otherSlot != null)
        {
            // 다른 슬롯에 결과 스킬 적용
            otherSlot.skillIndex = skillDataArray.IndexOf(combinedSkill);
            otherSlot.slotObject.GetComponent<SpriteRenderer>().sprite = combinedSkill.skillIcon;
            combinedSkill.isKnow = true;

            // 현재 슬롯은 빈 슬롯으로 설정
            currentSlotData.skillIndex = -1;
            currentSlotData.slotObject.GetComponent<SpriteRenderer>().sprite = blankSkillIconSprite;

            DestroyRangeAndUseableRange();
        }
    }

    // 겹쳐진 스킬 슬롯 검색 (현재 오브젝트와 다른 오브젝트 중 일정 거리 내에 있는 슬롯)
    private GameObject FindOverlappingSkill()
    {
        GameObject[] slotObjects = GameObject.FindGameObjectsWithTag("Skill");
        foreach (GameObject slotObject in slotObjects)
        {
            if (slotObject != gameObject && Vector3.Distance(slotObject.transform.position, transform.position) < 0.5f)
            {
                return slotObject;
            }
        }
        return null;
    }
    // 두 스킬이 조합 가능한지 확인
    private Skill_Combination_Data FindCombinationForSkills(Skill_Data currentSkill, Skill_Data otherSkill)
    {
        foreach (var combination in skillCombinations)
        {
            if (combination.IsCombination(currentSkill, otherSkill))
            {
                return combination;
            }
        }
        return null;
    }
    // 설명 UI를 생성하고 내용을 설정
    private void ShowCombinationDescriptionUI(Skill_Data currentSkill, Skill_Data otherSkill, Skill_Data resultSkill)
    {
        if (combinationDescriptionUIInstance == null && combinationDescriptionUIPrefab != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                combinationDescriptionUIInstance = Instantiate(combinationDescriptionUIPrefab, canvas.transform);
            }
        }
        if (combinationDescriptionUIInstance != null)
        {
            // CombinationDescriptionUI 스크립트에 Setup() 메서드를 만들어 둔 것으로 가정합니다.
            var uiScript = combinationDescriptionUIInstance.GetComponent<CombinationDescriptionUI>();
            string description;
            string damagetext;
            if (uiScript != null && resultSkill.isKnow == true)
            {
                if (resultSkill.skillscript != null)
                {
                    description = resultSkill.skillscript;
                    damagetext = resultSkill.damage.ToString();
                }
                else
                {
                    description = $"Combine {currentSkill.skillName} and {otherSkill.skillName} to create {resultSkill.skillName}";
                    damagetext = "";
                }
                uiScript.Setup(resultSkill.skillName, description, damagetext, resultSkill.skillIcon);
            }
            else
            {
                uiScript.Setup("???", "?????", "??", UnknowImage);
            }
        }
    }
    // 설명 UI 제거
    private void HideCombinationDescriptionUI()
    {
        if (combinationDescriptionUIInstance != null)
        {
            Destroy(combinationDescriptionUIInstance);
            combinationDescriptionUIInstance = null;
        }
    }
}
