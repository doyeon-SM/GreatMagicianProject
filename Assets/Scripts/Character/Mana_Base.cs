using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mana_Base : MonoBehaviour
{
    public Character character;
    public static float maxMana;       // 마나의 최대값
    public static float currentMana;   // 현재 마나
    public float manaRegenInterval = 0.1f;  // 마나 회복 주기 (초 단위)
    public float manaRegenAmount = 0.1f;    // 주기마다 회복되는 양
    private float manaTimer = 0f;

    public Text manaText;  // UI에 마나를 표시할 Text 컴포넌트

    void Start()
    {
        maxMana = character.Character_Mana;
        currentMana = 0f;
        UpdateManaUI();
    }

    void Update()
    {
        manaTimer += Time.deltaTime;

        // manaTimer가 interval을 초과했을 때 여러 번 채워줄 수도 있음
        while (manaTimer >= manaRegenInterval)
        {
            if (currentMana < maxMana)
            {
                currentMana = Mathf.Min(maxMana, currentMana + manaRegenAmount);
                UpdateManaUI();
            }
            manaTimer -= manaRegenInterval;
        }
    }

    private void UpdateManaUI()
    {
        if (manaText != null)
        {
            manaText.text = $"Mana: {currentMana:0.0}/{maxMana:0.0}";
        }
    }
}
