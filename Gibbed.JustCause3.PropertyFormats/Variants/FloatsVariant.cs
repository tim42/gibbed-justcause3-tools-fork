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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class FloatsVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private readonly List<float> _Values;

        public FloatsVariant()
        {
            this._Values = new List<float>();
        }

        public List<float> Values
        {
            get { return this._Values; }
        }

        public string Tag
        {
            get { return "vec_float"; }
        }

        public void Parse(string text)
        {
            this._Values.Clear();
            if (string.IsNullOrEmpty(text) == false)
            {
                var parts = text.Split(',');
                foreach (var part in parts)
                {
                    var value = float.Parse(part, CultureInfo.InvariantCulture);
                    this._Values.Add(value);
                }
            }
        }

        public string Compose(ProjectData.HashList<uint> names)
        {
            return string.Join(",", this._Values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        }

        public uint[] GetHashList()
        {
            return null; // nothing to declare
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Floats; }
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
            var values = this._Values;
            output.WriteValueS32(values.Count, endian);
            foreach (var value in values)
            {
                output.WriteValueF32(value, endian);
            }
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            int count = input.ReadValueS32(endian);
            var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = input.ReadValueF32(endian);
            }
            this._Values.Clear();
            this._Values.AddRange(values);
        }
        #endregion
    }
}
