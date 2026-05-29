using UnityEngine;

public class BoxView : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public Unit HiddenUnit { get; private set; }
    public bool IsBroken { get; private set; }
    public bool IsBreaking { get; private set; }

    public void Initialize(Vector2Int gridPosition, Unit hiddenUnit)
    {
        GridPosition = gridPosition;
        HiddenUnit = hiddenUnit;
        IsBroken = false;
        IsBreaking = false;
        gameObject.SetActive(true);
    }

    public bool BreakBox()
    {
        if (IsBroken || IsBreaking)
        {
            return false;
        }

        IsBreaking = true;
        IsBroken = true;
        IsBreaking = false;
        gameObject.SetActive(false);
        return true;
    }

    public void ResetBox()
    {
        GridPosition = Vector2Int.zero;
        HiddenUnit = null;
        IsBroken = false;
        IsBreaking = false;
        gameObject.SetActive(false);
    }
}
