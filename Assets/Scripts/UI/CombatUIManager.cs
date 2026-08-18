using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance { get; private set; }

    [SerializeField] GameObject combatUIRoot;
    [SerializeField] Transform timerBar;
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text killCountText;

    int activeCombatCount = 0;
    float timeRemaining = 0f;
    float totalDuration = 0f;
    bool timerRunning = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f)
        {
            timeRemaining = 0f;
        }

        UpdateTimerDisplay();
    }

    public void NotifyCombatStarted(float duration)
    {
        activeCombatCount++;

        totalDuration = duration;
        timeRemaining = duration;
        timerRunning = true;

        UpdateTimerDisplay();
        UpdateVisibility();
    }

    public void NotifyCombatEnded()
    {
        activeCombatCount--;
        timerRunning = false;

        UpdateVisibility();
    }

    public void UpdateKillCount(int count)
    {
        if (killCountText)
        {
            killCountText.text = count.ToString();
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerBar)
        {
            timerBar.localScale = new Vector3(timeRemaining / totalDuration, 1f, 1f);
        }

        if (timerText)
        {
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    void UpdateVisibility()
    {
        if (combatUIRoot)
        {
            combatUIRoot.SetActive(activeCombatCount > 0);
        }
    }
}