using System;

public interface IInputBuffer
{
    void Tick(float deltaTime);
    bool Interrupt();
    bool TryForceEnd();
}

public class InputBuffer : IInputBuffer
{
    private Action _onBufferEnd;

    private bool _isActive = false;
    private float _duration;
    private float _timer;
    private int _activationVersion;

    public InputBuffer(float duration, Action onBufferEnd)
    {
        _duration = duration;
        _onBufferEnd = onBufferEnd;
    }

    public void StartBuffer()
    {
        _activationVersion++;
        _timer = _duration;
        _isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void StartBuffer(float duration)
    {
        _duration = duration;
        StartBuffer();
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive) return;

        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            int completedActivationVersion = _activationVersion;
            _onBufferEnd?.Invoke();
            if (_activationVersion == completedActivationVersion)
            {
                EndBuffer();
            }
        }
    }

    public bool Interrupt()
    {
        if (!_isActive) return false;

        EndBuffer();
        return true;
    }

    public bool TryForceEnd()
    {
        if (!_isActive) return false;

        int completedActivationVersion = _activationVersion;
        _onBufferEnd?.Invoke();
        if (_activationVersion == completedActivationVersion)
        {
            EndBuffer();
        }
        return true;
    }

    private void EndBuffer()
    {
        _isActive = false;
        _timer = 0f;
        InputBufferList.RemoveBuffer(this);
    }
}

public class InputBuffer<T> : IInputBuffer
{
    private Action<T> _onBufferEnd;
    private T _bufferedInput;

    private bool _isActive = false;
    private float _duration;
    private float _timer;
    private int _activationVersion;

    public InputBuffer(float duration, Action<T> onBufferEnd)
    {
        _duration = duration;
        _onBufferEnd = onBufferEnd;
    }

    public void StartBuffer(T bufferedInput)
    {
        this._bufferedInput = bufferedInput;
        _activationVersion++;
        _timer = _duration;
        _isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void StartBuffer(float duration, T bufferedInput)
    {
        _duration = duration;
        StartBuffer(bufferedInput);
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive) return;

        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            int completedActivationVersion = _activationVersion;
            _onBufferEnd?.Invoke(_bufferedInput);
            if (_activationVersion == completedActivationVersion)
            {
                EndBuffer();
            }
        }
    }

    public bool Interrupt()
    {
        if (!_isActive) return false;

        EndBuffer();
        return true;
    }

    public bool TryForceEnd()
    {
        if (!_isActive) return false;

        int completedActivationVersion = _activationVersion;
        _onBufferEnd?.Invoke(_bufferedInput);
        if (_activationVersion == completedActivationVersion)
        {
            EndBuffer();
        }
        return true;
    }

    private void EndBuffer()
    {
        _isActive = false;
        _timer = 0f;
        InputBufferList.RemoveBuffer(this);
    }

    public T GetLastBufferedInput()
    {
        return _bufferedInput;
    }
}
