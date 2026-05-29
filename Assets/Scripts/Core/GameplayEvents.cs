using System;
using UnityEngine;

public static class GameplayEvents
{
    public static event Action<Unit> UnitSelectionRequested;
    public static event Action RetryRequested;
    public static event Action ContinueRequested;
    public static event Func<bool> UnitsInFlightCheckRequested;
    public static event Func<Unit, bool> CanAcceptIncomingUnitCheckRequested;
    public static event Action<Unit, Action<bool>> SlotInsertionRequested;
    public static event Action SlotClearRequested;

    public static event Action SlotResolutionStarted;
    public static event Action SlotResolutionCompleted;
    public static event Action SlotQueueDrained;
    public static event Action GridCleared;
    public static event Action SlotsFullNoMatch;

    public static event Action LevelStarting;
    public static event Action<string> LevelDisplayChanged;
    public static event Action GameWon;
    public static event Action GameFailed;

    public static event Action UnitSelected;
    public static event Action BoxesBroken;
    public static event Action<Vector3> UnitsMatched;
    public static event Action<Vector3> MatchVfxRequested;

    public static void RaiseUnitSelectionRequested(Unit unit) => UnitSelectionRequested?.Invoke(unit);
    public static void RaiseRetryRequested() => RetryRequested?.Invoke();
    public static void RaiseContinueRequested() => ContinueRequested?.Invoke();
    public static bool HasUnitsInFlight()
    {
        if (UnitsInFlightCheckRequested == null)
        {
            return false;
        }

        foreach (Func<bool> handler in UnitsInFlightCheckRequested.GetInvocationList())
        {
            if (handler())
            {
                return true;
            }
        }

        return false;
    }
    public static bool CanAcceptIncomingUnit(Unit unit)
    {
        if (CanAcceptIncomingUnitCheckRequested == null)
        {
            return false;
        }

        foreach (Func<Unit, bool> handler in CanAcceptIncomingUnitCheckRequested.GetInvocationList())
        {
            if (handler(unit))
            {
                return true;
            }
        }

        return false;
    }
    public static void RaiseSlotInsertionRequested(Unit unit, Action<bool> onComplete)
    {
        if (SlotInsertionRequested == null)
        {
            onComplete?.Invoke(false);
            return;
        }

        SlotInsertionRequested.Invoke(unit, onComplete);
    }
    public static void RaiseSlotClearRequested() => SlotClearRequested?.Invoke();

    public static void RaiseSlotResolutionStarted() => SlotResolutionStarted?.Invoke();
    public static void RaiseSlotResolutionCompleted() => SlotResolutionCompleted?.Invoke();
    public static void RaiseSlotQueueDrained() => SlotQueueDrained?.Invoke();
    public static void RaiseGridCleared() => GridCleared?.Invoke();
    public static void RaiseSlotsFullNoMatch() => SlotsFullNoMatch?.Invoke();

    public static void RaiseLevelStarting() => LevelStarting?.Invoke();
    public static void RaiseLevelDisplayChanged(string displayText) => LevelDisplayChanged?.Invoke(displayText);
    public static void RaiseGameWon() => GameWon?.Invoke();
    public static void RaiseGameFailed() => GameFailed?.Invoke();

    public static void RaiseUnitSelected() => UnitSelected?.Invoke();
    public static void RaiseBoxesBroken() => BoxesBroken?.Invoke();
    public static void RaiseUnitsMatched(Vector3 position) => UnitsMatched?.Invoke(position);
    public static void RaiseMatchVfxRequested(Vector3 position) => MatchVfxRequested?.Invoke(position);
}
