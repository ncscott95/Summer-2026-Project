using System.Collections;
using UnityEngine;

public class TimerTarget : LevelTarget
{
    float _timerValue;

    public override void Initialize(LevelTargetItem targetItem)
    {
        base.Initialize(targetItem);

        StartCoroutine(TimerCoroutine());
    }

    public override void OnTargetHit()
    {
        base.OnTargetHit();
    }

    private IEnumerator TimerCoroutine()
    {
        _timerValue = _targetItem.ModValue;

        while (_timerValue > 0)
        {
            _timerValue -= Time.deltaTime;
            // TODO: update visuals based on timer value
            yield return null;
        }

        // TODO: temp, add logic for what happens when the timer runs out
        OnTargetHit();
    }
}
