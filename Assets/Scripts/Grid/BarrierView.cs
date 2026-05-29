using UnityEngine;

public class BarrierView : MonoBehaviour
{
    [SerializeField] private Renderer barrierRenderer;

    private void Awake()
    {
        if (barrierRenderer == null)
        {
            barrierRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public void SetColor(Color color)
    {
        if (barrierRenderer != null)
        {
            barrierRenderer.material.color = color;
        }
    }
}
