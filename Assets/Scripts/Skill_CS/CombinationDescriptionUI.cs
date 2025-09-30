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
    public Text effectText;
    public Text areatimeText;

    /// <summary>
    /// 조합 설명 UI를 설정합니다.
    /// </summary>
    /// <param name="description">표시할 설명 텍스트 (예: resultSkill.skillscript)</param>
    /// <param name="resultSprite">표시할 결과 스킬 이미지</param>
    public void Setup(string name, string description, string damagescription, Sprite resultSprite, float effect, float areatime, string seffect, string stype)
    {
        // 안전 null 체크
        if (SkillNameText != null) SkillNameText.text = name ?? "";
        if (damageText != null) 
        {
            string dam;
            if (stype == "Create" || stype == "Summon") dam = "Hp: ";
            else dam = "Damage: ";
            damageText.text = dam + (damagescription ?? ""); 
        }
        if (descriptionText != null) descriptionText.text = description ?? "";

        // 결과 아이콘
        if (resultImage != null) resultImage.sprite = resultSprite;

        // ---- Effect 텍스트 처리 ----
        if (effectText != null)
        {
            // 먼저 켠 다음 값 보고 끄기
            effectText.gameObject.SetActive(true);

            // effect가 유효하면 표시, 아니면 숨김
            if (effect > Mathf.Epsilon)
            {
                effectText.text = skilleffect(seffect) + effect.ToString();
            }
            else
            {
                effectText.gameObject.SetActive(false);
            }
        }

        // ---- AreaTime 텍스트 처리 ----
        if (areatimeText != null)
        {
            areatimeText.gameObject.SetActive(true);

            if (areatime > Mathf.Epsilon)
            {
                areatimeText.text = "설치 지속시간: " + areatime.ToString() + "초";
            }
            else
            {
                areatimeText.gameObject.SetActive(false);
            }
        }
    }
    private string skilleffect(string effect)
    {
        switch (effect)
        {
            case "Knockback":
                return "넉백 힘: ";
            case "Fear":
                return "공포 지속시간: ";
            case "Burn":
                return "화상 지속시간: ";
            case "Posion":
                return "독 지속시간: ";
            case "Gravity":
                return "중력: ";
            default:
                return "???";
        }
    }
}
