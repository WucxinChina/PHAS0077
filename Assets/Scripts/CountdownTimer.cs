using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;

public class CountdownTimer : MonoBehaviour
{
    [Header("倒计时设置")]
    public float startTimeInSeconds = 420f; // 7分钟 = 420秒
    private float currentTime;

    [Header("UI 组件")]
    public TextMeshProUGUI timerText; // 拖到Inspector里

    private bool isRunning = true;

    void Start()
    {
        currentTime = startTimeInSeconds;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (!isRunning) return;

        // 递减时间
        currentTime -= Time.deltaTime;

        // 保证不会小于0
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UnityEditor.EditorApplication.isPlaying = false;
            Application.Quit();
        }

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
