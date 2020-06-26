using System;

namespace LogList.Control.Manipulation
{
    public class ViewWindow
    {
        public ViewWindow(int Offset, int Size)
        {
            this.Offset = Offset;
            this.Size   = Size;
        }

        public int Offset { get; }
        public int Size   { get; }

        protected bool Equals(ViewWindow other)
        {
            return Offset == other.Offset && Size == other.Size;
        }

        public override string ToString()
        {
            return $"{Offset} -> {Size}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ViewWindow) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Offset, Size);
        }

        public static bool operator ==(ViewWindow left, ViewWindow right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ViewWindow left, ViewWindow right)
        {
            return !Equals(left, right);
        }
    }
}