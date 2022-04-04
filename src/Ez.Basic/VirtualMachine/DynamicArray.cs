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

        public int ReadVarint(int location, out int value)
        {
            int result = 0;

            bool moreBytes;
            int bits = 0;
            int count = 0;
            do
            {
                byte readed = m_array[location];
                result |= readed << bits;
                moreBytes = (readed & 0x80) != 0;
                bits += 7;
                count++;
            } while (moreBytes);

            value = result;
            return count;
        }

        public int WriteVarint(int value)
        {
            Span<byte> tmp = stackalloc byte[5];
            int i = 0;
            do
            {
                byte coded = (byte)(value & 0x7F);
                value >>= 7;
                if (value > 0)
                    coded |= 0x80;

                tmp[i++] = coded;
            } while (value != 0);
            tmp = tmp.Slice(0, i);
            var index = m_count;

            EnsureCapacity(index + tmp.Length);

            Copy<byte>(m_array.AsSpan().Slice(m_count), tmp);
            m_count += tmp.Length;
            return index;
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
