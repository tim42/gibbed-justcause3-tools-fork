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
    public class CarPaintSimple : IRenderBlock
    {
        public struct Vertex
        {
            public float PositionX;
            public float PositionY;
            public float PositionZ;
            public float PositionW;
            public float TexCoordA;
            public float TexCoordB;
            public float TexCoordC;
            public float TexCoordD;

            public void Deserialize(Stream input)
            {
                this.PositionX = input.ReadValueF32();
                this.PositionY = input.ReadValueF32();
                this.PositionZ = input.ReadValueF32();
                this.PositionW = input.ReadValueF32();
                this.TexCoordA = input.ReadValueF32();
                this.TexCoordB = input.ReadValueF32();
                this.TexCoordC = input.ReadValueF32();
                this.TexCoordD = input.ReadValueF32();
            }

            public override string ToString()
            {
                return string.Format("{0},{1},{2}",
                                     this.PositionX,
                                     this.PositionY,
                                     this.PositionZ);
            }
        }

        public byte Version;
        public float Unknown01;
        public float Unknown02;
        public float Unknown03;
        public float Unknown04;
        public float Unknown05;
        public float Unknown06;
        public float Unknown07;
        public float Unknown08;
        public float Unknown09;
        public uint Unknown10;
        public uint Unknown11;
        public uint Unknown12;
        public uint Unknown13;
        public uint Unknown14;
        public List<string> Textures = new List<string>();
        public uint Unknown16;
        public List<Vertex> Vertices = new List<Vertex>();
        public List<short> Faces = new List<short>();

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Version = input.ReadValueU8();
            if (this.Version != 1)
            {
                throw new FormatException("unhandled version for CarPaintSimple");
            }

            this.Unknown01 = input.ReadValueF32();
            this.Unknown02 = input.ReadValueF32();
            this.Unknown03 = input.ReadValueF32();
            this.Unknown04 = input.ReadValueF32();
            this.Unknown05 = input.ReadValueF32();
            this.Unknown06 = input.ReadValueF32();
            this.Unknown07 = input.ReadValueF32();
            this.Unknown08 = input.ReadValueF32();
            this.Unknown09 = input.ReadValueF32();
            this.Unknown10 = input.ReadValueU32();
            this.Unknown11 = input.ReadValueU32();
            this.Unknown12 = input.ReadValueU32();
            this.Unknown13 = input.ReadValueU32();
            this.Unknown14 = input.ReadValueU32();

            this.Textures.Clear();
            for (int i = 0; i < 8; i++)
            {
                this.Textures.Add(input.ReadStringU32(endian));
            }
            this.Unknown16 = input.ReadValueU32();

            this.Vertices.Clear();
            {
                uint count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var data = new Vertex();
                    data.Deserialize(input);
                    this.Vertices.Add(data);
                }
            }

            this.Faces.Clear();
            {
                uint count = input.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    this.Faces.Add(input.ReadValueS16());
                }
            }
        }
    }
}
