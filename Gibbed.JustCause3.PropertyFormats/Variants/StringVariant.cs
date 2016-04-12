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
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class StringVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private string _Value = "";

        public string Value
        {
            get { return this._Value; }
            set { this._Value = value; }
        }

        public string Tag
        {
            get { return "string"; }
        }

        public void Parse(string text)
        {
            this._Value = text;
        }

        public string Compose(ProjectData.HashList<uint> names)
        {
            return this._Value;
        }

        public uint[] GetHashList()
        {
            return null; // nothing to declare
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.String; }
        }

        bool PropertyContainerFile.IRawVariant.IsPrimitive
        {
            get { return false; }
        }

        uint PropertyContainerFile.IRawVariant.Alignment
        {
            get { return 0; }
        }

        void PropertyContainerFile.IRawVariant.Serialize(Stream output, Endian endian)
        {
            output.WriteStringZ(this._Value, Encoding.ASCII);
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            this._Value = input.ReadStringZ(Encoding.ASCII);
        }
        #endregion
    }
}
