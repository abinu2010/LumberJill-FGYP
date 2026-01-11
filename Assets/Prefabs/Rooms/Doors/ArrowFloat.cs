using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ArrowFloat : MonoBehaviour
{
    public float amplitude = 20f;   // Pixels up/down
    public float frequency = 1f;    // Cycles per second

    private RectTransform rectTransform;
    private Vector2 startPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        rectTransform.anchoredPosition = startPos + Vector2.up * offset;
    }
}
