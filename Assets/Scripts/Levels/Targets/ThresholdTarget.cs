using UnityEngine;

public class ThresholdTarget : LevelTarget
{
    private float _currentValue;

    public override void Initialize(LevelTargetItem targetItem)
    {
        base.Initialize(targetItem);

        _currentValue = _targetItem.ModValue;
    }

    public override void OnTargetHit()
    {
        // TODO: decrease _currentValue by the amount of points scored by the player's hit
        if (_currentValue <= 0)
        {
            base.OnTargetHit();
        }
    }
}
