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
using System.Globalization;
using System.IO;
using System.Linq;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.PropertyFormats.Variants
{
    public class EventsVariant : IVariant, PropertyContainerFile.IRawVariant
    {
        private readonly List<KeyValuePair<uint, uint>> _Values;

        public EventsVariant()
        {
            this._Values = new List<KeyValuePair<uint, uint>>();
        }

        public List<KeyValuePair<uint, uint>> Values
        {
            get { return this._Values; }
        }

        public string Tag
        {
            get { return "vec_events"; }
        }

        public void Parse(string text)
        {
            this._Values.Clear();
            if (string.IsNullOrEmpty(text) == false)
            {
                var kvPairs = text.Split(',');
                foreach (string pair in kvPairs)
                {
                    var parts = pair.Split('=');
                    uint left;
                    uint right;

                    // the key
                    if (parts[0][0] == '$')
                        left = parts[0].Substring(1).HashJenkins();
                    else
                        left = uint.Parse(parts[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

                    // the value
                    if (parts.Length == 1)
                        right = left;
                    else if (parts[1][0] == '$')
                        right = parts[1].Substring(1).HashJenkins();
                    else
                        right = uint.Parse(parts[1], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    this._Values.Add(new KeyValuePair<uint, uint>(left, right));
                }
            }
        }

        public string Compose(ProjectData.HashList<uint> names)
        {
            return string.Join(", ", this._Values.Select(v => Compose(v, names)));
        }

        private static string Compose(KeyValuePair<uint, uint> kv, ProjectData.HashList<uint> names)
        {
            string key = kv.Key.ToString("X8", CultureInfo.InvariantCulture);
            string value = kv.Value.ToString("X8", CultureInfo.InvariantCulture);

            if (names.Contains(kv.Key) && names[kv.Key].IndexOf('=') == -1 && names[kv.Key].IndexOf(',') == -1)
                key = names[kv.Key];
            if (names.Contains(kv.Value) && names[kv.Value].IndexOf('=') == -1 && names[kv.Value].IndexOf(',') == -1)
                value = names[kv.Value];

            if (key == value)
                return key;
            return String.Format(
                "{0}={1}", key, value);
        }

        public uint[] GetHashList()
        {
            if (this._Values.Count == 0)
                return null;

            var hashes = new uint[this.Values.Count * 2];

            int i = 0;
            foreach (var val in this._Values)
            {
                hashes[i * 2 + 0] = val.Key;
                hashes[i * 2 + 1] = val.Value;
                ++i;
            }

            return hashes;
        }

        #region PropertyContainerFile
        PropertyContainerFile.VariantType PropertyContainerFile.IRawVariant.Type
        {
            get { return PropertyContainerFile.VariantType.Events; }
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
                output.WriteValueU32(value.Key, endian);
                output.WriteValueU32(value.Value, endian);
            }
        }

        void PropertyContainerFile.IRawVariant.Deserialize(Stream input, Endian endian)
        {
            int count = input.ReadValueS32(endian);
            var values = new KeyValuePair<uint, uint>[count];
            for (int i = 0; i < count; i++)
            {
                var left = input.ReadValueU32(endian);
                var right = input.ReadValueU32(endian);
                values[i] = new KeyValuePair<uint, uint>(left, right);
            }
            this._Values.Clear();
            this._Values.AddRange(values);
        }
        #endregion
    }
}
