using System;
using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine
{
    public class GC
    {
        private Dictionary<object, ObjectData> m_heapInfo;
        private List<object> m_heap;
        private List<int> m_freeIndexes;

        public GC()
        {
            m_heapInfo = new Dictionary<object, ObjectData>();
            m_heap = new List<object>();
            m_freeIndexes = new List<int>();
        }

        public void Reset()
        {
            m_heapInfo.Clear();
            m_heap.Clear();
            m_freeIndexes.Clear();
        }

        public Reference AddObject(object obj, bool constant = false)
        {
            if(!m_heapInfo.TryGetValue(obj, out var data))
            {
                int index;
                if(m_freeIndexes.Count == 0)
                {
                    m_heap.Add(obj);
                    index = m_heap.Count - 1;
                }
                else
                {
                    index = m_freeIndexes[m_freeIndexes.Count - 1];
                    m_freeIndexes.RemoveAt(m_freeIndexes.Count - 1);
                    m_heap[index] = obj;
                }

                data = new ObjectData()
                {
                    Index = index,
                    ReferenceCount = 0,
                };
            }
            if(constant)
                data.ReferenceCount = -1;
            else
                data.ReferenceCount++;
            m_heapInfo[obj] = data;
            return new() { ID = data.Index, Computed = true };
        }

        public object GetObject(Reference reference)
        {
            if (reference.ID >= m_heap.Count)
                // the id is not in the heap
                throw new NotImplementedException();

            var obj = m_heap[reference.ID];

            if (obj == null)
                // the object has been disposed, this probably is a bug.
                throw new NotImplementedException();

            return obj;
        }

        private void RemoveObject(object obj)
        {
            if (!m_heapInfo.TryGetValue(obj, out var data))
                // the object does exist, so must be a bug
                throw new NotImplementedException();
            
            // constant
            if(data.ReferenceCount == -1)
                return;

            if(--data.ReferenceCount == 0)
            {
                // disposes
                m_freeIndexes.Add(data.Index);
                m_heap[data.Index] = null;
                m_heapInfo.Remove(obj);
            }
            else
                m_heapInfo[obj] = data;
        }

        public void RemoveObject(ref Reference reference)
        {
            var obj = GetObject(reference);
            if(reference.Computed)
            {
                RemoveObject(obj);
                reference.Computed = false;
            }
        }

        private struct ObjectData
        {
            public int ReferenceCount;
            public int Index;
        }
    }
}
