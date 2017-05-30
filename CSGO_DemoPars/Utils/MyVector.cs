using DemoInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_DemoPars.Utils
{
    class MyVector : Vector
    {
        protected const float EqualityVariance = 0.0001f;

        public MyVector(float X, float Y, float Z) : base(X, Y, Z)
        {
        }

        public override bool Equals(object obj)
        {
            Vector vector = obj as Vector;
            if (vector == null) return false;

            return Equals((Vector)vector);
        }

        public bool Equals(Vector vector)
        {
            return Math.Abs(this.X - vector.X) < EqualityVariance &&
                   Math.Abs(this.Y - vector.Y) < EqualityVariance &&
                   Math.Abs(this.Z - vector.Z) < EqualityVariance;
        }
    }
}
