/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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
using System.IO;
using Gibbed.IO;

namespace Gibbed.Squish.DDS
{
    public class Header
    {
        public Header()
        {
            this.PixelFormat = new PixelFormat();
        }

        public uint GetSize()
        {
            return (18 * 4) + this.PixelFormat.GetSize() + (5 * 4);
        }

        public uint Size;
        public HeaderFlags Flags;
        public int Height;
        public int Width;
        public uint PitchOrLinearSize;
        public uint Depth;
        public uint MipMapCount;
        public byte[] Reserved1 = new byte[11 * 4];
        public PixelFormat PixelFormat;
        public uint SurfaceFlags;
        public uint CubemapFlags;
        public byte[] Reserved2 = new byte[3 * 4];

        [Obsolete]
        public void Serialize(Stream output, bool littleEndian)
        {
            this.Serialize(output, littleEndian == true ? Endian.Little : Endian.Big);
        }

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueU32(this.Size, endian);
            output.WriteValueEnum<HeaderFlags>(this.Flags, endian);
            output.WriteValueS32(this.Height, endian);
            output.WriteValueS32(this.Width, endian);
            output.WriteValueU32(this.PitchOrLinearSize, endian);
            output.WriteValueU32(this.Depth, endian);
            output.WriteValueU32(this.MipMapCount, endian);
            output.Write(this.Reserved1, 0, this.Reserved1.Length);
            this.PixelFormat.Serialize(output, endian);
            output.WriteValueU32(this.SurfaceFlags, endian);
            output.WriteValueU32(this.CubemapFlags, endian);
            output.Write(this.Reserved2, 0, this.Reserved2.Length);
        }

        [Obsolete]
        public void Deserialize(Stream input, bool littleEndian)
        {
            this.Deserialize(input, littleEndian == true ? Endian.Little : Endian.Big);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Size = input.ReadValueU32(endian);
            this.Flags = input.ReadValueEnum<HeaderFlags>(endian);
            this.Height = input.ReadValueS32(endian);
            this.Width = input.ReadValueS32(endian);
            this.PitchOrLinearSize = input.ReadValueU32(endian);
            this.Depth = input.ReadValueU32(endian);
            this.MipMapCount = input.ReadValueU32(endian);
            if (input.Read(this.Reserved1, 0, this.Reserved1.Length) != this.Reserved1.Length)
            {
                throw new EndOfStreamException();
            }
            this.PixelFormat.Deserialize(input, endian);
            this.SurfaceFlags = input.ReadValueU32(endian);
            this.CubemapFlags = input.ReadValueU32(endian);
            if (input.Read(this.Reserved2, 0, this.Reserved2.Length) != this.Reserved2.Length)
            {
                throw new EndOfStreamException();
            }
        }
    }
}
