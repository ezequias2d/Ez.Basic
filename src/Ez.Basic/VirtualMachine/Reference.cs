using System;

namespace Ez.Basic.VirtualMachine
{
    public struct Reference : IEquatable<Reference>
    {
        public int ID;
        internal bool Computed;

        public bool Equals(Reference other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (obj is Reference reference)
                Equals(reference);
            return base.Equals(obj);
        }

        public static bool operator ==(Reference left, Reference right) => left.Equals(right);
        public static bool operator !=(Reference left, Reference right) => !left.Equals(right);

        public override int GetHashCode() => ID.GetHashCode();

        public override string ToString() => $"[Ref {ID}]";
    }
}
