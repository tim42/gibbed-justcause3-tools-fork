using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using MemberDefinition = Gibbed.JustCause3.FileFormats.AdfFile.MemberDefinition;
using TypeDefinition = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinition;
using TypeDefinitionType = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinitionType;

namespace Gibbed.JustCause3.ConvertStringLookup
{
    internal static class Exporter
    {
        public static void Export(AdfFile adf, XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("string-lookup");
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
            // Unlike the ADF exporter, we will inline most of the arrays and completly split the big string array
            // We will still use the same style (attribute naming, ...) to have a simple Importer
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

            // We only do the first member, cause everything else is tied to it by references.
            {
                writer.WriteStartElement("member");
                var member = ii.Members[0];
                writer.WriteAttributeString("name", member.Name);
                writer.WriteAttributeString("gbltype", member.Type.ToString());
                writer.WriteAttributeString("id", member.Id.ToString());
                writer.WriteAttributeString("type-hash", member.TypeDef.NameHash.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("type", member.TypeDef.Name);

                // and we only do the first element of that structure (named "Languages"). The Text array will be recreated.
                ExportInstanceMember(ii, member.Members[0], writer);

                { // the second element is just here to keep track of its informations (again, simpler importer)
                    writer.WriteStartElement("member");
                    var subMember = member.Members[1];
                    writer.WriteAttributeString("name", subMember.Name);
                    writer.WriteAttributeString("gbltype", subMember.Type.ToString());
                    writer.WriteAttributeString("id", subMember.Id.ToString());
                    writer.WriteAttributeString("is-reference", "yay");
                    writer.WriteAttributeString("type-hash", subMember.TypeDef.NameHash.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("type", subMember.TypeDef.Name);
                    writer.WriteEndElement(); // subMember
                }
                writer.WriteEndElement(); // member
            }
            writer.WriteEndElement(); // instance
        }

        private static void ExportInstanceMember(AdfFile.InstanceInfo ii, AdfFile.InstanceMemberInfo imi, XmlWriter writer)
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

            if (imi.Name == "TextOffset" || imi.Name == "NameOffset") // hardcoded :( thingy to handle string lookup
            {
                writer.WriteAttributeString("type", "int32");
                ExportTextOffset(ii, imi, writer);
                writer.WriteEndElement();
                return;
            }

            // normal (except for the inlining of arrays)
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
                        writer.WriteValue(imi.Data.ReadValueS32().ToString("X8", CultureInfo.InvariantCulture));
                        break;
                    case AdfTypeHashes.Primitive.UInt32:
                        writer.WriteAttributeString("type", "uint32");
                        writer.WriteValue(imi.Data.ReadValueU32().ToString("X8", CultureInfo.InvariantCulture));
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
                    case TypeDefinitionType.Array: // we have a ref, get the ID and inline it:
                        if (imi.Id != 2 && imi.isReferenceToId == true)
                        {
                            int index = 0;
                            foreach (var m in ii.Members)
                            {
                                if (m.Id == imi.Id)
                                    break;
                                ++index;
                            }
                            ExportInstanceMember(ii, ii.Members[index], writer);
                        }
                        else if (imi.Id == 2 && imi.isReferenceToId == true)
                        {

                        }
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

            foreach (var member in imi.Members)
            {
                ExportInstanceMember(ii, member, writer);
            }

            writer.WriteEndElement();
        }

        private static void ExportTextOffset(AdfFile.InstanceInfo ii, AdfFile.InstanceMemberInfo imi, XmlWriter writer)
        {
            int offset = imi.Data.ReadValueS32();
            int stringArrayIndex = 0;
            foreach (var m in ii.Members)
            {
                if (m.Id == 2)
                    break;
                ++stringArrayIndex;
            }
            var stringArray = ii.Members[stringArrayIndex];

            List<byte> accum = new List<byte>();

            for (int i = offset; true; ++i)
            {
                byte current = (byte)stringArray.Members[i].Data.ReadByte();
                stringArray.Members[i].Data.Position = 0;

                if (current != 0)
                    accum.Add(current);
                else // write the string
                {
                    string t = Encoding.UTF8.GetString(accum.ToArray());
                    writer.WriteValue(t);
                    break;
                }
            }
        }
    }
}
