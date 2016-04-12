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
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.FileFormats
{
    public class SmallArchiveFile
    {
        private Endian _Endian;
        private readonly List<Entry> _Entries;

        public SmallArchiveFile()
        {
            this._Entries = new List<Entry>();
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public List<Entry> Entries
        {
            get { return this._Entries; }
        }

        public static long EstimateHeaderSize(IEnumerable<string> paths)
        {
            long size = 4 + 4 + 4 + 4;
            foreach (var path in paths)
            {
                size += 4 + Encoding.ASCII.GetByteCount(path).Align(4) + 4 + 4;
            }
            return size.Align(16);
        }

        public void Serialize(Stream output)
        {
            var endian = this._Endian;

            using (var index = new MemoryStream())
            {
                foreach (var entry in this._Entries)
                {
                    entry.Write(index, endian);
                }

                index.SetLength(index.Length.Align(16));
                index.Position = 0;

                output.WriteValueU32(4, endian);
                output.WriteString("SARC", Encoding.ASCII);
                output.WriteValueU32(2, endian);
                output.WriteValueU32((uint)index.Length, endian);

                output.WriteFromStream(index, index.Length);
                output.SetLength(output.Length.Align(4));
            }
        }

        public void Deserialize(Stream input)
        {
            uint magic = input.ReadValueU32();
            if (magic != 4 && magic.Swap() != 4)
            {
                throw new FormatException("bad header size");
            }
            var endian = magic == 4 ? Endian.Little : Endian.Big;

            var tag = input.ReadString(4, Encoding.ASCII);
            if (tag != "SARC")
            {
                throw new FormatException("bad header magic");
            }

            var version = input.ReadValueU32(endian);
            if (version != 2)
            {
                throw new FormatException("bad header version");
            }

            var indexSize = input.ReadValueU32(endian);

            var entries = new List<Entry>();
            using (var index = input.ReadToMemoryStream(indexSize))
            {
                while (index.Length - index.Position > 15)
                {
                    entries.Add(Entry.Read(index, endian));
                }
            }

            this._Endian = endian;
            this._Entries.Clear();
            this._Entries.AddRange(entries);
        }

        public struct Entry
        {
            public readonly string Name;
            public readonly uint Offset;
            public readonly uint Size;

            public Entry(string name, uint offset, uint size)
            {
                this.Name = name;
                this.Offset = offset;
                this.Size = size;
            }

            public MemoryStream ReadToMemoryStream(Stream arcStream)
            {
                MemoryStream ret = new MemoryStream();

                if (this.Offset == 0) // reference to another file
                    return ret;

                arcStream.Position = Offset;
                ret.WriteFromStream(arcStream, this.Size);
                ret.Position = 0;
                return ret;
            }

            public static Entry Read(Stream input, Endian endian)
            {
                uint length = input.ReadValueU32(endian);
                if (length > 256)
                {
                    throw new FormatException("entry file name is too long");
                }

                var name = input.ReadString(length, true, Encoding.ASCII);
                var offset = input.ReadValueU32(endian);
                var size = input.ReadValueU32(endian);
                return new Entry(name, offset, size);
            }

            public static void Write(Stream output, Entry value, Endian endian)
            {
                var nameBytes = Encoding.ASCII.GetBytes(value.Name);
                Array.Resize(ref nameBytes, nameBytes.Length.Align(4));
                output.WriteValueS32(nameBytes.Length, endian);
                output.Write(nameBytes, 0, nameBytes.Length);
                output.WriteValueU32(value.Offset, endian);
                output.WriteValueU32(value.Size, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }

            public override string ToString()
            {
                return string.Format("{0} @ {1:X} ({2} bytes)", this.Name, this.Offset, this.Size);
            }
        }
    }
}
