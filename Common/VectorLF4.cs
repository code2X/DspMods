using System;

namespace Maths
{
    /// <summary>
    /// Extract from dsp VectorLF4 class
    /// </summary>
    [Serializable]
    public struct VectorLF4
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public VectorLF4(double x_, double y_, double z_, double w_)
        {
            this.x = x_;
            this.y = y_;
            this.z = z_;
            this.w = w_;
        }

        public VectorLF4(VectorLF3 v3, double w_)
        {
            this.x = v3.x;
            this.y = v3.y;
            this.z = v3.z;
            this.w = w_;
        }

        public static VectorLF4 zero => new VectorLF4(0.0, 0.0, 0.0, 0.0);

        public static VectorLF4 one => new VectorLF4(1.0, 1.0, 1.0, 1.0);

        public static VectorLF4 minusone => new VectorLF4(-1.0, -1.0, -1.0, -1.0);

        public static VectorLF4 unit_x => new VectorLF4(1.0, 0.0, 0.0, 0.0);

        public static VectorLF4 unit_y => new VectorLF4(0.0, 1.0, 0.0, 0.0);

        public static VectorLF4 unit_z => new VectorLF4(0.0, 0.0, 1.0, 0.0);

        public static VectorLF4 unit_w => new VectorLF4(0.0, 0.0, 0.0, 1.0);

        public VectorLF3 xyz => new VectorLF3(this.x, this.y, this.z);
#if UNITY_EDITOR
        public UnityEngine.Vector3 xyzf => new UnityEngine.Vector3((float)this.x, (float)this.y, (float)this.z);
#endif
        public static bool operator ==(VectorLF4 lhs, VectorLF4 rhs) => lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;

        public static bool operator !=(VectorLF4 lhs, VectorLF4 rhs) => lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z || lhs.w != rhs.w;

        public static VectorLF4 operator *(VectorLF4 lhs, VectorLF4 rhs) => new VectorLF4(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w);

        public static VectorLF4 operator *(VectorLF4 lhs, double rhs) => new VectorLF4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs);

        public static VectorLF4 operator /(VectorLF4 lhs, double rhs) => new VectorLF4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs);

        public static VectorLF4 operator -(VectorLF4 vec) => new VectorLF4(-vec.x, -vec.y, -vec.z, -vec.w);

        public static VectorLF4 operator -(VectorLF4 lhs, VectorLF4 rhs) => new VectorLF4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);

        public static VectorLF4 operator +(VectorLF4 lhs, VectorLF4 rhs) => new VectorLF4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
#if UNITY_EDITOR
        public static implicit operator VectorLF4(UnityEngine.Vector4 vec4) => new VectorLF4((double)vec4.x, (double)vec4.y, (double)vec4.z, (double)vec4.w);

        public static implicit operator UnityEngine.Vector4(VectorLF4 vec4) => new UnityEngine.Vector4((float)vec4.x, (float)vec4.y, (float)vec4.z, (float)vec4.w);
#endif
        public double sqrMagnitude => this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w;

        public double magnitude => Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w);

        public override bool Equals(object obj) => obj != null && obj is VectorLF4 vectorLf4 && (this.x == vectorLf4.x && this.y == vectorLf4.y && this.z == vectorLf4.z) && this.w == vectorLf4.w;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => string.Format("[{0},{1},{2},{3}]", (object)this.x, (object)this.y, (object)this.z, (object)this.w);
    }
}
