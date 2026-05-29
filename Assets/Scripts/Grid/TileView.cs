using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private Renderer tileRenderer;

    private void Awake()
    {
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public void SetColor(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = color;
        }
    }
}
