/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.JustCause3.FileFormats;
using Gibbed.IO;

namespace Gibbed.JustCause3.RenderBlockModel.Blocks
{
    public struct SkinnedGeneralData0Big : IFormat
    {
        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public byte TexCoord1A;
        public byte TexCoord1B;
        public byte TexCoord1C;
        public byte TexCoord1D;

        public byte TexCoord2A;
        public byte TexCoord2B;
        public byte TexCoord2C;
        public byte TexCoord2D;

        public byte TexCoord3A;
        public byte TexCoord3B;
        public byte TexCoord3C;
        public byte TexCoord3D;

        public byte TexCoord4A;
        public byte TexCoord4B;
        public byte TexCoord4C;
        public byte TexCoord4D;

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueF32(this.PositionX);
            output.WriteValueF32(this.PositionY);
            output.WriteValueF32(this.PositionZ);

            output.WriteValueU8(this.TexCoord1A);
            output.WriteValueU8(this.TexCoord1B);
            output.WriteValueU8(this.TexCoord1C);
            output.WriteValueU8(this.TexCoord1D);

            output.WriteValueU8(this.TexCoord2A);
            output.WriteValueU8(this.TexCoord2B);
            output.WriteValueU8(this.TexCoord2C);
            output.WriteValueU8(this.TexCoord2D);

            output.WriteValueU8(this.TexCoord3A);
            output.WriteValueU8(this.TexCoord3B);
            output.WriteValueU8(this.TexCoord3C);
            output.WriteValueU8(this.TexCoord3D);

            output.WriteValueU8(this.TexCoord4A);
            output.WriteValueU8(this.TexCoord4B);
            output.WriteValueU8(this.TexCoord4C);
            output.WriteValueU8(this.TexCoord4D);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.PositionX = input.ReadValueF32();
            this.PositionY = input.ReadValueF32();
            this.PositionZ = input.ReadValueF32();

            this.TexCoord1A = input.ReadValueU8();
            this.TexCoord1B = input.ReadValueU8();
            this.TexCoord1C = input.ReadValueU8();
            this.TexCoord1D = input.ReadValueU8();

            this.TexCoord2A = input.ReadValueU8();
            this.TexCoord2B = input.ReadValueU8();
            this.TexCoord2C = input.ReadValueU8();
            this.TexCoord2D = input.ReadValueU8();

            this.TexCoord3A = input.ReadValueU8();
            this.TexCoord3B = input.ReadValueU8();
            this.TexCoord3C = input.ReadValueU8();
            this.TexCoord3D = input.ReadValueU8();

            this.TexCoord4A = input.ReadValueU8();
            this.TexCoord4B = input.ReadValueU8();
            this.TexCoord4C = input.ReadValueU8();
            this.TexCoord4D = input.ReadValueU8();
        }
    }
}
