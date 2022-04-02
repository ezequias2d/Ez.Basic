using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ez.Basic.VirtualMachine.Attributes
{
    internal class ClassFileAttribute : IAttribute
    {
        public string Name { get; set; }
        public int SourceFileIndex { get; set; }
    }
}
