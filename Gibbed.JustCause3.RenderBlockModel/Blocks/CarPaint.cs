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
using Gibbed.IO;

namespace Gibbed.JustCause3.RenderBlockModel.Blocks
{
    public class CarPaint : IRenderBlock
    {
        public byte Version;

        public UnknownData0 Unknown1;
        public Material Material;
        public List<CarPaintData0> VertexData0 = new List<CarPaintData0>();
        public List<CarPaintData1> VertexData1 = new List<CarPaintData1>();
        public List<short> Faces = new List<short>();
        public DeformTable DeformTable = new DeformTable();

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var version = input.ReadValueU8();
            if (version < 1 || version > 4)
            {
                throw new FormatException("unhandled version for CarPaint");
            }

            this.Version = version;
            this.Unknown1.Deserialize(input, endian);

            if (version >= 4)
            {
                this.DeformTable.Deserialize(input, endian);
            }

            this.Material.Deserialize(input, endian);

            if (version == 1)
            {
                // TODO: upgrade old vertex data into new format
                throw new NotImplementedException();
            }
            else if (version == 2)
            {
                // TODO: upgrade old vertex data into new format
                throw new NotImplementedException();
            }
            else if (version >= 3)
            {
                input.ReadArray(this.VertexData0, endian);
                input.ReadArray(this.VertexData1, endian);
            }
            else
            {
                throw new FormatException();
            }

            input.ReadFaces(this.Faces, endian);
            
            if (version <= 3)
            {
                this.DeformTable.Deserialize(input, endian);
            }
        }
    }
}
