using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic
{
    internal class Stack<T> : IEnumerable<T>
    {
        private T[] m_array;
        private int m_count;

        public Stack(int initialSize = 0)
        {
            if(initialSize == 0)
                m_array = Array.Empty<T>();
            else
                m_array = new T[initialSize];
            m_count = 0;
        }

        public bool Has => m_count > 0;
        public int Count => m_count;

        public void Push(T value)
        {
            EnsureSize(m_count + 1);
            m_array[m_count++] = value;
        }

        public bool TryPop(out T value)
        {
            if(m_count <= 0)
            {
                value = default;
                return false;
            }

            value = m_array[--m_count];
            m_array[m_count] = default;
            return true;
        }

        public T Pop()
        {
            var result = m_array[--m_count];
            m_array[m_count] = default;
            return result;
        }

        public void Pop(int n)
        {
            m_count -= n;
        }

        public ref T Peek(int n = 0)
        {
            return ref m_array[m_count - 1 - n];
        }

        public void Reset()
        {
            Array.Clear(m_array, 0, m_count);
            m_count = 0;
        }

        private void EnsureSize(int size)
        {
            if (m_array != null && m_array.Length >= size)
                return;

            var newSize = Math.Max(m_array.Length * 2, Math.Max(size, 16));
            newSize = Math.Max(newSize, size);
            Array.Resize(ref m_array, newSize);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < m_count; i++)
            {
                yield return m_array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
