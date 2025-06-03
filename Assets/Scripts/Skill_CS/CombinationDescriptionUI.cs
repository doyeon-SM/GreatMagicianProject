using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombinationDescriptionUI : MonoBehaviour
{
    // Inspector에서 할당할 수 있는 UI 컴포넌트들
    public Text descriptionText;   // 설명 텍스트를 표시할 Text 컴포넌트
    public Text damageText;
    public Text SkillNameText;
    public Image resultImage;      // 결과 스킬 이미지를 표시할 Image 컴포넌트

    /// <summary>
    /// 조합 설명 UI를 설정합니다.
    /// </summary>
    /// <param name="description">표시할 설명 텍스트 (예: resultSkill.skillscript)</param>
    /// <param name="resultSprite">표시할 결과 스킬 이미지</param>
    public void Setup(string name, string description, string damagescription, Sprite resultSprite)
    {
        if (descriptionText != null && damageText != null)
        {
            SkillNameText.text = name;
            damageText.text = "Damage: "+damagescription;
            descriptionText.text = description;
        }
        else
        {
            Debug.LogWarning("CombinationDescriptionUI: descriptionText is not assigned.");
        }

        if (resultImage != null)
        {
            resultImage.sprite = resultSprite;
        }
        else
        {
            Debug.LogWarning("CombinationDescriptionUI: resultImage is not assigned.");
        }
    }
}
