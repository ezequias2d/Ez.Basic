using System;
using System.Collections.Generic;
using System.Text;

namespace Ez.Basic.VirtualMachine.Objects
{
    public class BasicString : BasicObject
    {
        public BasicString(string value) : base(ObjectType.String)
        {
            Value = value;
        }

        public readonly string Value;

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            string other;
            if (obj is BasicString bs)
                other = bs.Value;
            else if (obj is string s)
                other = s;
            else
                return false;

            return Value.Equals(other);
        }

        public static bool operator ==(BasicString left, BasicString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BasicString left, BasicString right)
        {
            return !left.Equals(right);
        }

        public static BasicString operator +(BasicString left, BasicString right)
        {
            return new BasicString(left.Value + right.Value);
        }
    }
}
