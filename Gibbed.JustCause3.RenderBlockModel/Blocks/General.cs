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
    public class General : IRenderBlock
    {
        public byte Version;

        public bool HasBigVertices
        {
            get { return this.Unknown11 == 0; }
        }

        public float Unknown01;
        public float Unknown02;
        public float Unknown03;
        public float Unknown04;
        public float Unknown05;
        public float Unknown06;
        public float Unknown07;
        public float Unknown08;
        public float Unknown09;
        public float Unknown10;

        public uint Unknown11;

        public float Unknown12;
        public float Unknown13;
        public float Unknown14;
        public float Unknown15;
        public float Unknown16;
        public float Unknown17;
        public int Unknown18;
        public int Unknown19;

        public Material Material;
        public List<GeneralData0Small> VertexData0Small = new List<GeneralData0Small>();
        public List<GeneralData0Big> VertexData0Big = new List<GeneralData0Big>();
        public List<short> Faces = new List<short>();

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        private static float GetFloatFromS16N(short c)
        {
            if (c == -1)
            {
                return -1.0f;
            }

            return c * (1.0f / 32767);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Version = input.ReadValueU8();
            if (this.Version < 2 || this.Version > 3)
            {
                throw new FormatException("unhandled version for General");
            }

            this.Unknown01 = input.ReadValueF32(endian);
            this.Unknown02 = input.ReadValueF32(endian);
            this.Unknown03 = input.ReadValueF32(endian);
            this.Unknown04 = input.ReadValueF32(endian);
            this.Unknown05 = input.ReadValueF32(endian);
            this.Unknown06 = input.ReadValueF32(endian);
            this.Unknown07 = input.ReadValueF32(endian);
            this.Unknown08 = input.ReadValueF32(endian);
            this.Unknown09 = input.ReadValueF32(endian);
            this.Unknown10 = input.ReadValueF32(endian);

            this.Unknown11 = input.ReadValueU32(endian);

            this.Unknown12 = input.ReadValueF32(endian);
            this.Unknown13 = input.ReadValueF32(endian);
            this.Unknown14 = input.ReadValueF32(endian);
            this.Unknown15 = input.ReadValueF32(endian);
            this.Unknown16 = input.ReadValueF32(endian);
            this.Unknown17 = input.ReadValueF32(endian);

            if (this.Version == 3)
            {
                this.Unknown18 = input.ReadValueS32(endian);
                this.Unknown19 = input.ReadValueS32(endian);
            }

            this.Material.Deserialize(input, endian);

            if (this.HasBigVertices == false)
            {
                input.ReadArray(this.VertexData0Small, endian);

                /*
                this.HackToFixDumbVertices.Clear();
                {
                    uint count = input.ReadValueU32();
                    for (uint i = 0; i < count; i++)
                    {
                        var data = new GeneralData0Small();
                        data.Deserialize(input);
                        this.SmallVertices.Add(data);

                        var hack = new HackToFixDumbVertex
                        {
                            PositionX = GetFloatFromS16N(data.PositionX) * this.Unknown10,
                            PositionY = GetFloatFromS16N(data.PositionY) * this.Unknown10,
                            PositionZ = GetFloatFromS16N(data.PositionZ) * this.Unknown10,
                            TexCoordA = GetFloatFromS16N(data.TexCoord1A),
                            TexCoordB = GetFloatFromS16N(data.TexCoord1B)
                        };
                        this.HackToFixDumbVertices.Add(hack);
                    }
                }
                */
            }
            else
            {
                input.ReadArray(this.VertexData0Big, endian);
            }

            input.ReadFaces(this.Faces, endian);
        }
    }
}
