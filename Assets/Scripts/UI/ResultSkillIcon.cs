using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultSkillIcon : MonoBehaviour
{
    public Image iconImage;
    public Text countText;
    public GameObject newText; // ‘NEW’ 배지 (미리 배치, 기본 비활성)

    public void Setup(Sprite icon, int count, bool showNew)
    {
        if (iconImage) iconImage.sprite = icon;
        if (countText) countText.text = (count <= 1) ? "1" : count.ToString();
        if (newText) newText.SetActive(showNew);
    }
}
