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

namespace Gibbed.JustCause3.RenderBlockModel
{
    public struct UnknownData0 : IFormat
    {
        public float Unknown00;
        public float Unknown04;
        public float Unknown08;
        public float Unknown0C;
        public float Unknown10;
        public float Unknown14;
        public float Unknown18;
        public float Unknown1C;
        public float Unknown20;
        public float Unknown24;
        public float Unknown28;
        public float Unknown2C;
        public float Unknown30;
        public uint Unknown34;

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Unknown00 = input.ReadValueF32(endian);
            this.Unknown04 = input.ReadValueF32(endian);
            this.Unknown08 = input.ReadValueF32(endian);
            this.Unknown0C = input.ReadValueF32(endian);
            this.Unknown10 = input.ReadValueF32(endian);
            this.Unknown14 = input.ReadValueF32(endian);
            this.Unknown18 = input.ReadValueF32(endian);
            this.Unknown1C = input.ReadValueF32(endian);
            this.Unknown20 = input.ReadValueF32(endian);
            this.Unknown24 = input.ReadValueF32(endian);
            this.Unknown28 = input.ReadValueF32(endian);
            this.Unknown2C = input.ReadValueF32(endian);
            this.Unknown30 = input.ReadValueF32(endian);
            this.Unknown34 = input.ReadValueU32(endian);
        }
    }
}
