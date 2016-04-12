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
    public class SkinnedGeneral : IRenderBlock
    {
        public byte Version;
        public uint Flags;

        public byte Mode
        {
            get { return (byte)(this.Flags & 0xFF); }
        }

        public bool HasBigVertices
        {
            get { return this.Version >= 3 && (this.Flags & 0x80000) == 0x80000; }
        }

        public float Unknown01;
        public float Unknown02;
        public float Unknown03;
        public float Unknown04;
        public float Unknown05;
        public float Unknown06;
        public Material Material;
        public readonly List<SkinnedGeneralData0Small> VertexData0Small = new List<SkinnedGeneralData0Small>();
        public readonly List<SkinnedGeneralData0Big> VertexData0Big = new List<SkinnedGeneralData0Big>();
        public readonly List<SkinnedGeneralData1> VertexData1 = new List<SkinnedGeneralData1>();
        public readonly List<SkinnedGeneralSkinBatch> SkinBatches = new List<SkinnedGeneralSkinBatch>();
        public readonly List<short> Faces = new List<short>();

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Version = input.ReadValueU8();
            if (this.Version != 0 && this.Version != 3)
            {
                throw new FormatException("unhandled version for SkinnedGeneral");
            }

            this.Flags = input.ReadValueU32(endian);
            this.Unknown01 = input.ReadValueF32(endian);
            this.Unknown02 = input.ReadValueF32(endian);
            this.Unknown03 = input.ReadValueF32(endian);
            this.Unknown04 = input.ReadValueF32(endian);
            this.Unknown05 = input.ReadValueF32(endian);
            this.Unknown06 = input.ReadValueF32(endian);
            this.Material.Deserialize(input, endian);

            if (this.HasBigVertices == false)
            {
                input.ReadArray(this.VertexData0Small, endian);
            }
            else
            {
                input.ReadArray(this.VertexData0Big, endian);
            }

            input.ReadArray(this.VertexData1, endian);
            input.ReadArray(this.SkinBatches, endian);
            input.ReadFaces(this.Faces, endian);
        }
    }
}
