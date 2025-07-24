using TMPro;
using UnityEngine;

public class SimpleFPSCounter : MonoBehaviour
{
    [Header("Colors")]
    public Color goodColor = Color.green;      // >= 60 FPS
    public Color okayColor = Color.yellow;     // >= 30 FPS
    public Color badColor = Color.red;         // < 30 FPS
    
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private Canvas fpsCanvas;
    [SerializeField] private float deltaTime = 0.0f;
    
    
    private void Update()
    {
        if (TestingManager.instance.isTesting)
        {
            if (fpsText == null) return;
            
            // Calculate FPS with smoothing
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            
            // Update text
            fpsText.text = $"FPS: {fps:F0}";
            
            // Color coding based on performance
            if (fps >= 60f)
                fpsText.color = goodColor;
            else if (fps >= 30f)
                fpsText.color = okayColor;
            else
                fpsText.color = badColor;
        }
    }
}