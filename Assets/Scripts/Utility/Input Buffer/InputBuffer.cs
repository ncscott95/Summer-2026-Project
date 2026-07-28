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

    public InputBuffer(float duration, Action onBufferEnd)
    {
        _duration = duration;
        _onBufferEnd = onBufferEnd;
    }

    public void StartBuffer()
    {
        _timer = _duration;
        _isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive) return;

        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            _onBufferEnd?.Invoke();
            EndBuffer();
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

        _onBufferEnd?.Invoke();
        EndBuffer();
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

    public InputBuffer(float duration, Action<T> onBufferEnd)
    {
        _duration = duration;
        _onBufferEnd = onBufferEnd;
    }

    public void StartBuffer(T bufferedInput)
    {
        this._bufferedInput = bufferedInput;
        _timer = _duration;
        _isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void Tick(float deltaTime)
    {
        if (!_isActive) return;

        _timer -= deltaTime;
        if (_timer <= 0f)
        {
            _onBufferEnd?.Invoke(_bufferedInput);
            EndBuffer();
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

        _onBufferEnd?.Invoke(_bufferedInput);
        EndBuffer();
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
