using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wall_System_Base : MonoBehaviour
{
    public Character character;
    public Score_System scoreSystem;
    public GameObject gameResultUIPrefab;
    public int maxHealth = 10; // 벽의 최대 체력
    public int currentHealth; // 현재 체력
    public Text healthText; // 체력을 표시할 UI 텍스트
    public Text ScoreText;  // 점수를 표시할 UI
    public Text TimerText;  // 시간을 표시할 UI
    public float PlayTime = 0f;

    //private bool isUnderAttack = false; // 적이 충돌하고 있는지 확인
    private int monstersInContact = 0;  // 현재 벽에 충돌 중인 몬스터의 개수
    private float damageInterval = 1f; // 1초에 한 번 체력이 감소
    private float damageTimer = 0f;
    private GameResultUI gameresultui;
    private bool isGameOver = false;

    // Inspector에서 UnderUI prefab을 할당하세요.
    public GameObject underUIPrefab;

    // Start is called before the first frame update
    void Start()
    {
        PlayTime = 0f;
        maxHealth = character.WallHP;
        Vector3 myVector = new Vector3(0, -8.3f, 0);
        currentHealth = maxHealth; // 처음엔 체력이 최대치
        UpdateUI(); // UI 업데이트
        if (underUIPrefab == null)
        {
            Debug.LogError("UnderUI prefab이 할당되지 않았습니다.");
            return;
        }

        // 원하는 위치(예: (0,0,0))와 기본 회전으로 UnderUI prefab 인스턴스 생성
        GameObject underUIInstance = Instantiate(underUIPrefab, myVector, Quaternion.identity);

        // 이름을 명확히 지정 (옵션)
        underUIInstance.name = "UnderUI_System_Instance";
    }

    // Update is called once per frame
    void Update()
    {
        PlayTime += Time.deltaTime;
        if (monstersInContact > 0)
        {
            damageTimer += Time.deltaTime;

            if (damageTimer >= damageInterval)
            {
                TakeDamage(1);  // 1초마다 체력 1 감소
                damageTimer = 0f;
            }
        }

        // 체력이 0 이하가 되면 게임 종료
        if (currentHealth <= 0 && !isGameOver)
        {
            GameOver();
            isGameOver = true;
        }

        UpdateUI(); // UI 업데이트
    }

    // 적이 충돌했을 때 호출
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            monstersInContact++;  // 몬스터 개체수 증가
        }
    }

    // 적이 벽에서 떨어질 때 호출
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            monstersInContact--;  // 몬스터 개체수 감
        }
    }

    // 체력을 감소시키는 함수
    public void TakeDamage(int damage)
    {
        currentHealth = currentHealth - (damage * monstersInContact);
    }

    // 체력을 UI로 업데이트하는 함수
    private void UpdateUI()
    {
        healthText.text = "Wall Health: " + currentHealth.ToString();
        ScoreText.text = "Score: " + scoreSystem.score.ToString();
        int minutes = Mathf.FloorToInt(PlayTime / 60f);
        int seconds = Mathf.FloorToInt(PlayTime % 60f);
        TimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        PlayTime = 0f;
        // Canvas를 찾아서 결과 UI를 생성
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            // prefab을 인스턴스화하고, 부모를 Canvas로 설정
            GameObject uiInstance = Instantiate(gameResultUIPrefab, canvas.transform);
            gameresultui = uiInstance.GetComponent<GameResultUI>();
        }
        else
        {
            Debug.LogError("Canvas를 찾을 수 없습니다.");
        }
        // GameResultUI가 할당되어 있으면 결과를 출력
        if (gameresultui != null)
        {   
            gameresultui.ShowResult();
        }
        else
        {
            Debug.LogError("GameResultUI가 할당되어 있지 않습니다.");
        }
        Time.timeScale = 0; // 게임 멈추기 (선택 사항)
    }
}
