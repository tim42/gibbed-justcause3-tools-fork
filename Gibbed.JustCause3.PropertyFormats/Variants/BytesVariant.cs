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

using System.Globalization;
using System.IO;
using System.Linq;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class BytesVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private byte[] _Value;

        public byte[] Value
        {
            get { return this._Value; }
        }

        public string Tag
        {
            get { return "vec_byte"; }
        }

        public void Parse(string text)
        {
            if (string.IsNullOrEmpty(text) == false)
            {
                var parts = text.Split(',');
                var bytes = new byte[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    bytes[i] = byte.Parse(parts[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                }
                this._Value = bytes;
            }
            else
            {
                this._Value = new byte[0];
            }
        }

        public string Compose(ProjectData.HashList<uint> names)
        {
            return string.Join(",", this._Value.Select(v => v.ToString("X2", CultureInfo.InvariantCulture)));
        }

        public uint[] GetHashList()
        {
            return null; // nothing to declare
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Bytes; }
        }

        bool PropertyContainerFile.IRawVariant.IsPrimitive
        {
            get { return false; }
        }

        uint PropertyContainerFile.IRawVariant.Alignment
        {
            get { return 4; }
        }

        void PropertyContainerFile.IRawVariant.Serialize(Stream output, Endian endian)
        {
            var bytes = this._Value;
            if (bytes == null)
            {
                output.WriteValueS32(0, endian);
                return;
            }

            output.WriteValueS32(bytes.Length, endian);
            output.WriteBytes(bytes);
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            int count = input.ReadValueS32(endian);
            var bytes = input.ReadBytes(count);
            this._Value = bytes;
        }
        #endregion
    }
}
