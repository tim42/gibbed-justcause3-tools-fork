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

namespace Gibbed.JustCause3.PropertyFormats
{
    public static class VariantFactory
    {
        public static IVariant GetVariant(string type)
        {
            // TODO(rick): move these to constants on the variant type itself

            switch (type)
            {
                case "int":
                {
                    return new Variants.IntegerVariant();
                }

                case "float":
                {
                    return new Variants.FloatVariant();
                }

                case "string":
                {
                    return new Variants.StringVariant();
                }

                case "vec2":
                {
                    return new Variants.Vector2Variant();
                }

                case "vec":
                {
                    return new Variants.Vector3Variant();
                }

                case "vec4":
                {
                    return new Variants.Vector4Variant();
                }

                case "mat":
                {
                    return new Variants.Matrix4x4Variant();
                }

                case "vec_int":
                {
                    return new Variants.IntegersVariant();
                }

                case "vec_float":
                {
                    return new Variants.FloatsVariant();
                }

                case "vec_byte":
                {
                    return new Variants.BytesVariant();
                }

                case "objectid":
                {
                    return new Variants.ObjectIdVariant();
                }

                case "vec_events":
                {
                    return new Variants.EventsVariant();
                }
            }

            throw new ArgumentException("unknown variant type", "type");
        }

        internal static PropertyContainerFile.IRawVariant GetVariant(PropertyContainerFile.VariantType type)
        {
            switch (type)
            {
                case PropertyContainerFile.VariantType.Integer:
                {
                    return new Variants.IntegerVariant();
                }

                case PropertyContainerFile.VariantType.Float:
                {
                    return new Variants.FloatVariant();
                }

                case PropertyContainerFile.VariantType.String:
                {
                    return new Variants.StringVariant();
                }

                case PropertyContainerFile.VariantType.Vector2:
                {
                    return new Variants.Vector2Variant();
                }

                case PropertyContainerFile.VariantType.Vector3:
                {
                    return new Variants.Vector3Variant();
                }

                case PropertyContainerFile.VariantType.Vector4:
                {
                    return new Variants.Vector4Variant();
                }

                case PropertyContainerFile.VariantType.Matrix4x4:
                {
                    return new Variants.Matrix4x4Variant();
                }

                case PropertyContainerFile.VariantType.Integers:
                {
                    return new Variants.IntegersVariant();
                }

                case PropertyContainerFile.VariantType.Floats:
                {
                    return new Variants.FloatsVariant();
                }

                case PropertyContainerFile.VariantType.Bytes:
                {
                    return new Variants.BytesVariant();
                }

                case PropertyContainerFile.VariantType.ObjectId:
                {
                    return new Variants.ObjectIdVariant();
                }

                case PropertyContainerFile.VariantType.Events:
                {
                    return new Variants.EventsVariant();
                }
            }

            throw new ArgumentException("unknown variant type", "type");
        }
    }
}
