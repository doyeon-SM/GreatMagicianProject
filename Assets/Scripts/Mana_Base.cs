using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mana_Base : MonoBehaviour
{
    public Character character;
    public static int maxMana;       // 마나의 최대값
    public static int currentMana;      // 현재 마나
    public float manaRegenRate = 1f;  // 마나 회복 속도 (초당 1)
    private float manaTimer = 0f;

    public Text manaText;  // UI에 마나를 표시할 Text 컴포넌트


    // Start is called before the first frame update
    void Start()
    {
        maxMana = character.Character_Mana;
        currentMana = 0;
        UpdateManaUI();
    }

    // Update is called once per frame
    void Update()
    {
        manaTimer += Time.deltaTime;

        if (manaTimer >= manaRegenRate)
        {
            if (currentMana < maxMana)
            {
                currentMana += 1;
                UpdateManaUI();
            }
            manaTimer = 0f;
        }
    }

    private void UpdateManaUI()
    {
        // UI에 마나 값 업데이트
        if (manaText != null)
        {
            manaText.text = $"Mana: {currentMana}/{maxMana}";
        }
    }
}
