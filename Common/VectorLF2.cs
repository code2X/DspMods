using System;

namespace Maths
{
    /// <summary>
    ///Extract from dsp VectorLF2 class
    /// </summary>
    [Serializable]
    public struct VectorLF2
    {
        public double x;
        public double y;

        public VectorLF2(VectorLF2 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
        }

        public VectorLF2(double x_, double y_)
        {
            this.x = x_;
            this.y = y_;
        }

        public VectorLF2(float x_, float y_)
        {
            this.x = (double)x_;
            this.y = (double)y_;
        }

        public static VectorLF2 zero => new VectorLF2(0.0f, 0.0f);

        public static VectorLF2 one => new VectorLF2(1f, 1f);

        public static VectorLF2 minusone => new VectorLF2(-1f, -1f);

        public static VectorLF2 unit_x => new VectorLF2(1f, 0.0f);

        public static VectorLF2 unit_y => new VectorLF2(0.0f, 1f);

        public static bool operator ==(VectorLF2 lhs, VectorLF2 rhs) => lhs.x == rhs.x && lhs.y == rhs.y;

        public static bool operator !=(VectorLF2 lhs, VectorLF2 rhs) => lhs.x != rhs.x || lhs.y != rhs.y;

        public static VectorLF2 operator *(VectorLF2 lhs, VectorLF2 rhs) => new VectorLF2(lhs.x * rhs.x, lhs.y * rhs.y);

        public static VectorLF2 operator *(VectorLF2 lhs, double rhs) => new VectorLF2(lhs.x * rhs, lhs.y * rhs);

        public static VectorLF2 operator /(VectorLF2 lhs, double rhs) => new VectorLF2(lhs.x / rhs, lhs.y / rhs);

        public static VectorLF2 operator -(VectorLF2 vec) => new VectorLF2(-vec.x, -vec.y);

        public static VectorLF2 operator -(VectorLF2 lhs, VectorLF2 rhs) => new VectorLF2(lhs.x - rhs.x, lhs.y - rhs.y);

        public static VectorLF2 operator +(VectorLF2 lhs, VectorLF2 rhs) => new VectorLF2(lhs.x + rhs.x, lhs.y + rhs.y);

#if UNITY_EDITOR
        public static implicit operator VectorLF2(UnityEngine.Vector2 vec2) => new VectorLF2(vec2.x, vec2.y);

        public static implicit operator UnityEngine.Vector2(VectorLF2 vec2) => new UnityEngine.Vector2((float)vec2.x, (float)vec2.y);
#endif
        public double sqrMagnitude => this.x * this.x + this.y * this.y;

        public double magnitude => Math.Sqrt(this.x * this.x + this.y * this.y);

        public double Distance(VectorLF2 vec) => Math.Sqrt((vec.x - this.x) * (vec.x - this.x) + (vec.y - this.y) * (vec.y - this.y));

        public override bool Equals(object obj) => obj != null && obj is VectorLF2 vectorLf2 && this.x == vectorLf2.x && this.y == vectorLf2.y;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => string.Format("[{0},{1}]", (object)this.x, (object)this.y);

        public VectorLF2 normalized
        {
            get
            {
                double d = this.x * this.x + this.y * this.y;
                if (d < 1E-34)
                    return new VectorLF2(0.0f, 0.0f);
                double num = Math.Sqrt(d);
                return new VectorLF2(this.x / num, this.y / num);
            }
        }
    }
}
