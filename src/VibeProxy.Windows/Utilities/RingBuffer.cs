using System;
using System.Collections.Generic;

namespace VibeProxy.Windows.Utilities;

public sealed class RingBuffer<T>
{
    private readonly T?[] _storage;
    private int _head;
    private int _tail;
    private int _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _storage = new T?[capacity];
    }

    public int Count => _count;

    public void Add(T item)
    {
        _storage[_tail] = item;
        _tail = (_tail + 1) % _storage.Length;

        if (_count == _storage.Length)
        {
            _head = (_head + 1) % _storage.Length;
        }
        else
        {
            _count++;
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        if (_count == 0)
        {
            return Array.Empty<T>();
        }

        var result = new T[_count];
        for (var i = 0; i < _count; i++)
        {
            var index = (_head + i) % _storage.Length;
            result[i] = _storage[index]!;
        }

        return result;
    }
}
