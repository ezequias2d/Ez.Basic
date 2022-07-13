using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine
{
    internal class DynamicArray
    {
        private byte[] m_array;
        private int m_count;

        public DynamicArray()
        {
            m_array = Array.Empty<byte>();
            m_count = 0;
        }

        public byte this[int index]
        {
            get
            {
                if (!(index >= 0 && index < m_count))
                    throw new IndexOutOfRangeException();

                return m_array[index];
            }
        }

        public int Count => m_count;

        public Span<byte> AsSpan => new Span<byte>(m_array, 0, m_count);

        public int Append<T>(in T value) where T : unmanaged
        {
            var index = m_count;
            int size = SizeOf<T>();
            var newCount = index + size;

            EnsureCapacity(newCount);
            m_count = newCount;

            Span<T> tmp = stackalloc T[1];
            tmp[0] = value;
            Copy<T>(m_array.AsSpan().Slice(index), tmp);

            return index;
        }

        public void Update<T>(int index, in T value) where T : unmanaged
        {
            int size = SizeOf<T>();

            if (!(index >= 0 && index < m_count && index + size <= m_count))
                throw new IndexOutOfRangeException();

            Span<T> tmp = stackalloc T[1];
            tmp[0] = value;
            Copy<T>(m_array.AsSpan().Slice(index), tmp);
        }

        public void Insert<T>(int index, in T value) where T : unmanaged
        {
            var size = SizeOf<T>();
            EnsureCapacity(m_count + size);

            if (index > m_count)
                throw new NotImplementedException();

            if (index == m_count)
            {
                Append(value);
                return;
            }

            var dst = m_array.AsSpan().Slice(index + size);
            var src = m_array.AsSpan().Slice(index, m_count - index);
            Span<byte> tmp = stackalloc byte[src.Length];
            Copy<byte>(tmp, src);
            // moves to right
            Copy<byte>(dst, tmp);
            Update(index, value);
            m_count += size;
        }
        
        public void Remove<T>(int index) where T : unmanaged
        {
            var size = SizeOf<T>();

            var dst = m_array.AsSpan().Slice(index, m_count - index - size);
            var src = m_array.AsSpan().Slice(index + size, m_count - index - size);
            Span<byte> tmp = stackalloc byte[src.Length];
            
            Copy<byte>(tmp, src);
            Copy<byte>(dst, tmp);
            m_count -= size;
        }

        public T Get<T>(int location) where T : unmanaged
        {
            Copy(out T tmp, m_array.AsSpan().Slice(location, SizeOf<T>()));
            return tmp;
        }

        public T Peek<T>() where T : unmanaged
        {
            var size = SizeOf<T>();
            var index = m_count - size;
            return Get<T>(index);
        }

        private void EnsureCapacity(int capacity)
        {
            if (m_array.Length >= capacity)
                return;

            var newCapacity = Math.Max(capacity * 2, 16);
            Array.Resize(ref m_array, newCapacity);
        }

        private unsafe int SizeOf<T>() where T : unmanaged
        {
            return sizeof(T);
        }

        private unsafe void Copy<T>(Span<byte> dst, in T source) where T : unmanaged
        {
            var size = sizeof(T);
            if (dst.Length < size)
                throw new ArgumentOutOfRangeException();

            fixed(byte* ptr = dst)
            {
                *(T*)ptr = source;
            }
        }

        private unsafe void Copy<T>(out T dst, ReadOnlySpan<byte> src) where T : unmanaged
        {
            var size = sizeof(T);
            if (size < src.Length)
                throw new ArgumentOutOfRangeException();

            fixed(T* pDst = &dst)
                fixed(byte* pSrc = src)
            {
                byte* bDst = (byte*)pDst;
                byte* bDstMax = bDst + src.Length;
                byte* bSrc = pSrc;

                while (bDst < bDstMax)
                    *bDst++ = *bSrc++;
            }
        }

        private unsafe void Copy<T>(Span<byte> dst, ReadOnlySpan<T> source) where T : unmanaged
        {
            var size = sizeof(T);
            if (dst.Length < source.Length * size)
                throw new ArgumentOutOfRangeException();

            fixed(byte* pDst = dst)
                fixed(T* pSrc = source)
            {
                T* tDst = (T*)pDst;
                T* tDstMax = tDst + source.Length * size;
                T* tSrc = pSrc;
                while(tDst < tDstMax)
                {
                    *tDst++ = *tSrc++;
                }
            }
        }
    }
}
