

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using MemberDefinition = Gibbed.JustCause3.FileFormats.AdfFile.MemberDefinition;
using TypeDefinition = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinition;
using TypeDefinitionType = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinitionType;

namespace Gibbed.JustCause3.ConvertAdf
{
    internal static class Exporter
    {
        public static void Export(AdfFile adf, XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("adf");
            writer.WriteAttributeString("extension", adf.extension);
            writer.WriteAttributeString("endian", adf.Endian.ToString());
            writer.WriteAttributeString("comment", adf.Comment);

            if (adf.TypeDefinitions.Count > 0)
            {
                writer.WriteStartElement("type-definitions");
                writer.WriteAttributeString("WARNING", "DO NOT TOUCH THAT FUCKING THING");
                foreach (var typeDef in adf.TypeDefinitions)
                {
                    ExportTypeDefinition(typeDef, writer);
                }
                writer.WriteEndElement();
            }

            if (adf.InstanceInfos.Count > 0)
            {
                writer.WriteStartElement("instances");
                writer.WriteAttributeString("NOTE", "HERE YOU CAN TOUCH, BUT BE GENTLE");
                foreach (var instanceInfo in adf.InstanceInfos)
                {
                    ExportInstanceInfo(instanceInfo, writer);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        private static void ExportTypeDefinition(AdfFile.TypeDefinition typeDef, XmlWriter writer)
        {
            writer.WriteStartElement("type");
            writer.WriteAttributeString("type", typeDef.Type.ToString());
            writer.WriteAttributeString("size", typeDef.Size.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("alignment", typeDef.Alignment.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("name", typeDef.Name);
            writer.WriteAttributeString("name-hash", typeDef.NameHash.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("flags", typeDef.Flags.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("length", typeDef.ElementLength.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("eltypehash", typeDef.ElementTypeHash.ToString(CultureInfo.InvariantCulture));

            if (typeDef.Type == TypeDefinitionType.Structure)
            {
                foreach (var memDef in typeDef.Members)
                {
                    ExportMemberDefinition(memDef, writer);
                }
            }

            writer.WriteEndElement();
        }
        private static void ExportMemberDefinition(AdfFile.MemberDefinition memDef, XmlWriter writer)
        {
            writer.WriteStartElement("member");
            writer.WriteAttributeString("name", memDef.Name);
            writer.WriteAttributeString("type-hash", memDef.TypeHash.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("size", memDef.Size.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("offset", memDef.Offset.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("deftype", memDef.DefaultType.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("defval", memDef.DefaultValue.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        private static void ExportInstanceInfo(AdfFile.InstanceInfo ii, XmlWriter writer)
        {
            writer.WriteStartElement("instance");
            writer.WriteAttributeString("name", ii.Name);
            writer.WriteAttributeString("name-hash", ii.NameHash.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("type", ii.Type.Type.ToString());
            writer.WriteAttributeString("type-hash", ii.Type.NameHash.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("type-name", ii.Type.Name);
            if (ii.InlineArrayIndex != -1)
                writer.WriteAttributeString("inline-array-copy-index", ii.InlineArrayIndex.ToString(CultureInfo.InvariantCulture));
            if (ii.MinInlineArraySize > 0)
                writer.WriteAttributeString("inline-array-copy-minsz", ii.MinInlineArraySize.ToString(CultureInfo.InvariantCulture));

            foreach (var member in ii.Members)
            {
                ExportInstanceMember(member, writer);
            }

            writer.WriteEndElement();
        }

        private static void ExportInstanceMember(AdfFile.InstanceMemberInfo imi, XmlWriter writer)
        {
            writer.WriteStartElement("member");
            if (!String.IsNullOrEmpty(imi.Name))
                writer.WriteAttributeString("name", imi.Name);
            writer.WriteAttributeString("gbltype", imi.Type.ToString());
            if (imi.isReferenceToId)
                writer.WriteAttributeString("is-reference", "yay");
            if (imi.Id >= 0 || imi.isReferenceToId)
                writer.WriteAttributeString("id", imi.Id.ToString());
            if (imi.HasOwnCopyOfInlineArrays)
                writer.WriteAttributeString("own-copy-of-inline-arrays", "yay");
            if (imi.FileOffset > 0)
                writer.WriteAttributeString("offset-in-file", imi.FileOffset.ToString("X8", CultureInfo.InvariantCulture));
            if (imi.Type == TypeDefinitionType.Primitive)
            {
                switch (imi.TypeHash)
                {
                    case AdfTypeHashes.Primitive.Int8:
                        writer.WriteAttributeString("type", "int8");
                        writer.WriteValue(imi.Data.ReadValueS8().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.UInt8:
                        writer.WriteAttributeString("type", "uint8");
                        writer.WriteValue(imi.Data.ReadValueU8().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.Int16:
                        writer.WriteAttributeString("type", "int16");
                        writer.WriteValue(imi.Data.ReadValueS16().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.UInt16:
                        writer.WriteAttributeString("type", "uint16");
                        writer.WriteValue(imi.Data.ReadValueU16().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.Int32:
                        writer.WriteAttributeString("type", "int32");
                        writer.WriteValue(imi.Data.ReadValueS32().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.UInt32:
                        writer.WriteAttributeString("type", "uint32");
                        writer.WriteValue(imi.Data.ReadValueU32().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.Float:
                        writer.WriteAttributeString("type", "float");
                        // G18 'cause it may seems stupid but that's what it takes to have matching checksums
                        writer.WriteValue(imi.Data.ReadValueF32().ToString("G18", CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.Int64:
                        writer.WriteAttributeString("type", "int64");
                        writer.WriteValue(imi.Data.ReadValueS64().ToString("G", CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.UInt64:
                        writer.WriteAttributeString("type", "uint64");
                        writer.WriteValue(imi.Data.ReadValueU64().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.Double:
                        writer.WriteValue(imi.Data.ReadValueF64().ToString(CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.String:
                        writer.WriteAttributeString("type", "string");
                        if (imi.ReferenceToString == true)
                            writer.WriteAttributeString("reference-string-from", imi.StringTableId.ToString(CultureInfo.InvariantCulture));
                        writer.WriteValue(imi.StringData);
                        break;
                }
            }
            else
            {
                writer.WriteAttributeString("type-hash", imi.TypeDef.NameHash.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("type", imi.TypeDef.Name);
                switch (imi.Type)
                {
                    case TypeDefinitionType.Array:
                        break;
                    case TypeDefinitionType.InlineArray:
                        break;
                    case TypeDefinitionType.Structure:
                        break;
                    case TypeDefinitionType.StringHash:
                        writer.WriteValue(imi.StringData);
                        break;
                }
            }

            bool isString = false;
            if (imi.Type == TypeDefinitionType.Array && imi.TypeDef.ElementTypeHash == AdfTypeHashes.Primitive.Int8
                && imi.Members.Count > 1)
            {
                // check if that's a string. First thing: the name should be *text* or *string* (for stringlookups)
                // Second thing, it should end with 0
                sbyte last = (sbyte)imi.Members.Last().Data.ReadByte();
                imi.Members.Last().Data.Position = 0;
                var nameMatchingRegexp = new System.Text.RegularExpressions.Regex("(.*Text.*)|(.*String.*)");
                if (last == 0 && nameMatchingRegexp.IsMatch(imi.Name))
                {
                    isString = true; // yeah try the string
                    writer.WriteAttributeString("int8-array-is-string-array", "yay");
                    // write the string
                    List<byte> accum = new List<byte>();
                    foreach (var member in imi.Members)
                    {
                        byte current = (byte)member.Data.ReadByte();
                        if (current != 0)
                            accum.Add(current);
                        else // write the string
                        {
                            writer.WriteStartElement("inline-string");
                            string t = Encoding.UTF8.GetString(accum.ToArray());
                            accum.Clear();
                            writer.WriteValue(t);
                            writer.WriteEndElement();
                        }
                    }
                }
            }

            if (!isString)
            {
                foreach (var member in imi.Members)
                {
                    ExportInstanceMember(member, writer);
                }
            }

            writer.WriteEndElement();
        }
    }
}
