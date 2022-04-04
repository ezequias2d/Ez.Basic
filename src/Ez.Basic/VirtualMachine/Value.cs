using Ez.Basic.VirtualMachine.Objects;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ez.Basic.VirtualMachine
{
    
    public struct Value
    {
        private AsValue m_as;
        private BasicObject m_object;
        public ValueType Type;


        public double Number
        {
            get
            {
                switch(Type)
                {
                    case ValueType.Number:
                        return m_as.Double;
                    case ValueType.Bool:
                        return m_as.Boolean ? 1.0 : 0.0;
                    case ValueType.Nil:
                        return 0.0;
                    case ValueType.Object:
                        return 0.0;
                }
                throw new InvalidOperationException();
            }
            set
            {
                Type = ValueType.Number;
                m_as.Double = value;
            }
        }

        public bool Boolean
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Number:
                        return m_as.Double != 0;
                    case ValueType.Bool:
                        return m_as.Boolean;
                    case ValueType.Nil:
                        return false;
                    case ValueType.Object:
                        return true;
                }
                throw new InvalidOperationException();
            }
            set
            {
                Type = ValueType.Bool;
                m_as.Boolean = value;
            }
        }

        public BasicObject Object
        {
            get
            {
                switch(Type)
                {
                    case ValueType.Object:
                        return m_object;
                }
                throw new InvalidOperationException();
            }
            set
            {
                if(value is null)
                {
                    Type = ValueType.Nil;
                    m_object = null;
                    return;
                }
                Type = ValueType.Object;
                m_object = value;
            }
        }

        public BasicString String => (BasicString)Object;
        public string NetString => String.Value;

        public bool IsNumber => Type == ValueType.Number;
        public bool IsBool => Type == ValueType.Bool;
        public bool IsNil => Type == ValueType.Nil;
        public bool IsObject => Type == ValueType.Object;
        public bool IsString => IsType(ObjectType.String);
        
        public bool IsType(ObjectType type)
        {
            return IsObject && m_object.Type == type;
        }

        public override string ToString()
        {
            switch(Type)
            {
                case ValueType.Number:
                    return Number.ToString();
                case ValueType.Bool:
                    return Boolean.ToString();
                case ValueType.Nil:
                    return "NULL";
                case ValueType.Object:
                    return Object.ToString();
            }
            return "{Undefined value type}";
        }

        public static implicit operator Value(double d) => new Value() { Number = d };

        public static implicit operator double(Value d) => d.Number;

        public static implicit operator Value(bool b) => new Value() { Boolean = b };

        public static implicit operator bool(Value d) => d.Boolean;
        public static implicit operator BasicObject(Value d) => d.Object;
        public static implicit operator Value(BasicObject o) => new Value() { Object = o };

        public static Value MakeNull() => new Value() { Object = null };

        public static Value operator -(Value value)
        {
            if(value.Type == ValueType.Number)
            {
                return -value.Number;
            }
            Debug.Assert(false);
            return value;
        }

        public static Value operator +(Value left, Value right) =>
            left.Number + right.Number;

        public static Value operator -(Value left, Value right) =>
            left.Number - right.Number;

        public static Value operator *(Value left, Value right) =>
            left.Number * right.Number;

        public static Value operator /(Value left, Value right) =>
            left.Number / right.Number;

        public static bool operator ==(Value left, Value right)
        {
            if (left.Type != right.Type)
                return false;

            switch(left.Type)
            {
                case ValueType.Bool:
                    return left.Boolean == right.Boolean;
                case ValueType.Nil:
                    return true;
                case ValueType.Number:
                    return left.Number == right.Number;
                case ValueType.Object:
                    return left.Object == right.Object;
                default:
                    return false;
            }
        }

        public static bool operator !=(Value left, Value right)
        {
            if (left.Type != right.Type)
                return true;

            switch (left.Type)
            {
                case ValueType.Bool:
                    return left.Boolean != right.Boolean;
                case ValueType.Nil:
                    return false;
                case ValueType.Number:
                    return left.Number != right.Number;
                case ValueType.Object:
                    return left.Object != right.Object;
                default:
                    return true;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct AsValue
        {
            [FieldOffset(0)]
            public bool Boolean;

            [FieldOffset(0)]
            public double Double;
        };
    }
}
