using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Unit : MonoBehaviour
{
    private static readonly int WakeHash = Animator.StringToHash("Wake");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DieStateHash = Animator.StringToHash("Die");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");

    [SerializeField] private Animator animator;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Material trailMaterial;

    private Vector3 _defaultScale;
    private Material _runtimeMaterialInstance;
    private Material _baseSharedMaterial;
    private Material _trailRuntimeMaterial;
    private Collider _collider;
    private bool _hasInitializedSelectableState;
    private bool _previousSelectable;
    private bool _runAnimationPlayed;

    public UnitTypeData UnitType { get; private set; }
    public Vector2Int GridCoordinate { get; private set; }
    public bool IsSelectable { get; private set; }
    public bool IsBoxed { get; private set; }
    public UnitState State { get; private set; } = UnitState.Pooled;

    private void Awake()
    {
        _defaultScale = transform.localScale;
        _collider = GetComponent<Collider>();
        CacheComponents();

        if (meshRenderer != null)
        {
            _baseSharedMaterial = meshRenderer.sharedMaterial;
        }
    }

    public void SetColliderEnabled(bool enabled)
    {
        if (_collider != null)
        {
            _collider.enabled = enabled;
        }
    }

    public void PrepareFromPool()
    {
        StopMovement();
        transform.localScale = _defaultScale;
        SetColliderEnabled(true);
        gameObject.SetActive(true);
        SetState(UnitState.Pooled);
        ResetSelectableTracking();
        ResetAnimatorState();
    }

    public void Configure(UnitTypeData unitType, Vector2Int gridPosition)
    {
        UnitType = unitType;
        IsBoxed = false;
        GridCoordinate = gridPosition;
        SetSelectable(false);
        SetState(UnitState.OnGrid);
        ApplyVisual();
        ResetTrailState();
        SetColliderEnabled(true);
        transform.localScale = _defaultScale;
    }

    public void Initialize()
    {
        PrepareFromPool();
    }

    public void Initialize(UnitTypeData unitType, Vector2Int gridPosition)
    {
        Configure(unitType, gridPosition);
    }

    public void SetSelectable(bool selectable, bool isInitialSetup = false)
    {
        if (IsBoxed)
        {
            IsSelectable = false;
            _previousSelectable = false;
            if (isInitialSetup)
            {
                _hasInitializedSelectableState = true;
                PlayDieState();
            }

            return;
        }

        if (isInitialSetup)
        {
            IsSelectable = selectable;
            _previousSelectable = selectable;
            _hasInitializedSelectableState = true;

            if (selectable)
            {
                PlayIdleState();
            }
            else
            {
                PlayDieState();
            }

            return;
        }

        if (_hasInitializedSelectableState && !_previousSelectable && selectable)
        {
            PlayWakeTrigger();
        }

        IsSelectable = selectable;
        _previousSelectable = selectable;
    }

    public void SetSelectable(bool selectable)
    {
        SetSelectable(selectable, false);
    }

    public void SetState(UnitState state)
    {
        State = state;
        UpdateTrailEmitting();

        if (state == UnitState.InSlot)
        {
            _runAnimationPlayed = false;
        }
    }

    public void SetGridCoordinate(Vector2Int coordinate)
    {
        GridCoordinate = coordinate;
    }

    public void PlayWakeTrigger()
    {
        if (animator == null)
        {
            return;
        }

        ResetAllAnimatorTriggers();
        animator.SetTrigger(WakeHash);
    }

    public void PlayRunAnimation()
    {
        if (animator == null || _runAnimationPlayed || IsBoxed)
        {
            return;
        }

        _runAnimationPlayed = true;
        ResetAllAnimatorTriggers();
        animator.SetTrigger(RunHash);
    }

    public void PlayJumpAnimation()
    {
        if (animator == null)
        {
            return;
        }

        ResetAllAnimatorTriggers();
        animator.SetTrigger(JumpHash);
    }

    public void PlayIdleState()
    {
        PlayAnimatorState(IdleStateHash);
    }

    public void PlayDieState()
    {
        PlayAnimatorState(DieStateHash);
    }

    public void ResetAnimatorState()
    {
        ResetAllAnimatorTriggers();
        PlayDieState();
    }

    private void ResetAllAnimatorTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(WakeHash);
        animator.ResetTrigger(RunHash);
        animator.ResetTrigger(JumpHash);
    }

    private void PlayAnimatorState(int stateHash)
    {
        if (animator == null)
        {
            return;
        }

        ResetAllAnimatorTriggers();

        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
        }

        animator.Update(0f);
    }

    public void StopMovement()
    {
        transform.DOKill();
    }

    public void SetBoxed(bool boxed)
    {
        IsBoxed = boxed;
        if (boxed)
        {
            HideInsideBox();
        }
    }

    public void HideInsideBox()
    {
        IsBoxed = true;
        SetSelectable(false);
        SetState(UnitState.Boxed);
        SetColliderEnabled(false);
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.Clear();
        }
    }

    public void RevealFromBox()
    {
        if (!IsBoxed)
        {
            return;
        }

        IsBoxed = false;
        SetState(UnitState.OnGrid);
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        SetColliderEnabled(true);
        PlayDieState();
    }

    public void ResetForPool()
    {
        StopMovement();
        ResetTrailState();
        ReleaseRuntimeMaterial();
        RestoreBaseVisual();
        SetColliderEnabled(false);

        SetState(UnitState.Pooled);
        ResetSelectableTracking();
        IsBoxed = false;
        UnitType = null;
        GridCoordinate = Vector2Int.zero;
        transform.localScale = _defaultScale;
        ResetAnimatorState();
        gameObject.SetActive(false);
    }

    public void Deactivate()
    {
        ResetForPool();
    }

    private void ResetSelectableTracking()
    {
        IsSelectable = false;
        _previousSelectable = false;
        _hasInitializedSelectableState = false;
        _runAnimationPlayed = false;
    }

    private void OnMouseDown()
    {
        if (State != UnitState.OnGrid)
        {
            return;
        }

        if (IsBoxed || !IsSelectable)
        {
            PlayBlockedFeedback();
            return;
        }

        GameplayEvents.RaiseUnitSelectionRequested(this);
    }

    private void PlayBlockedFeedback()
    {
        if (DOTween.instance == null)
        {
            return;
        }

        StopMovement();
        transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 4, 0.5f);
    }

    private void CacheComponents()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheRenderers();

        if (meshRenderer != null && _baseSharedMaterial == null)
        {
            _baseSharedMaterial = meshRenderer.sharedMaterial;
        }
    }

    private void CacheRenderers()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<Renderer>();
        }

        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }
    }

    private void ApplyVisual()
    {
        CacheRenderers();

        if (UnitType == null || meshRenderer == null)
        {
            return;
        }

        meshRenderer.enabled = true;
        ApplyTrailMaterial();
        ApplyTrailColor(UnitType.TrailColor);
        ApplyMeshVisual();
        UpdateTrailEmitting();
    }

    private void RestoreBaseVisual()
    {
        CacheRenderers();

        if (meshRenderer != null && _baseSharedMaterial != null)
        {
            meshRenderer.sharedMaterial = _baseSharedMaterial;
        }
    }

    private void ApplyTrailMaterial()
    {
        if (trailRenderer == null)
        {
            return;
        }

        if (_trailRuntimeMaterial == null)
        {
            _trailRuntimeMaterial = CreateNeutralTrailMaterial();
        }

        if (_trailRuntimeMaterial != null)
        {
            trailRenderer.sharedMaterial = _trailRuntimeMaterial;
        }
    }

    private void ApplyTrailColor(Color color)
    {
        if (trailRenderer == null)
        {
            return;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trailRenderer.colorGradient = gradient;

        if (UnitType != null)
        {
            Debug.Log($"{name} trail color applied: {UnitType.DisplayName} / {color}");
        }
    }

    private void UpdateTrailEmitting()
    {
        SetTrailEmitting(
            State == UnitState.MovingToExit
            || State == UnitState.MovingToSlot
            || State == UnitState.WaitingForSlot);
    }

    private void SetTrailEmitting(bool emitting)
    {
        CacheRenderers();

        if (trailRenderer == null)
        {
            return;
        }

        if (emitting)
        {
            if (UnitType != null)
            {
                ApplyTrailColor(UnitType.TrailColor);
            }

            trailRenderer.Clear();
        }

        trailRenderer.emitting = emitting;

        if (!emitting)
        {
            trailRenderer.Clear();
        }
    }

    private void ResetTrailState()
    {
        if (trailRenderer == null)
        {
            return;
        }

        if (UnitType != null)
        {
            ApplyTrailMaterial();
            ApplyTrailColor(UnitType.TrailColor);
        }

        trailRenderer.emitting = false;
        trailRenderer.Clear();
    }

    private void ApplyMeshVisual()
    {
        if (UnitType.Material != null)
        {
            meshRenderer.sharedMaterial = UnitType.Material;
            return;
        }

        if (UnitType.Texture != null)
        {
            if (_runtimeMaterialInstance == null)
            {
                _runtimeMaterialInstance = new Material(_baseSharedMaterial != null ? _baseSharedMaterial : meshRenderer.sharedMaterial);
            }

            meshRenderer.material = _runtimeMaterialInstance;
            _runtimeMaterialInstance.mainTexture = UnitType.Texture;
            return;
        }

        // Keep base mesh material state when no explicit unit material/texture is set.
        RestoreBaseVisual();
    }

    private void ReleaseRuntimeMaterial()
    {
        if (_runtimeMaterialInstance == null)
        {
            return;
        }

        if (_baseSharedMaterial != null)
        {
            _runtimeMaterialInstance.mainTexture = _baseSharedMaterial.mainTexture;
            _runtimeMaterialInstance.color = _baseSharedMaterial.color;
        }
        else
        {
            _runtimeMaterialInstance.mainTexture = null;
            _runtimeMaterialInstance.color = Color.white;
        }
    }

    private Material CreateNeutralTrailMaterial()
    {
        Material source = trailMaterial;
        if (source != null)
        {
            Material clone = new Material(source);
            SetMaterialToWhite(clone);
            return clone;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("Unit: Could not find a suitable trail shader.");
            return null;
        }

        Material material = new Material(shader);
        SetMaterialToWhite(material);
        return material;
    }

    private static void SetMaterialToWhite(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }
    }

    private void OnDestroy()
    {
        if (_trailRuntimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_trailRuntimeMaterial);
        }
        else
        {
            DestroyImmediate(_trailRuntimeMaterial);
        }
    }
}
