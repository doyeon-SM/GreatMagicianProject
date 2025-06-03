using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void ReloadAllSkillData()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ReloadSkillData();
        }
        else
        {
            Debug.LogError("SceneLoader: SkillManager 인스턴스가 존재하지 않습니다.");
        }
    }
    // GameStartButton 클릭 시 호출될 메서드
    public void LoadSampleScene()
    {
        // "SampleScene"으로 씬 전환
        ReloadAllSkillData();
        SceneManager.LoadScene("SampleScene");
    }

    public void LoadEnhanceMenuScene()
    {
        ReloadAllSkillData();
        SceneManager.LoadScene("EnhanceMenu");
    }

    public void LoadLobyScene()
    {
        ReloadAllSkillData();
        SceneManager.LoadScene("Loby");
    }

    public void LoadCharacterMenuScene()
    {
        ReloadAllSkillData();
        SceneManager.LoadScene("CharacterMenu");
    }

    public void LoadSkillArchiveScene()
    {
        ReloadAllSkillData();
        SceneManager.LoadScene("SkillArchive");
    }
}
