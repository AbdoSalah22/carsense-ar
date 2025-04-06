using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseScale = 0.2f;

    private Vector3 originalScale;
    private bool isPulsing = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isPulsing)
        {
            float scale = 1 + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = originalScale * scale;
        }
    }

    public void StartPulse()
    {
        isPulsing = true;
    }

    public void StopPulse()
    {
        isPulsing = false;
        transform.localScale = originalScale;
    }
}
