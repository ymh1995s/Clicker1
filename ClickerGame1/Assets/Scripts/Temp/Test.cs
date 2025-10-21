using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button incrementButton; // 버튼을 연결할 변수
    public TextMeshProUGUI scoreText; // TextMeshProUGUI를 사용
    private int score = 0; // 초기 점수

    void Start()
    {
        // 버튼 클릭 이벤트에 메서드 연결
        if (incrementButton != null)
        {
            incrementButton.onClick.AddListener(AddScore);
        }

        // 초기 점수 텍스트 설정
        UpdateScoreText();
    }

    // 점수를 5씩 추가하는 메서드
    void AddScore()
    {
        score += 5;
        UpdateScoreText();
    }

    // 텍스트를 업데이트하는 메서드
    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
