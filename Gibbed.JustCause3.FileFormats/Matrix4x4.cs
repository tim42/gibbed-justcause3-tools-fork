/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.FileFormats
{
    // ReSharper disable InconsistentNaming
    public struct Matrix4x4
        // ReSharper restore InconsistentNaming
    {
        public readonly float M11;
        public readonly float M12;
        public readonly float M13;
        public readonly float M14;

        public readonly float M21;
        public readonly float M22;
        public readonly float M23;
        public readonly float M24;

        public readonly float M31;
        public readonly float M32;
        public readonly float M33;
        public readonly float M34;

        public readonly float M41;
        public readonly float M42;
        public readonly float M43;
        public readonly float M44;

        public Matrix4x4(
            float m11,
            float m12,
            float m13,
            float m14,
            float m21,
            float m22,
            float m23,
            float m24,
            float m31,
            float m32,
            float m33,
            float m34,
            float m41,
            float m42,
            float m43,
            float m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }

        public static Matrix4x4 Read(Stream input, Endian endian)
        {
            var m11 = input.ReadValueF32(endian);
            var m12 = input.ReadValueF32(endian);
            var m13 = input.ReadValueF32(endian);
            var m14 = input.ReadValueF32(endian);
            var m21 = input.ReadValueF32(endian);
            var m22 = input.ReadValueF32(endian);
            var m23 = input.ReadValueF32(endian);
            var m24 = input.ReadValueF32(endian);
            var m31 = input.ReadValueF32(endian);
            var m32 = input.ReadValueF32(endian);
            var m33 = input.ReadValueF32(endian);
            var m34 = input.ReadValueF32(endian);
            var m41 = input.ReadValueF32(endian);
            var m42 = input.ReadValueF32(endian);
            var m43 = input.ReadValueF32(endian);
            var m44 = input.ReadValueF32(endian);
            return new Matrix4x4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }

        public static void Write(Stream output, Matrix4x4 value, Endian endian)
        {
            output.WriteValueF32(value.M11, endian);
            output.WriteValueF32(value.M12, endian);
            output.WriteValueF32(value.M13, endian);
            output.WriteValueF32(value.M14, endian);
            output.WriteValueF32(value.M21, endian);
            output.WriteValueF32(value.M22, endian);
            output.WriteValueF32(value.M23, endian);
            output.WriteValueF32(value.M24, endian);
            output.WriteValueF32(value.M31, endian);
            output.WriteValueF32(value.M32, endian);
            output.WriteValueF32(value.M33, endian);
            output.WriteValueF32(value.M34, endian);
            output.WriteValueF32(value.M41, endian);
            output.WriteValueF32(value.M42, endian);
            output.WriteValueF32(value.M43, endian);
            output.WriteValueF32(value.M44, endian);
        }

        public void Write(Stream output, Endian endian)
        {
            Write(output, this, endian);
        }
    }
}
