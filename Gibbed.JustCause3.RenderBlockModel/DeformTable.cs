using System;
using Gibbed.IO;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.RenderBlockModel
{
    public class DeformTable : IFormat
    {
        public readonly uint[] Data = new uint[256];

        public void Serialize(Stream output, Endian endian)
        {
            for (int i = 0; i < this.Data.Length; i++)
            {
                output.WriteValueU32(this.Data[i], endian);
            }
        }

        public void Deserialize(Stream input, Endian endian)
        {
            for (int i = 0; i < this.Data.Length; i++)
            {
                this.Data[i] = input.ReadValueU32(endian);
            }
        }
    }
}
