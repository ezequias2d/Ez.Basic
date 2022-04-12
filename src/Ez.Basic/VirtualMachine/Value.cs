using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Ez.Basic.VirtualMachine
{
    
    public struct Value : IEquatable<Value>
    {
        internal AsValue m_as;
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

        public Reference ObjectReference
        {
            get
            {
                switch(Type)
                {
                    case ValueType.Object:
                        return m_as.Reference;
                }
                throw new InvalidOperationException();
            }
            set
            {
                if(value.ID < 0)
                {
                    Type = ValueType.Nil;
                    m_as.Reference.ID = -1;
                    return;
                }
                Type = ValueType.Object;
                m_as.Reference = value;
            }
        }

        //public BasicString String => (BasicString)Object;
        //public string NetString => String.Value;

        public bool IsNumber => Type == ValueType.Number;
        public bool IsBool => Type == ValueType.Bool;
        public bool IsNil => Type == ValueType.Nil;
        public bool IsObject => Type == ValueType.Object;
        //public bool IsString => IsType(ObjectType.String);
        
        //public bool IsType(ObjectType type)
        //{
        //    return IsObject && m_object.Type == type;
        //}

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
                    return "$" + ObjectReference.ID.ToString("X");
            }
            return "{Undefined value type}";
        }

        public static implicit operator Value(double d) => new() { Number = d };

        public static implicit operator double(Value d) => d.Number;

        public static implicit operator Value(bool b) => new() { Boolean = b };

        public static implicit operator bool(Value d) => d.Boolean;

        public static explicit operator int(Value d) => (int)d.Number;

        public static explicit operator Value(int i) => new() { Number = i };
        public static implicit operator Value(Reference r) => new() {  ObjectReference = r};
        public static implicit operator Reference(Value r) => r.ObjectReference;
        //public static implicit operator BasicObject(Value d) => d.Object;
        //public static implicit operator Value(BasicObject o) => new Value() { Object = o };

        public static Value MakeNull() => new Value() { ObjectReference = new() { ID = -1, Computed = true, } };
        public static Value MakeObjectReference(int reference) => new() { ObjectReference = new() { ID = reference , Computed = false } };

        public bool Equals(Value other)
        {
            if(Type == other.Type)
            {
                switch(Type)
                {
                    case ValueType.Bool:
                        return Boolean == other.Boolean;
                    case ValueType.Nil:
                        return true;
                    case ValueType.Number:
                        return Number == other.Number;
                    case ValueType.Object:
                        return ObjectReference == other.ObjectReference;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if(obj is Reference reference)
                return Equals(reference);
            return false;
        }

        public override int GetHashCode()
        {
            switch(Type)
            {
                case ValueType.Bool:
                    return HashCode.Combine(ValueType.Bool, Boolean);
                case ValueType.Nil:
                    return HashCode.Combine(ValueType.Nil);
                case ValueType.Number:
                    return HashCode.Combine(ValueType.Number, Number);
                case ValueType.Object:
                    return HashCode.Combine(ValueType.Object, ObjectReference);
            }
            return base.GetHashCode();
        }

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

        public static Value operator %(Value left, Value right) =>
            left.Number % right.Number;

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
                    return left.ObjectReference.ID == right.ObjectReference.ID;
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
                    return left.ObjectReference.ID != right.ObjectReference.ID;
                default:
                    return true;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct AsValue
        {
            [FieldOffset(0)]
            public Reference Reference;

            [FieldOffset(0)]
            public bool Boolean;

            [FieldOffset(0)]
            public double Double;
        };
    }
}
