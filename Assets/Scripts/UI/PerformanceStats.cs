using System.Text;
using TMPro;
using UnityEngine;

public class PerformanceStats : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI debugText;

    [Header("Settings")]
    [SerializeField] float refreshRate = 0.25f;

    float timer;
    readonly StringBuilder builder = new();

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer < refreshRate)
            return;

        timer = 0f;
        UpdateText();
    }

    void UpdateText()
    {
        if (debugText == null)
            return;

        builder.Clear();
        AppendFrameStats();

        debugText.text = builder.ToString();
    }

    void AppendFrameStats()
    {
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float frameMs = Time.unscaledDeltaTime * 1000f;

        builder.AppendLine($"FPS: {fps:F0}");
        builder.AppendLine($"Frame: {frameMs:F1} ms");
    }

}