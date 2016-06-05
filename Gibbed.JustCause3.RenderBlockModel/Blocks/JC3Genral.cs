using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.RenderBlockModel.Blocks
{
    class JC3Genral : IRenderBlock
    {
        public byte Version;

        public bool HasBigVertices
        {
            get { return this.Unknown11 == 0; }
        }
        public uint Unknown11;

        public Material Material;
        public List<ShortVec4Buffer> Positions = new List<ShortVec4Buffer>();
        public List<Vec4Buffer> UVs = new List<Vec4Buffer>();
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
            if (this.Version != 5)
            {
                throw new FormatException("unhandled version for JC3General");
            }

            input.Position += 0x24;
            this.Unknown11 = input.ReadValueU32(endian);
            Console.WriteLine(" -- 11: {0:X8}", Unknown11);
            input.Position += 0x17D - 0x25;
            Console.WriteLine(" -- X: {0:X8}", input.ReadValueU32());
            input.Position -= 4;

            this.Material.Deserialize(input, endian);

            input.ReadValueU32(endian);
            input.ReadValueU32(endian);
            input.ReadValueU32(endian);
            input.ReadValueU32(endian);

            Console.WriteLine(" vertex offset: {0:X8}", input.Position);

            input.ReadArray(this.Positions, endian);
            Console.WriteLine(" UVs offset: {0:X8}", input.Position);
            input.ReadArray(this.UVs, endian);

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
            //}
            //else
            //{
            //    input.ReadArray(this.VertexData0Big, endian);
            //}

            //input.ReadValueU32(endian); // ????

            Console.WriteLine(" face offset: {0:X8}", input.Position);
            input.ReadFaces(this.Faces, endian);
        }
    }
}
