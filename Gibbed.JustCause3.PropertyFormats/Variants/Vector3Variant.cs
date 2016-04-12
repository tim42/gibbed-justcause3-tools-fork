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
using System.Globalization;
using System.IO;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class Vector3Variant : IVariant, PropertyContainerFile.IRawVariant
    {
        private FileFormats.Vector3 _Value;

        public FileFormats.Vector3 Value
        {
            get { return this._Value; }
            set { this._Value = value; }
        }

        public string Tag
        {
            get { return "vec"; }
        }

        public void Parse(string text)
        {
            var parts = text.Split(',');

            if (parts.Length != 3)
            {
                throw new FormatException("vec requires 3 float values delimited by commas");
            }

            var x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            var y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            var z = float.Parse(parts[2], CultureInfo.InvariantCulture);
            this._Value = new FileFormats.Vector3(x, y, z);
        }

        public string Compose(ProjectData.HashList<uint> names)
        {
            return string.Format(
                "{0},{1},{2}",
                this._Value.X.ToString(CultureInfo.InvariantCulture),
                this._Value.Y.ToString(CultureInfo.InvariantCulture),
                this._Value.Z.ToString(CultureInfo.InvariantCulture));
        }

        public uint[] GetHashList()
        {
            return null; // nothing to declare
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Vector3; }
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
            FileFormats.Vector3.Write(output, this._Value, endian);
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            this._Value = FileFormats.Vector3.Read(input, endian);
        }
        #endregion
    }
}
