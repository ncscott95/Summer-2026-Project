using System;

public interface IInputBuffer
{
    void Tick(float deltaTime);
    bool Interrupt();
    bool TryForceEnd();
}

public class InputBuffer : IInputBuffer
{
    private Action onBufferEnd;

    private bool isActive = false;
    private float duration;
    private float timer;

    public InputBuffer(float duration, Action onBufferEnd)
    {
        this.duration = duration;
        this.onBufferEnd = onBufferEnd;
    }

    public void StartBuffer()
    {
        timer = duration;
        isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void Tick(float deltaTime)
    {
        if (!isActive) return;

        timer -= deltaTime;
        if (timer <= 0f)
        {
            onBufferEnd?.Invoke();
            EndBuffer();
        }
    }

    public bool Interrupt()
    {
        if (!isActive) return false;

        EndBuffer();
        return true;
    }

    public bool TryForceEnd()
    {
        if (!isActive) return false;

        onBufferEnd?.Invoke();
        EndBuffer();
        return true;
    }

    private void EndBuffer()
    {
        isActive = false;
        timer = 0f;
        InputBufferList.RemoveBuffer(this);
    }
}

public class InputBuffer<T> : IInputBuffer
{
    private Action<T> onBufferEnd;
    private T bufferedInput;

    private bool isActive = false;
    private float duration;
    private float timer;

    public InputBuffer(float duration, Action<T> onBufferEnd)
    {
        this.duration = duration;
        this.onBufferEnd = onBufferEnd;
    }

    public void StartBuffer(T bufferedInput)
    {
        this.bufferedInput = bufferedInput;
        timer = duration;
        isActive = true;
        InputBufferList.AddBuffer(this);
    }

    public void Tick(float deltaTime)
    {
        if (!isActive) return;

        timer -= deltaTime;
        if (timer <= 0f)
        {
            onBufferEnd?.Invoke(bufferedInput);
            EndBuffer();
        }
    }

    public bool Interrupt()
    {
        if (!isActive) return false;

        EndBuffer();
        return true;
    }

    public bool TryForceEnd()
    {
        if (!isActive) return false;

        onBufferEnd?.Invoke(bufferedInput);
        EndBuffer();
        return true;
    }

    private void EndBuffer()
    {
        isActive = false;
        timer = 0f;
        InputBufferList.RemoveBuffer(this);
    }

    public T GetLastBufferedInput()
    {
        return bufferedInput;
    }
}
