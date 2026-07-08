using System;
using System.Collections.Generic;

public static class InputBufferList
{
    private static readonly List<IInputBuffer> activeBuffers = new List<IInputBuffer>();
    private static readonly List<IInputBuffer> buffersToAdd = new List<IInputBuffer>();
    private static readonly List<IInputBuffer> buffersToRemove = new List<IInputBuffer>();

    public static void TickAll(float deltaTime)
    {
        foreach (IInputBuffer buffer in activeBuffers)
        {
            buffer.Tick(deltaTime);
        }

        foreach (IInputBuffer buffer in buffersToRemove)
        {
            if (activeBuffers.Contains(buffer))
            {
                activeBuffers.Remove(buffer);
            }
        }

        foreach (IInputBuffer buffer in buffersToAdd)
        {
            if (!activeBuffers.Contains(buffer))
            {
                activeBuffers.Add(buffer);
            }
        }

        buffersToAdd.Clear();
        buffersToRemove.Clear();
    }

    public static void AddBuffer(IInputBuffer buffer)
    {
        if (!activeBuffers.Contains(buffer))
        {
            buffersToAdd.Add(buffer);
        }
    }

    public static void RemoveBuffer(IInputBuffer buffer)
    {
        if (activeBuffers.Contains(buffer))
        {
            buffersToRemove.Add(buffer);
        }
    }
}
