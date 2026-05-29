using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_VISUAL_EFFECT_GRAPH
using UnityEngine.VFX;
#endif

public class VfxManager : MonoBehaviour
{
    [FormerlySerializedAs("matchVfxPool")]
    [SerializeField] private List<GameObject> _matchVfxPool = new List<GameObject>();
    [FormerlySerializedAs("finishVfx")]
    [SerializeField] private GameObject _finishVfx;
    [FormerlySerializedAs("defaultVfxLifetime")]
    [SerializeField] private float _defaultVfxLifetime = 1.5f;
    [FormerlySerializedAs("debugLogs")]
    [SerializeField] private bool _debugLogs;

    private readonly Dictionary<GameObject, int> _deactivateTokens = new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, int> _playOrder = new Dictionary<GameObject, int>();
    private int _playSequence;
    private bool _finishVfxPlayed;

    private void Awake()
    {
        for (int i = 0; i < _matchVfxPool.Count; i++)
        {
            GameObject matchVfx = _matchVfxPool[i];
            if (matchVfx == null)
            {
                continue;
            }

            _deactivateTokens[matchVfx] = 0;
            _playOrder[matchVfx] = 0;
            matchVfx.SetActive(false);
            StopVfx(matchVfx);
        }

        ResetFinishVfx();
    }

    private void OnEnable()
    {
        GameplayEvents.MatchVfxRequested += PlayMatchVfx;
        GameplayEvents.GameWon += PlayFinishVfx;
        GameplayEvents.LevelStarting += ResetFinishVfx;
    }

    private void OnDisable()
    {
        GameplayEvents.MatchVfxRequested -= PlayMatchVfx;
        GameplayEvents.GameWon -= PlayFinishVfx;
        GameplayEvents.LevelStarting -= ResetFinishVfx;
    }

    public void PlayMatchVfx(Vector3 position)
    {
        GameObject vfxObject = GetNextMatchVfxObject();
        if (vfxObject == null)
        {
            if (_debugLogs)
            {
                Debug.Log("VfxManager: No match VFX available.");
            }

            return;
        }

        _playSequence++;
        _playOrder[vfxObject] = _playSequence;

        int token = _deactivateTokens.TryGetValue(vfxObject, out int currentToken) ? currentToken + 1 : 1;
        _deactivateTokens[vfxObject] = token;

        vfxObject.transform.position = position;
        vfxObject.SetActive(true);
        RestartVfx(vfxObject);

        float duration = GetVfxDuration(vfxObject);
        StartCoroutine(DeactivateAfterDelay(vfxObject, duration, token));
    }

    public void PlayFinishVfx()
    {
        if (_finishVfxPlayed || _finishVfx == null)
        {
            return;
        }

        _finishVfxPlayed = true;
        _finishVfx.SetActive(true);
        RestartVfx(_finishVfx);
    }

    public void ResetFinishVfx()
    {
        _finishVfxPlayed = false;

        if (_finishVfx == null)
        {
            return;
        }

        StopVfx(_finishVfx);
        _finishVfx.SetActive(false);
    }

    private GameObject GetNextMatchVfxObject()
    {
        for (int i = 0; i < _matchVfxPool.Count; i++)
        {
            GameObject vfxObject = _matchVfxPool[i];
            if (vfxObject != null && !vfxObject.activeSelf)
            {
                return vfxObject;
            }
        }

        GameObject oldest = null;
        int oldestOrder = int.MaxValue;

        for (int i = 0; i < _matchVfxPool.Count; i++)
        {
            GameObject vfxObject = _matchVfxPool[i];
            if (vfxObject == null)
            {
                continue;
            }

            int order = _playOrder.TryGetValue(vfxObject, out int value) ? value : 0;
            if (order < oldestOrder)
            {
                oldestOrder = order;
                oldest = vfxObject;
            }
        }

        return oldest;
    }

    private IEnumerator DeactivateAfterDelay(GameObject vfxObject, float delay, int token)
    {
        yield return new WaitForSeconds(delay);

        if (vfxObject == null)
        {
            yield break;
        }

        if (_deactivateTokens.TryGetValue(vfxObject, out int latestToken) && latestToken == token)
        {
            StopVfx(vfxObject);
            vfxObject.SetActive(false);
        }
    }

    private void RestartVfx(GameObject vfxObject)
    {
        if (vfxObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystems[i].Play(true);
        }

#if UNITY_VISUAL_EFFECT_GRAPH
        VisualEffect[] visualEffects = vfxObject.GetComponentsInChildren<VisualEffect>(true);
        for (int i = 0; i < visualEffects.Length; i++)
        {
            visualEffects[i].Reinit();
            visualEffects[i].Play();
        }
#endif
    }

    private float GetVfxDuration(GameObject vfxObject)
    {
        if (vfxObject == null)
        {
            return _defaultVfxLifetime;
        }

        float duration = 0f;
        ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            ParticleSystem.MainModule main = ps.main;
            float startLifetime = main.startLifetime.constantMax;
            float systemDuration = main.duration + startLifetime;
            if (systemDuration > duration)
            {
                duration = systemDuration;
            }
        }

        if (duration <= 0f)
        {
            duration = _defaultVfxLifetime;
        }

        return duration;
    }

    private static void StopVfx(GameObject vfxObject)
    {
        if (vfxObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

#if UNITY_VISUAL_EFFECT_GRAPH
        VisualEffect[] visualEffects = vfxObject.GetComponentsInChildren<VisualEffect>(true);
        for (int i = 0; i < visualEffects.Length; i++)
        {
            visualEffects[i].Stop();
        }
#endif
    }
}
