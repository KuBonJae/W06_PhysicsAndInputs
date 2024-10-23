using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularInputBuffer : MonoBehaviour
{

    public static CircularInputBuffer instance;

    private int head { get; set; }
    private int tail { get; set; }

    public int Head
    {
        get { return head; }
        set { head = value; }
    }
    public int Tail
    {
        get { return tail; }
        set { tail = value; }
    }
    private Tuple<Vector2, float>[] buffer = new Tuple<Vector2, float>[9];

    public bool BufferIsEmpty()
    {
        return head == tail;
    }

    public bool BufferIsFull()
    {
        return (head + 1 == tail || head - 8 == tail);
    }

    public void InputToBuffer(Vector2 num, float time)
    {
        if (BufferIsFull())
        {
            buffer[tail] = new Tuple<Vector2, float>(Vector2.zero, 0f);
            tail = tail == buffer.Length - 1 ? 0 : tail + 1;
        }
        buffer[head] = new Tuple<Vector2, float>(num, time);
        head = head == buffer.Length - 1 ? 0 : head + 1;
    }

    public Tuple<Vector2, float>[] GetBuffer()
    {
        return buffer;
    }

    private void Awake()
    {
        instance = this;
    }

    private void LateUpdate()
    {
        try
        {
            int count = 0;
            for (int i = tail; ; i++)
            {
                count++;
                if (count > 10)
                    throw new InvalidOperationException($"반복 횟수가 10회를 초과했습니다.");

                if (i > instance.GetBuffer().Length - 1)
                    i = 0;

                if (i == head)
                    break;

                if (Time.time - instance.GetBuffer()[i].Item2 > 1f)
                    instance.Tail = instance.Tail == instance.GetBuffer().Length - 1 ? 0 : instance.Tail + 1;
                else break;
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
        }

    }
}
