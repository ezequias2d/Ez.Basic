using Ez.Basic.VirtualMachine.Attributes;
using System;
using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine
{
    public class Chunk
    {
        private DynamicArray m_code;
        private List<IAttribute> m_attributes;

        public Chunk()
        {
            m_code = new DynamicArray();
            m_attributes = new List<IAttribute>();
            
            LineNumberTable = new LineNumberTableAttribute();
            m_attributes.Add(LineNumberTable);
        }

        public LineNumberTableAttribute LineNumberTable { get; }
        
        public int Count => m_code.Count;
        public T Read<T>(int location) where T : unmanaged
        {
            //if(location >= Count)
            //    return m_code.Get<T>(Count - 1);
            return m_code.Get<T>(location);
        }

        public T Peek<T>() where T : unmanaged
        {
            return m_code.Peek<T>();
        }

        public int Write<T>(in T value, int line = -1) where T : unmanaged
        {
            var pc = m_code.Append(value);
            LineNumberTable.AddLine(pc, line);
            return pc;
        }

        public int WriteVarint(int value)
        {
            Span<byte> tmp = stackalloc byte[5];
            tmp = tmp.Slice(0, Varint.Encode(value, tmp));

            var i = 0;
            do
            {
                m_code.Append(tmp[i]);
                i++;
            } while (i < tmp.Length);

            return m_code.Count - tmp.Length;
        }

        public int ReadVariant(int location, out int value)
        {
            var src = m_code.AsSpan.Slice(location);
            return Varint.Decode(src, out value);
        }

        public void InsertVarint(int index, int value)
        {
            Span<byte> tmp = stackalloc byte[5];
            tmp = tmp.Slice(0, Varint.Encode(value, tmp));

            var i = 0;
            do
            {
                m_code.Insert(index++,tmp[i]);
                i++;
            } while (i < tmp.Length);
        }

        public void UpdateVarint(int index, int value)
        {
            // encode new value
            Span<byte> tmp = stackalloc byte[5];
            tmp = tmp.Slice(0, Varint.Encode(value, tmp));

            // read old value
            var dst = m_code.AsSpan.Slice(index, tmp.Length);
            var oldLenght = Varint.Decode(dst, out _);


            for(var i = 0; i < oldLenght && i < tmp.Length; i++)
                dst[i] = tmp[i];

            for(var i = oldLenght; i < tmp.Length; i++)
                m_code.Insert(i, tmp[i]);

            for(var i = tmp.Length; i < oldLenght; i++)
                m_code.Remove<byte>(i);
        }
    }
}
