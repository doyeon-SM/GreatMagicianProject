using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    // UI 컴포넌트 (Inspector에서 할당)
    public Text scoreText;     // 점수를 표시할 텍스트

    public SceneLoader sceneLoader;
    public Score_System scoreSystem;

    /// <summary>
    /// GameOver 시 호출하여 결과 UI를 표시합니다.
    /// </summary>
    /// <param name="score">Score_System.cs의 score 값</param>
    public void ShowResult()
    {
        gameObject.SetActive(true);
        if (scoreText != null)
        {
            scoreText.text = "Score: " + scoreSystem.score.ToString();
        }
    }

    /// <summary>
    /// 종료 버튼 클릭 시 실행되는 메서드
    /// </summary>
    public void OnExitButtonClicked()
    {
        scoreSystem.ResultScore();
        scoreSystem.score = 0;
        sceneLoader.LoadLobyScene();
        Time.timeScale = 1; // 게임 멈추기 (선택 사항)
    }
}
