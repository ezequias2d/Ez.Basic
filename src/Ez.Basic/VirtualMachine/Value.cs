using System;

namespace Ez.Basic.VirtualMachine
{
    public struct Value
    {
        public double Double;

        public override string ToString()
        {
            return Double.ToString();
        }

        public static implicit operator Value(double d) => new Value()
        {
            Double = d
        };

        public static implicit operator double(Value d) => d.Double;

        public static Value operator -(Value value) => -value.Double;

        public static Value operator +(Value left, Value right) =>
            left.Double + right.Double;

        public static Value operator -(Value left, Value right) =>
            left.Double - right.Double;

        public static Value operator *(Value left, Value right) =>
            left.Double * right.Double;

        public static Value operator /(Value left, Value right) =>
            left.Double / right.Double;
    }
}
