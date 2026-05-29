using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public static class UnitAnimationHelper
{
    private static bool IsDotweenAvailable => DOTween.instance != null;

    public static void MoveTo(MonoBehaviour host, Unit unit, Vector3 worldPosition, float duration, Action onComplete)
    {
        if (unit == null)
        {
            onComplete?.Invoke();
            return;
        }

        unit.StopMovement();

        if (IsDotweenAvailable)
        {
            unit.transform.DOKill();
            bool callbackInvoked = false;
            unit.transform.DOMove(worldPosition, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    callbackInvoked = true;
                    unit.transform.position = worldPosition;
                    onComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    if (callbackInvoked)
                    {
                        return;
                    }

                    callbackInvoked = true;
                    onComplete?.Invoke();
                });
            return;
        }

        host.StartCoroutine(MoveCoroutine(unit.transform, worldPosition, duration, onComplete));
    }

    public static void MoveUnitWithHop(
        MonoBehaviour host,
        Unit unit,
        Vector3 start,
        Vector3 target,
        float hopHeight,
        float duration,
        Action onComplete)
    {
        if (unit == null)
        {
            onComplete?.Invoke();
            return;
        }

        unit.StopMovement();
        host.StartCoroutine(MoveUnitWithHopRoutine(unit, start, target, hopHeight, duration, onComplete));
    }

    public static IEnumerator MoveUnitWithHopRoutine(
        Unit unit,
        Vector3 start,
        Vector3 target,
        float hopHeight,
        float duration)
    {
        if (unit == null)
        {
            yield break;
        }

        Transform unitTransform = unit.transform;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Vector3 flatPosition = Vector3.Lerp(start, target, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * hopHeight;
            unitTransform.position = flatPosition + Vector3.up * heightOffset;
            yield return null;
        }

        unitTransform.position = target;
    }

    private static IEnumerator MoveUnitWithHopRoutine(
        Unit unit,
        Vector3 start,
        Vector3 target,
        float hopHeight,
        float duration,
        Action onComplete)
    {
        yield return MoveUnitWithHopRoutine(unit, start, target, hopHeight, duration);
        onComplete?.Invoke();
    }

    private static IEnumerator MoveCoroutine(Transform target, Vector3 end, float duration, Action onComplete)
    {
        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        target.position = end;
        onComplete?.Invoke();
    }
}
