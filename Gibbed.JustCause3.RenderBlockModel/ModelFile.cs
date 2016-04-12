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
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.RenderBlockModel
{
    public class ModelFile
    {
        public Version Version;
        public float MinX;
        public float MinY;
        public float MinZ;
        public float MaxX;
        public float MaxY;
        public float MaxZ;

        public readonly List<IRenderBlock> Blocks = new List<IRenderBlock>();

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magicLength = input.ReadValueU32(Endian.Little);
            if (magicLength != 5 &&
                magicLength.Swap() != 5)
            {
                throw new FormatException("invalid magic length");
            }
            var endian = magicLength == 5 ? Endian.Little : Endian.Big;

            var magic = input.ReadString(5, Encoding.ASCII);
            if (magic != "RBMDL")
            {
                throw new FormatException("invalid magic");
            }

            var versionMajor = input.ReadValueS32(endian);
            var versionMinor = input.ReadValueS32(endian);
            var versionRevision = input.ReadValueS32(endian);

            if (versionMajor != 1 || versionMinor != 13)
            {
                throw new FormatException("unsupported RBMDL version");
            }

            this.Version = new Version(versionMajor, versionMinor, 0, versionRevision);

            this.MinX = input.ReadValueF32(endian);
            this.MinY = input.ReadValueF32(endian);
            this.MinZ = input.ReadValueF32(endian);
            this.MaxX = input.ReadValueF32(endian);
            this.MaxY = input.ReadValueF32(endian);
            this.MaxZ = input.ReadValueF32(endian);

            var count = input.ReadValueS32(endian);
            this.Blocks.Clear();
            this.Blocks.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                uint typeHash = input.ReadValueU32(endian);

                var block = BlockTypeFactory.Create(typeHash);
                if (block == null)
                {
                    var typeName = BlockTypeFactory.GetName(typeHash);
                    if (string.IsNullOrEmpty(typeName) == false)
                    {
                        throw new NotSupportedException("unhandled block type " + typeName + " (0x" + typeHash.ToString("X8") + ")");
                    }

                    throw new NotSupportedException("unknown block type 0x" + typeHash.ToString("X8"));
                }

                block.Deserialize(input, endian);

                if (input.ReadValueU32(endian) != 0x89ABCDEF)
                {
                    throw new FormatException("invalid block footer (data corrupt? or misread?)");
                }

                this.Blocks.Add(block);
            }
        }
    }
}
