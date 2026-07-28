using System;
using System.Collections.Generic;

public static class InputBufferList
{
    private static readonly List<IInputBuffer> _activeBuffers = new List<IInputBuffer>();
    private static readonly List<IInputBuffer> _buffersToAdd = new List<IInputBuffer>();
    private static readonly List<IInputBuffer> _buffersToRemove = new List<IInputBuffer>();

    public static void TickAll(float deltaTime)
    {
        foreach (IInputBuffer buffer in _activeBuffers)
        {
            buffer.Tick(deltaTime);
        }

        foreach (IInputBuffer buffer in _buffersToRemove)
        {
            if (_activeBuffers.Contains(buffer))
            {
                _activeBuffers.Remove(buffer);
            }
        }

        foreach (IInputBuffer buffer in _buffersToAdd)
        {
            if (!_activeBuffers.Contains(buffer))
            {
                _activeBuffers.Add(buffer);
            }
        }

        _buffersToAdd.Clear();
        _buffersToRemove.Clear();
    }

    public static void AddBuffer(IInputBuffer buffer)
    {
        if (!_activeBuffers.Contains(buffer))
        {
            _buffersToAdd.Add(buffer);
        }
    }

    public static void RemoveBuffer(IInputBuffer buffer)
    {
        if (_activeBuffers.Contains(buffer))
        {
            _buffersToRemove.Add(buffer);
        }
    }
}
