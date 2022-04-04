using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine.Objects
{
    public class BasicObject
    {
        public readonly ObjectType Type;

        public BasicObject(ObjectType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return $"/{Type}/";
        }
    }
}
