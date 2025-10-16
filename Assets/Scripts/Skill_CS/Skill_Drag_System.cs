using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    
    // === 안내 UI 프리팹 & 인스턴스 ===
    [Header("Learned Skill Info UI")]
    public GameObject informationCombinedSkillUIPrefab; // 미리 만든 프리팹 할당
    private GameObject informationCombinedSkillUIInstance;

    // 드래그 중 슬로우 관리용
    private static int s_activeDragCount = 0;
    private bool slowApplied = false;

    void Start()
    {
        skillDataArray.AddRange(character.tier0Skills);
        skillDataArray.AddRange(character.tier1Skills);
        skillDataArray.AddRange(character.tier2Skills);

        skillCombinations.AddRange(character.tier1SkillsCombination);
        skillCombinations.AddRange(character.tier2SkillsCombination);

        InitializeComponents();
    }
    void Update()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsShowing)
            return;

        if (HasInput())
        {
            Vector3 screenPos = GetInputPosition();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;

            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.gameObject == gameObject)
            {
                if (!isDragging && InputStarted())
                {
                    TryStartDrag(worldPos);
                }
                else if (isDragging)
                {
                    OnDragging(worldPos);
                }
            }
        }
        else if (isDragging)
        {
            OnRelease();
        }
    }


    private bool HasInput()
    {
#if UNITY_EDITOR
        return Input.GetMouseButton(0);
#else
        return Input.touchCount > 0;
#endif
    }

    private bool InputStarted()
    {
#if UNITY_EDITOR
        return Input.GetMouseButtonDown(0);
#else
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }

    private Vector3 GetInputPosition()
    {
#if UNITY_EDITOR
        return Input.mousePosition;
#else
        return Input.GetTouch(0).position;
#endif
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
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsShowing)
            return;

        if (spriteRenderer.sprite.name != "Blank_Skill_Icon")
        {
            InitializeDrag();
            currentSlotData = FindSlotDataFromPosition();
        }
    }
    private void TryStartDrag(Vector3 pos)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsShowing)
            return;

        if (spriteRenderer.sprite.name != "Blank_Skill_Icon")
        {
            if (Vector2.Distance(transform.position, pos) < 1f) // 근접한 터치일 경우만 시작
            {
                InitializeDrag();
                currentSlotData = FindSlotDataFromPosition();
            }
        }
    }
    private void InitializeDrag()
    {
        zCoordinate = Camera.main.WorldToScreenPoint(transform.position).z;
        offset = transform.position - GetMouseWorldPosition();
        startPosition = transform.position;
        spriteRenderer.sortingOrder = 100;
        isDragging = true;

        // 드래그 시작: 슬로우 적용
        if (!slowApplied)
        {
            s_activeDragCount++;
            slowApplied = true;
            Time.timeScale = 0.5f;
        }

        DestroyRangeAndUseableRange(); // 드래그 시작 시 초기화
    }

    private void OnMouseDrag()
    {
        if (!isDragging || spriteRenderer.sprite.name == "Blank_Skill_Icon")
            return;

        // 위치 갱신 (사거리 제한 고려)
        Vector3 mousePosition = GetClampedMousePosition();
        transform.position = mousePosition;

        // 겹치는 슬롯/조합 후보 계산 (UI는 아래에서 한 번에 처리)
        GameObject overlappingSkill = FindOverlappingSkill();
        if (overlappingSkill != null)
        {
            Skill_Data currentSkillData = GetSkillDataForCurrentSkill();
            UnderUI_Slot_System otherSlotData = overlappingSkill.GetComponent<UnderUI_Slot_System>();

            if (currentSkillData != null && otherSlotData != null && otherSlotData.GetSkillIndex() != -1)
            {
                Skill_Data otherSkillData = skillDataArray[otherSlotData.GetSkillIndex()];
                Skill_Combination_Data combination = FindCombinationForSkills(currentSkillData, otherSkillData);

                if (combination != null)
                {
                    pendingCombination = combination;
                    pendingOtherSlot = otherSlotData;
                }
                else
                {
                    pendingCombination = null;
                    pendingOtherSlot = null;
                }
            }
            else
            {
                pendingCombination = null;
                pendingOtherSlot = null;
            }
        }
        else
        {
            pendingCombination = null;
            pendingOtherSlot = null;
        }

        // 설명 UI 표시 분기 (조합 > 일반설명 > 숨김)
        bool insideUnderUI = IsMouseWithinUnderUI();
        if (pendingCombination != null && pendingOtherSlot != null)
        {
            Skill_Data currentSkillData = GetSkillDataForCurrentSkill();
            Skill_Data otherSkillData = (pendingOtherSlot != null && pendingOtherSlot.GetSkillIndex() != -1)
                ? skillDataArray[pendingOtherSlot.GetSkillIndex()]
                : null;

            if (currentSkillData != null && otherSkillData != null)
                ShowCombinationDescriptionUI(currentSkillData, otherSkillData, pendingCombination.resultSkill);
        }
        else if (insideUnderUI)
        {
            ShowSkillDescriptionUI(GetSkillDataForCurrentSkill());
        }
        else
        {
            HideCombinationDescriptionUI();
        }

        // UnderUI 안/밖에 따른 아이콘 표시 및 범위 오브젝트 관리
        if (!insideUnderUI)
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

    private void OnDragging(Vector3 pos)
    {
        // 위치 갱신 (드래그 오프셋 + 사거리 제한)
        Vector3 clampedPos = ClampToUseableRange(pos + offset);
        transform.position = clampedPos;


        // 겹치는 슬롯/조합 후보 계산 (UI는 아래에서 한 번에 처리)
        GameObject overlapping = FindOverlappingSkill();
        if (overlapping != null)
        {
            Skill_Data currentSkill = GetSkillDataForCurrentSkill();
            UnderUI_Slot_System otherSlot = overlapping.GetComponent<UnderUI_Slot_System>();

            if (currentSkill != null && otherSlot != null && otherSlot.GetSkillIndex() != -1)
            {
                Skill_Data otherSkill = skillDataArray[otherSlot.GetSkillIndex()];
                Skill_Combination_Data combo = FindCombinationForSkills(currentSkill, otherSkill);

                if (combo != null)
                {
                    pendingCombination = combo;
                    pendingOtherSlot = otherSlot;
                }
                else
                {
                    pendingCombination = null;
                    pendingOtherSlot = null;
                }
            }
            else
            {
                pendingCombination = null;
                pendingOtherSlot = null;
            }
        }
        else
        {
            pendingCombination = null;
            pendingOtherSlot = null;
        }

        // 설명 UI 표시 분기 (조합 > 일반설명 > 숨김)
        bool insideUnderUI = underUISprite.bounds.Contains(pos);
        if (pendingCombination != null && pendingOtherSlot != null)
        {
            Skill_Data currentSkill = GetSkillDataForCurrentSkill();
            Skill_Data otherSkill = (pendingOtherSlot != null && pendingOtherSlot.GetSkillIndex() != -1)
                ? skillDataArray[pendingOtherSlot.GetSkillIndex()]
                : null;

            if (currentSkill != null && otherSkill != null)
                ShowCombinationDescriptionUI(currentSkill, otherSkill, pendingCombination.resultSkill);
        }
        else if (insideUnderUI)
        {
            ShowSkillDescriptionUI(GetSkillDataForCurrentSkill());
        }
        else
        {
            HideCombinationDescriptionUI();
        }

        // UnderUI 안/밖에 따른 아이콘 표시 및 범위 오브젝트 관리
        if (!insideUnderUI)
        {
            spriteRenderer.enabled = false;
            CreateRangeAndUseableRange(clampedPos);
        }
        else
        {
            spriteRenderer.enabled = true;
            DestroyRangeAndUseableRange();
        }
    }


    private void OnMouseUp()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsShowing)
            return;

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
            // 드래그 종료: 슬로우 해제(모든 드래그 끝났을 때만 1로 복귀)
            if (slowApplied)
            {
                s_activeDragCount = Mathf.Max(0, s_activeDragCount - 1);
                slowApplied = false;

                if (s_activeDragCount == 0 && !InformationCombinedSkillUI.IsModalPause)
                {
                    Time.timeScale = 1f;
                }
            }

            // 후보 변수 초기화
            pendingCombination = null;
            pendingOtherSlot = null;
        }
        
    }
    private void OnRelease()
    {
        isDragging = false;
        HideCombinationDescriptionUI();

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

        ClearCombinationState();

        // 드래그 종료: 슬로우 해제
        if (slowApplied)
        {
            s_activeDragCount = Mathf.Max(0, s_activeDragCount - 1);
            slowApplied = false;

            if (s_activeDragCount == 0 && !InformationCombinedSkillUI.IsModalPause)
            {
                Time.timeScale = 1f;
            }
        }
    }
    private void ClearCombinationState()
    {
        HideCombinationDescriptionUI();
        pendingCombination = null;
        pendingOtherSlot = null;
    }

    private Vector3 ClampToUseableRange(Vector3 pos)
    {
        if (instantiatedUseableRange == null) return pos;

        Vector3 center = instantiatedUseableRange.transform.position;
        float radius = instantiatedUseableRange.GetComponent<CircleCollider2D>().radius * instantiatedUseableRange.transform.localScale.x;
        Vector3 direction = pos - center;

        if (direction.magnitude > radius)
            return center + direction.normalized * radius;

        return pos;
    }


    private void ApplySkillEffect()
    {
        // 항상 유효한(사거리 내) 시전 좌표를 사용
        Vector3 castPos = GetValidCastPosition();

        if (instantiatedRange != null)
        {
            Skill_Range_System rangeHandler = instantiatedRange.GetComponent<Skill_Range_System>();
            if (rangeHandler != null)
            {
                rangeHandler.skillData = GetSkillDataForCurrentSkill();
                rangeHandler.ApplySkillEffect(castPos);
                // 퀘스트: 스킬 사용 1회 보고
                if (QuestManager.Instance != null)
                    QuestManager.Instance.ReportSkillUse();
            }
        }
        else
        {
            // 혹시라도 range가 없더라도 안전하게 처리
            Skill_Data sd = GetSkillDataForCurrentSkill();
            if (sd != null)
            {
                // range 없이 직접 처리한다면, 여기서도 castPos 사용
                // 예: Instantiate(sd.attackPrefab, castPos, Quaternion.identity);
            }
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
    // 마우스 위치를 스킬 사거리(UseableRange) 내로 강제 클램프해서 '실제로 시전할 좌표'를 반환
    private Vector3 GetValidCastPosition()
    {
        // 기본: 현재 마우스 월드 좌표
        Vector3 mouse = GetMouseWorldPosition();
        mouse.z = 0f;

        Skill_Data skill = GetSkillDataForCurrentSkill();
        if (skill == null) return mouse;

        // 1) Around 타입은 고정 중심 사용
        if (skill.skillType == Skill_Data.SkillType.Around)
        {
            // 고정 중심 (현재 코드 기준)
            return new Vector3(0f, -8f, 0f);
        }

        // 2) 사거리 오브젝트가 없으면(이상 케이스) 그냥 마우스 좌표 사용
        if (instantiatedUseableRange == null)
            return mouse;

        // 3) 원형 사거리 기준으로 클램프
        Vector3 center = instantiatedUseableRange.transform.position;
        float radius = 0f;

        var cc = instantiatedUseableRange.GetComponent<CircleCollider2D>();
        if (cc != null)
            radius = cc.radius * instantiatedUseableRange.transform.localScale.x;

        // 안정적 안쪽 배치(콜라이더 경계 살짝 안쪽)
        const float innerPadding = 0.01f;
        float maxDist = Mathf.Max(radius - innerPadding, 0f);

        Vector3 dir = mouse - center;
        float dist = dir.magnitude;

        // 사거리 안이면 그대로, 밖이면 경계로 보정
        if (dist <= maxDist || dist == 0f)
            return mouse;

        return center + dir.normalized * maxDist;
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
                    if(skillData.skillType == Skill_Data.SkillType.StraightLine || 
                       (skillData.skillType == Skill_Data.SkillType.Projectile && skillData.skillEffect != Skill_Data.SkillEffect.Explosion) ||
                       skillData.skillType == Skill_Data.SkillType.Chain ||
                       skillData.skillType == Skill_Data.SkillType.Scattered)
                    {
                        // 직사각형 범위 생성
                        if (instantiatedRange == null)
                        {
                            if(skillData.skillEffect == Skill_Data.SkillEffect.Rolling)
                                instantiatedRange = Instantiate(skillData.rangePrefab, new Vector3(mousePosition.x, -8, 0), Quaternion.identity);
                            else
                                instantiatedRange = Instantiate(skillData.rangePrefab, new Vector3(0, -8, 0), Quaternion.identity);
                            //instantiatedRange.transform.localScale = new Vector3(25f, 1f, 1f);  // 1x10 크기 설정
                            ConfigureRangeObject(instantiatedRange);
                        }

                        if (skillData.skillEffect == Skill_Data.SkillEffect.Rolling)
                        {
                            Vector3 bottomCenter = new Vector3(mousePosition.x, -8, 0);
                            Vector3 direction = (mousePosition - bottomCenter).normalized;
                            float distance = 5f;
                            Vector3 correctedMousePosition = bottomCenter + direction * distance;
                            instantiatedRange.transform.rotation = Quaternion.Euler(0, 0, 90);
                            instantiatedRange.transform.position = bottomCenter;
                        }
                        else
                        {
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
            if (skillData != null && 
                (skillData.skillType == Skill_Data.SkillType.StraightLine || 
                (skillData.skillType == Skill_Data.SkillType.Projectile && skillData.skillEffect != Skill_Data.SkillEffect.Explosion) ||
                skillData.skillType == Skill_Data.SkillType.Chain ||
                skillData.skillType == Skill_Data.SkillType.Scattered))
            {
                if (skillData.skillEffect == Skill_Data.SkillEffect.Rolling)
                {
                    Vector3 bottomCenter = new Vector3(mousePosition.x, -8, 0);
                    Vector3 direction = (mousePosition - bottomCenter).normalized;
                    float distance = 5f;
                    Vector3 correctedMousePosition = bottomCenter + direction * distance;
                    instantiatedRange.transform.rotation = Quaternion.Euler(0, 0, 90);
                    instantiatedRange.transform.position = bottomCenter;
                }
                else
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

    private void ApplyCombinedSkill(Skill_Data combinedSkill, UnderUI_Slot_System otherSlot)
    {
        if (currentSlotData == null || otherSlot == null || combinedSkill == null)
            return;

        // 캐논컬 인덱스/인스턴스로 매핑
        int canonicalIndex = FindCanonicalIndex(combinedSkill);
        if (canonicalIndex < 0)
        {
            Debug.LogError($"[Combine] 캐논컬 인덱스 찾기 실패: {combinedSkill.name}");
            // 안전하게 빠지되, 범용적으로 빈 슬롯 처리
            ResetIconToOriginalPosition();
            return;
        }
        Skill_Data canonical = skillDataArray[canonicalIndex];

        // 다른 슬롯에 결과 스킬 적용 (캐논컬 기준)
        otherSlot.skillIndex = canonicalIndex;
        otherSlot.slotObject.GetComponent<SpriteRenderer>().sprite = canonical.skillIcon;

        // 이제부터 이 스킬을 '앎'
        bool wasKnown = canonical.isKnow;
        canonical.isKnow = true;

        // 현재 슬롯은 빈 슬롯 처리
        currentSlotData.skillIndex = -1;
        currentSlotData.slotObject.GetComponent<SpriteRenderer>().sprite = blankSkillIconSprite;

        DestroyRangeAndUseableRange();
        if (!wasKnown)
        {
            ShowCombinedSkillInfoUI(canonical);
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
    // 공통: UI 인스턴스 보장
    private void EnsureDescriptionUIInstance()
    {
        if (combinationDescriptionUIInstance == null && combinationDescriptionUIPrefab != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                combinationDescriptionUIInstance = Instantiate(combinationDescriptionUIPrefab, canvas.transform);
            }
        }
    }

    // 일반 스킬 설명 표시
    private void ShowSkillDescriptionUI(Skill_Data skill)
    {
        if (skill == null) return;
        EnsureDescriptionUIInstance();
        if (combinationDescriptionUIInstance == null) return;

        var uiScript = combinationDescriptionUIInstance.GetComponent<CombinationDescriptionUI>();
        if (uiScript == null) return;

        // 캐논컬 동기화
        int idx = FindCanonicalIndex(skill);
        Skill_Data canonical = (idx >= 0) ? skillDataArray[idx] : skill;

        if (canonical != null && canonical.isKnow)
        {
            string name = canonical.skillName;
            string desc = string.IsNullOrEmpty(canonical.skillscript)
                ? "No Description."
                : canonical.skillscript;
            string dmg = canonical.damage.ToString() + " + " + (character.Character_Int/10).ToString();
            string eff = canonical.skillEffect.ToString();
            string type = canonical.skillType.ToString();
            uiScript.Setup(name, desc, dmg, canonical.skillIcon, canonical.Effect_Value, canonical.AreaTime, eff, type);
        }
        else
        {
            uiScript.Setup("???", "?????", "??", UnknowImage,0,0,"??","??");
        }
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
            var uiScript = combinationDescriptionUIInstance.GetComponent<CombinationDescriptionUI>();
            if (uiScript == null) return;

            // 캐논컬로 동기화
            int idx = FindCanonicalIndex(resultSkill);
            Skill_Data canonical = (idx >= 0) ? skillDataArray[idx] : resultSkill;

            if (canonical != null && canonical.isKnow)
            {
                string description = !string.IsNullOrEmpty(canonical.skillscript)
                    ? canonical.skillscript
                    : $"Combine {currentSkill.skillName} and {otherSkill.skillName} to create {canonical.skillName}";

                string damagetext = canonical.damage.ToString() + " + " + (character.Character_Int / 10).ToString();
                string eff = canonical.skillEffect.ToString();
                string type = canonical.skillType.ToString();
                uiScript.Setup(canonical.skillName, description, damagetext, canonical.skillIcon, canonical.Effect_Value, canonical.AreaTime, eff, type);
            }
            else
            {
                uiScript.Setup("???", "?????", "??", UnknowImage,0,0,"??","??");
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



    // 조합 결과 Skill_Data를 skillDataArray/Character 티어 배열 내 "캐논컬 인스턴스" 인덱스로 매핑
    private int FindCanonicalIndex(Skill_Data sd)
    {
        if (sd == null) return -1;

        // 1) 동일 참조가 skillDataArray에 이미 있는지
        int idx = skillDataArray.IndexOf(sd);
        if (idx >= 0) return idx;

        // 2) Character의 티어 배열에서 같은 참조 찾기 → 글로벌 인덱스로 변환
        if (FindSkillIndexInTiers(sd, out int tier, out int local))
        {
            int offset = 0;
            if (tier > 0) offset += (character.tier0Skills?.Length ?? 0);
            if (tier > 1) offset += (character.tier1Skills?.Length ?? 0);
            return offset + local; // skillDataArray를 (0→1→2티어 순)로 AddRange 했으니 글로벌 인덱스 = 리스트 인덱스
        }

        // 3) 마지막으로 이름으로 매칭(동일 SO를 다른 참조로 들고 있을 때)
        for (int i = 0; i < skillDataArray.Count; i++)
        {
            var k = skillDataArray[i];
            if (k != null && sd != null && k.name == sd.name)
                return i;
        }

        return -1;
    }

    private bool FindSkillIndexInTiers(Skill_Data target, out int tier, out int local)
    {
        tier = -1; local = -1;
        if (character == null || target == null) return false;

        var t0 = character.tier0Skills;
        if (t0 != null)
            for (int i = 0; i < t0.Length; i++)
                if (t0[i] == target) { tier = 0; local = i; return true; }

        var t1 = character.tier1Skills;
        if (t1 != null)
            for (int i = 0; i < t1.Length; i++)
                if (t1[i] == target) { tier = 1; local = i; return true; }

        var t2 = character.tier2Skills;
        if (t2 != null)
            for (int i = 0; i < t2.Length; i++)
                if (t2[i] == target) { tier = 2; local = i; return true; }

        return false;
    }
    private void OnDisable()
    {
        // 드래그 중 비활성화되면 슬로우 복구
        if (slowApplied)
        {
            s_activeDragCount = Mathf.Max(0, s_activeDragCount - 1);
            slowApplied = false;

            if (s_activeDragCount == 0 && !InformationCombinedSkillUI.IsModalPause)
            {
                Time.timeScale = 1f;
            }
        }
    }

    private void OnDestroy()
    {
        // 파괴 시도 동일
        if (slowApplied)
        {
            s_activeDragCount = Mathf.Max(0, s_activeDragCount - 1);
            slowApplied = false;

            if (s_activeDragCount == 0 && !InformationCombinedSkillUI.IsModalPause)
            {
                Time.timeScale = 1f;
            }
        }
    }

    private void ShowCombinedSkillInfoUI(Skill_Data learnedSkill)
    {
        if (informationCombinedSkillUIPrefab == null)
        {
            Debug.LogError("[InfoUI] informationCombinedSkillUIPrefab 가 비어있습니다. 인스펙터에 할당하세요.");
            return;
        }

        // 기존 인스턴스가 떠 있다면 정리 (중복 방지)
        if (informationCombinedSkillUIInstance != null)
        {
            Destroy(informationCombinedSkillUIInstance);
            informationCombinedSkillUIInstance = null;
        }

        // Canvas 찾기
        GameObject canvas = GameObject.Find("Canvas");
        Transform parent = canvas != null ? canvas.transform : null;

        informationCombinedSkillUIInstance = Instantiate(informationCombinedSkillUIPrefab, parent);

        // 타임스케일 저장 → 0으로 멈춤
        float prevScale = Time.timeScale;
        InformationCombinedSkillUI.IsModalPause = true;

        Time.timeScale = 0f;

        // 세팅
        var ui = informationCombinedSkillUIInstance.GetComponent<InformationCombinedSkillUI>();
        if (ui != null)
        {
            string skillName = learnedSkill != null ? learnedSkill.skillName : "Unknown Skill";
            Sprite icon = learnedSkill != null ? learnedSkill.skillIcon : null;
            ui.Setup(skillName, icon, prevScale);
        }
        else
        {
            Debug.LogError("[InfoUI] InformationCombinedSkillUI 컴포넌트를 프리팹에 붙이세요.");
        }
    }

}
