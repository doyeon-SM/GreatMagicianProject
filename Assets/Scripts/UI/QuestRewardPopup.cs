using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuestRewardPopup : MonoBehaviour
{
    public Text titleText;
    public Text rewardText;
    public float lifeSeconds = 1.0f;

    private System.Action _onClosed;

    public void Setup(string questTitle, QuestReward reward, int times, System.Action onClosed)
    {
        _onClosed = onClosed;
        if (titleText) titleText.text = $"퀘스트 완료: {questTitle}";
        if (rewardText)
        {
            int exp = reward.exp * times;
            int gold = reward.gold * times;
            int dust = reward.skillDust * times;
            rewardText.text = $"보상: EXP {exp}, GOLD {gold}, DUST {dust}";
        }
        StartCoroutine(AutoClose());
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(lifeSeconds);
        _onClosed?.Invoke();
        Destroy(gameObject);
    }
}
