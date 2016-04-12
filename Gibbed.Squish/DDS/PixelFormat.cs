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
    public class PixelFormat
    {
        public uint Size;
        public PixelFormatFlags Flags;
        public uint FourCC;
        public uint RGBBitCount;
        public uint RedBitMask;
        public uint GreenBitMask;
        public uint BlueBitMask;
        public uint AlphaBitMask;

        public uint GetSize()
        {
            return 8 * 4;
        }

        public void Initialise(FileFormat fileFormat)
        {
            this.Size = this.GetSize();
            
            switch (fileFormat)
            {
                case FileFormat.DXT1:
                {
                    this.Flags = PixelFormatFlags.FourCC;
                    this.RGBBitCount = 0;
                    this.RedBitMask = 0;
                    this.GreenBitMask = 0;
                    this.BlueBitMask = 0;
                    this.AlphaBitMask = 0;
                    this.FourCC = 0x31545844; // "DXT1"
                    break;
                }

                case FileFormat.DXT3:
                {
                    this.Flags = PixelFormatFlags.FourCC;
                    this.RGBBitCount = 0;
                    this.RedBitMask = 0;
                    this.GreenBitMask = 0;
                    this.BlueBitMask = 0;
                    this.AlphaBitMask = 0;
                    this.FourCC = 0x33545844; // "DXT3"
                    break;
                }

                case FileFormat.DXT5:
                {
                    this.Flags = PixelFormatFlags.FourCC;
                    this.RGBBitCount = 0;
                    this.RedBitMask = 0;
                    this.GreenBitMask = 0;
                    this.BlueBitMask = 0;
                    this.AlphaBitMask = 0;
                    this.FourCC = 0x35545844; // "DXT5"
                    break;
                }

                case FileFormat.A8R8G8B8:
                {
                    Flags = PixelFormatFlags.RGBA;
                    RGBBitCount = 32;
                    FourCC = 0;
                    RedBitMask = 0x00FF0000;
                    GreenBitMask = 0x0000FF00;
                    BlueBitMask = 0x000000FF;
                    AlphaBitMask = 0xFF000000;
                    break;
                }

                case FileFormat.X8R8G8B8:
                {
                    Flags = PixelFormatFlags.RGB;
                    RGBBitCount = 32;
                    FourCC = 0;
                    RedBitMask = 0x00FF0000;
                    GreenBitMask = 0x0000FF00;
                    BlueBitMask = 0x000000FF;
                    AlphaBitMask = 0x00000000;
                    break;
                }

                case FileFormat.A8B8G8R8:
                {
                    Flags = PixelFormatFlags.RGBA;
                    RGBBitCount = 32;
                    FourCC = 0;
                    RedBitMask = 0x000000FF;
                    GreenBitMask = 0x0000FF00;
                    BlueBitMask = 0x00FF0000;
                    AlphaBitMask = 0xFF000000;
                    break;
                }

                case FileFormat.X8B8G8R8:
                {
                    Flags = PixelFormatFlags.RGB;
                    RGBBitCount = 32;
                    FourCC = 0;
                    RedBitMask = 0x000000FF;
                    GreenBitMask = 0x0000FF00;
                    BlueBitMask = 0x00FF0000;
                    AlphaBitMask = 0x00000000;
                    break;
                }

                case FileFormat.A1R5G5B5:
                {
                    Flags = PixelFormatFlags.RGBA;
                    RGBBitCount = 16;
                    FourCC = 0;
                    RedBitMask = 0x00007C00;
                    GreenBitMask = 0x000003E0;
                    BlueBitMask = 0x0000001F;
                    AlphaBitMask = 0x00008000;
                    break;
                }

                case FileFormat.A4R4G4B4:
                {
                    Flags = PixelFormatFlags.RGBA;
                    RGBBitCount = 16;
                    FourCC = 0;
                    RedBitMask = 0x00000F00;
                    GreenBitMask = 0x000000F0;
                    BlueBitMask = 0x0000000F;
                    AlphaBitMask = 0x0000F000;
                    break;
                }

                case FileFormat.R8G8B8:
                {
                    Flags = PixelFormatFlags.RGB;
                    FourCC = 0;
                    RGBBitCount = 24;
                    RedBitMask = 0x00FF0000;
                    GreenBitMask = 0x0000FF00;
                    BlueBitMask = 0x000000FF;
                    AlphaBitMask = 0x00000000;
                    break;
                }

                case FileFormat.R5G6B5:
                {
                    Flags = PixelFormatFlags.RGB;
                    FourCC = 0;
                    RGBBitCount = 16;
                    RedBitMask = 0x0000F800;
                    GreenBitMask = 0x000007E0;
                    BlueBitMask = 0x0000001F;
                    AlphaBitMask = 0x00000000;
                    break;
                }

                default:
                {
                    throw new NotSupportedException();
                }
            }
        }

        [Obsolete]
        public void Serialize(Stream output, bool littleEndian)
        {
            this.Serialize(output, littleEndian == true ? Endian.Little : Endian.Big);
        }

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteValueU32(this.Size, endian);
            output.WriteValueEnum<PixelFormatFlags>(this.Flags, endian);
            output.WriteValueU32(this.FourCC, endian);
            output.WriteValueU32(this.RGBBitCount, endian);
            output.WriteValueU32(this.RedBitMask, endian);
            output.WriteValueU32(this.GreenBitMask, endian);
            output.WriteValueU32(this.BlueBitMask, endian);
            output.WriteValueU32(this.AlphaBitMask, endian);
        }

        [Obsolete]
        public void Deserialize(Stream input, bool littleEndian)
        {
            this.Deserialize(input, littleEndian == true ? Endian.Little : Endian.Big);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.Size = input.ReadValueU32(endian);
            this.Flags = input.ReadValueEnum<PixelFormatFlags>(endian);
            this.FourCC = input.ReadValueU32(endian);
            this.RGBBitCount = input.ReadValueU32(endian);
            this.RedBitMask = input.ReadValueU32(endian);
            this.GreenBitMask = input.ReadValueU32(endian);
            this.BlueBitMask = input.ReadValueU32(endian);
            this.AlphaBitMask = input.ReadValueU32(endian);
        }
    }
}
