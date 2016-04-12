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
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.FileFormats
{
    public class ArchiveTableFile
    {
        public const uint Signature = 0x00424154; // 'TAB\0'

        private Endian _Endian;
        private uint _Alignment;
        private readonly List<EntryInfo> _Entries;

        public ArchiveTableFile()
        {
            this._Endian = Endian.Little;
            this._Alignment = 0x800;
            this._Entries = new List<EntryInfo>();
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public uint Alignment
        {
            get { return this._Alignment; }
            set { this._Alignment = value; }
        }

        public List<EntryInfo> Entries
        {
            get { return this._Entries; }
        }

        public void Serialize(Stream output)
        {
            output.WriteValueU32(Signature, _Endian);
            output.WriteValueU16(2, _Endian);
            output.WriteValueU16(1, _Endian);
            output.WriteValueU32(_Alignment, _Endian);

            foreach (var entry in _Entries)
            {
                output.WriteValueU32(entry.NameHash, _Endian);
                output.WriteValueU32(entry.Offset, _Endian);
                output.WriteValueU32(entry.Size, _Endian);
            }
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var unk04 = input.ReadValueU16(endian);
            var unk06 = input.ReadValueU16(endian);
            if (unk04 != 2 || unk06 != 1)
            {
                throw new FormatException();
            }

            var alignment = input.ReadValueU32(endian);
            if (alignment != 0x800)
            {
                throw new FormatException();
            }

            var entries = new List<EntryInfo>();
            while (input.Position + 12 <= input.Length)
            {
                var nameHash = input.ReadValueU32(endian);
                var offset = input.ReadValueU32(endian);
                var size = input.ReadValueU32(endian);
                entries.Add(new EntryInfo(nameHash, offset, size));
            }

            this._Endian = endian;
            this._Alignment = alignment;

            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        public struct EntryInfo
        {
            public readonly uint NameHash;
            public readonly uint Offset;
            public readonly uint Size;

            public MemoryStream ReadToMemoryStream(Stream arcStream)
            {
                MemoryStream ret = new MemoryStream();

                arcStream.Position = Offset;
                ret.WriteFromStream(arcStream, this.Size);
                ret.Position = 0;
                return ret;
            }

            public EntryInfo(uint nameHash, uint offset, uint size)
            {
                this.NameHash = nameHash;
                this.Offset = offset;
                this.Size = size;
            }
        }
    }
}
