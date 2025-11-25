using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    [SerializeField] private float scaleAmount = .9f;   // How big it gets
    [SerializeField] private float speed = 1f;           // How fast it pulses

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // 0 to 1 oscillation
        float scale = Mathf.Lerp(1f, scaleAmount, t);
        transform.localScale = originalScale * scale;
    }
}
