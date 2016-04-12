/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.JustCause3.FileFormats
{
    public class TextureFile
    {
        public const uint Signature = 0x58545641; // 'AVTX'
        public const int ElementCount = 8;

        #region Fields
        private Endian _Endian;
        private byte _Unknown06;
        private byte _Dimension;
        private uint _Format;
        private ushort _Width;
        private ushort _Height;
        private ushort _Depth;
        private ushort _Flags;
        private byte _MipCount;
        private byte _HeaderMipCount;
        private uint _Unknown1C;
        private readonly Element[] _Elements;
        #endregion

        public TextureFile()
        {
            this._Elements = new Element[ElementCount];
            this._Endian = Endian.Little;
        }

        #region Properties
        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public byte Unknown06
        {
            get { return this._Unknown06; }
            set { this._Unknown06 = value; }
        }

        public byte Dimension
        {
            get { return this._Dimension; }
            set { this._Dimension = value; }
        }

        public uint Format
        {
            get { return this._Format; }
            set { this._Format = value; }
        }

        public ushort Width
        {
            get { return this._Width; }
            set { this._Width = value; }
        }

        public ushort Height
        {
            get { return this._Height; }
            set { this._Height = value; }
        }

        public ushort Depth
        {
            get { return this._Depth; }
            set { this._Depth = value; }
        }

        public ushort Flags
        {
            get { return this._Flags; }
            set { this._Flags = value; }
        }

        public byte MipCount
        {
            get { return this._MipCount; }
            set { this._MipCount = value; }
        }

        public byte HeaderMipCount
        {
            get { return this._HeaderMipCount; }
            set { this._HeaderMipCount = value; }
        }

        public uint Unknown1C
        {
            get { return this._Unknown1C; }
            set { this._Unknown1C = value; }
        }

        public Element[] Elements
        {
            get { return this._Elements; }
        }
        #endregion

        public void Serialize(Stream output)
        {
            output.WriteValueU32(Signature, this._Endian); // magiv
            output.WriteValueU16(1, this._Endian); // version
            output.WriteValueU8(this.Unknown06);
            output.WriteValueU8(this.Dimension);
            output.WriteValueU32(this.Format, this._Endian);
            output.WriteValueU16(this.Width, this._Endian);
            output.WriteValueU16(this.Height, this._Endian);
            output.WriteValueU16(this.Depth, this._Endian);
            output.WriteValueU16(this.Flags, this._Endian);
            output.WriteValueU8(this.MipCount);
            output.WriteValueU8(this.HeaderMipCount);

            output.WriteValueU8(0); // [unknown16] or 1 or 2
            output.WriteValueU8(0); // [unknown17]
            output.WriteValueU8(0); // [unknown18] or 1 or 2 or 3 or 4
            output.WriteValueU8(0); // [unknown19]
            output.WriteValueU8(0); // [unknown1A]
            output.WriteValueU8(0); // [unknown1B]
            output.WriteValueU32(this.Unknown1C, this._Endian);

            // serialze elements
            for (int i = 0; i < this._Elements.Length; i++)
                this._Elements[i].Write(output, this._Endian);
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU16(endian);
            if (version != 1)
            {
                throw new FormatException();
            }

            var unknown06 = input.ReadValueU8();
            var dimension = input.ReadValueU8();
            var format = input.ReadValueU32(endian);
            var width = input.ReadValueU16(endian);
            var height = input.ReadValueU16(endian);
            var depth = input.ReadValueU16(endian);
            var flags = input.ReadValueU16(endian);
            var mipCount = input.ReadValueU8();
            var headerMipCount = input.ReadValueU8();

            var unknown16 = input.ReadValueU8();
            var unknown17 = input.ReadValueU8();
            var unknown18 = input.ReadValueU8();
            var unknown19 = input.ReadValueU8();
            var unknown1A = input.ReadValueU8();
            var unknown1B = input.ReadValueU8();
            var unknown1C = input.ReadValueU32(endian);

            var elements = new Element[ElementCount];
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i] = Element.Read(input, endian);
            }

            if (flags != 0 && (flags & ~(1 | 8 | 0x40)) != 0)
            {
                throw new FormatException();
            }

            if (unknown17 != 0 ||
                unknown19 != 0 ||
                unknown1A != 0 ||
                unknown1B != 0)
            {
                throw new FormatException();
            }

            if (unknown16 != 0 && unknown16 != 1 && unknown16 != 2)
            {
                throw new FormatException();
            }

            if (unknown18 != 0 && unknown18 != 2 && unknown18 != 1 && unknown18 != 3 && unknown18 != 4)
            {
                throw new FormatException();
            }

            this._Endian = endian;
            this._Unknown06 = unknown06;
            this._Dimension = dimension;
            this._Format = format;
            this._Width = width;
            this._Height = height;
            this._Depth = depth;
            this._Flags = flags;
            this._MipCount = mipCount;
            this._HeaderMipCount = headerMipCount;
            this._Unknown1C = unknown1C;
            Array.Copy(elements, this._Elements, elements.Length);
        }

        public struct Element
        {
            public uint Offset;
            public uint Size;
            public ushort Unknown8;
            public byte UnknownA;
            public bool IsExternal;

            internal static Element Read(Stream input, Endian endian)
            {
                Element instance;
                instance.Offset = input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                instance.Unknown8 = input.ReadValueU16(endian);
                instance.UnknownA = input.ReadValueU8();
                instance.IsExternal = input.ReadValueB8();
                return instance;
            }

            internal static void Write(Stream output, Element instance, Endian endian)
            {
                output.WriteValueU32(instance.Offset, endian);
                output.WriteValueU32(instance.Size, endian);
                output.WriteValueU16(instance.Unknown8, endian);
                output.WriteValueU8(instance.UnknownA);
                output.WriteValueB8(instance.IsExternal);
            }

            internal void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }
    }
}
