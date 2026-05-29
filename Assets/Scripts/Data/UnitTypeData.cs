using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "UnitType", menuName = "Puzzle/Unit Type Data")]
public class UnitTypeData : ScriptableObject
{
    [FormerlySerializedAs("id")]
    [SerializeField] private string _id;
    [FormerlySerializedAs("displayName")]
    [SerializeField] private string _displayName;
    [FormerlySerializedAs("iconSprite")]
    [SerializeField] private Sprite _iconSprite;
    [FormerlySerializedAs("texture")]
    [SerializeField] private Texture2D _texture;
    [FormerlySerializedAs("material")]
    [SerializeField] private Material _material;
    [FormerlySerializedAs("trailColor")]
    [SerializeField] private Color _trailColor = Color.white;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite IconSprite => _iconSprite;
    public Texture2D Texture => _texture;
    public Material Material => _material;
    public Color TrailColor => _trailColor;

    /// <summary>
    /// Single color used for mesh tint and trail, derived from this unit type's configured visuals.
    /// </summary>
    public Color VisualColor
    {
        get
        {
            if (_material != null)
            {
                return _material.color;
            }

            return _trailColor;
        }
    }

    public bool Matches(UnitTypeData other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(_id) && _id == other._id)
        {
            return true;
        }

        return this == other;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep trailColor as an explicit per-type setting.
        // Do not auto-overwrite it from material, otherwise pooled units
        // can appear to share one trail color at runtime.
    }
#endif
}
