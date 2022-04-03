using System;
using System.Collections.Generic;

namespace Ez.Basic.VirtualMachine.Attributes
{
    public class LineNumberTableAttribute : IAttribute
    {
        private List<Entry> m_list;
        private int m_lastPc;
        private int m_lastLine;
        public string Name { get; set; }

        //public LineNumberEntry[] Table { get; set; }

        public LineNumberTableAttribute()
        {
            Name = nameof(LineNumberTableAttribute);
            m_list = new List<Entry>();
            m_lastPc = -1;
        }

        public void AddLine(int pc, int line)
        {
            if (pc <= m_lastPc)
                throw new ArgumentOutOfRangeException(nameof(pc));

            m_lastPc = pc;
            if(line > m_lastLine)
            {
                m_list.Add(new Entry(pc, line));
                m_lastLine = line;
            }
        }

        public int GetLine(int pc)
        {
            return Search(0, m_list.Count - 1, pc);
        }

        private int Search(in int first, in int last, in int pc)
        {
            if (last < first)
                return -1;

            var mid = first + (last - first) / 2;

            if (CheckPc(mid, pc))
                return mid;

            if (m_list[mid].StartPC > pc)
                return Search(first, mid - 1, pc);

            return Search(mid + 1, last, pc);
        }

        private bool CheckPc(int index, int targetPc)
        {
            var nextIndex = index + 1;
            var count = m_list.Count;
            return index < count && m_list[index].StartPC <= targetPc &&
                (nextIndex == count || m_list[nextIndex].StartPC > targetPc);
        }

        private struct Entry
        {
            /// <summary>
            /// Start program counter
            /// </summary>
            public int StartPC;

            /// <summary>
            /// The line of code.
            /// </summary>
            public int LineNumber;

            public Entry(int startPc, int lineNumber)
            {
                StartPC = startPc;
                LineNumber = lineNumber;
            }
        }
    }
}
