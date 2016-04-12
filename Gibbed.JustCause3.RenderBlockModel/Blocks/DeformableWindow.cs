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
    public class DeformableWindow : IRenderBlock
    {
        public byte Version;
        public Material Material;
        public DeformTable DeformTable = new DeformTable();
        public readonly List<DeformableWindowData0> VertexData0 = new List<DeformableWindowData0>();
        public List<short> Faces = new List<short>();
        public uint Unknown24C;

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var version = input.ReadValueU8();
            if (version > 2)
            {
                throw new FormatException("unhandled version for DeformableWindow");
            }

            this.Version = version;
            this.Material.Deserialize(input, endian);

            if (version >= 2)
            {
                this.Unknown24C = input.ReadValueU32(endian);
                this.DeformTable.Deserialize(input, endian);
            }

            input.ReadArray(this.VertexData0, endian);
            input.ReadFaces(this.Faces, endian);

            if (version >= 0 && version <= 1)
            {
                this.DeformTable.Deserialize(input, endian);
            }

            if (version == 1)
            {
                this.Unknown24C = input.ReadValueU32(endian);
            }
        }
    }
}
