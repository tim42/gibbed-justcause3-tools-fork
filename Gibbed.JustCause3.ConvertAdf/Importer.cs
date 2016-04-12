
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using MemberDefinition = Gibbed.JustCause3.FileFormats.AdfFile.MemberDefinition;
using TypeDefinition = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinition;
using TypeDefinitionType = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinitionType;
using InstanceInfo = Gibbed.JustCause3.FileFormats.AdfFile.InstanceInfo;

namespace Gibbed.JustCause3.ConvertAdf
{
    internal static class Importer
    {
        public static AdfFile Import(XPathNavigator root)
        {
            AdfFile adf = new AdfFile()
            {
                Endian = Endian.Little,
            };

            adf.Comment = root.GetAttribute("comment", "");
            Endian adfEndian;
            Enum.TryParse(root.GetAttribute("endian", ""), out adfEndian);
            adf.Endian = adfEndian;

            var typeDefNode = root.SelectSingleNode("type-definitions");
            if (typeDefNode != null)
            {
                var typesList = typeDefNode.Select("type");

                foreach (XPathNavigator typeNode in typesList)
                {
                    ImportTypeDefinition(typeNode, adf);
                }

                // loop the loop
                adf.Runtime.AddTypeDefinitions(adf);
            }

            var instancesNode = root.SelectSingleNode("instances");
            if (instancesNode != null)
            {
                var instancesList = instancesNode.Select("instance");

                foreach (XPathNavigator instanceNode in instancesList)
                {
                    ImportInstanceInfo(instanceNode, adf);
                }
            }
            return adf;
        }

        private static void ImportTypeDefinition(XPathNavigator typeNode, AdfFile adf)
        {
            TypeDefinition typeDef = new TypeDefinition()
            {
                Size = uint.Parse(typeNode.GetAttribute("size", "")),
                Alignment = uint.Parse(typeNode.GetAttribute("alignment", ""), CultureInfo.InvariantCulture),
                Name = typeNode.GetAttribute("name", ""),
                NameHash = uint.Parse(typeNode.GetAttribute("name-hash", ""), CultureInfo.InvariantCulture),
                Flags = uint.Parse(typeNode.GetAttribute("flags", ""), CultureInfo.InvariantCulture),
                ElementLength = uint.Parse(typeNode.GetAttribute("length", ""), CultureInfo.InvariantCulture),
                ElementTypeHash = uint.Parse(typeNode.GetAttribute("eltypehash", ""), CultureInfo.InvariantCulture),
            };
            Enum.TryParse(typeNode.GetAttribute("type", ""), out typeDef.Type);

            if (typeDef.Type == TypeDefinitionType.Structure)
            {
                // it has members
                var membersList = typeNode.Select("member");
                // so we will parse them
                typeDef.Members = new AdfFile.MemberDefinition[membersList.Count];

                int i = 0;
                foreach (XPathNavigator memberNode in membersList)
                {
                    typeDef.Members[i].Name = memberNode.GetAttribute("name", "");
                    typeDef.Members[i].TypeHash = uint.Parse(memberNode.GetAttribute("type-hash", ""), CultureInfo.InvariantCulture);
                    typeDef.Members[i].Size = uint.Parse(memberNode.GetAttribute("size", ""), CultureInfo.InvariantCulture);
                    typeDef.Members[i].Offset = uint.Parse(memberNode.GetAttribute("offset", ""), CultureInfo.InvariantCulture);
                    typeDef.Members[i].DefaultType = uint.Parse(memberNode.GetAttribute("deftype", ""), CultureInfo.InvariantCulture);
                    typeDef.Members[i].DefaultValue = ulong.Parse(memberNode.GetAttribute("defval", ""), CultureInfo.InvariantCulture);
                    ++i;
                }
            }

            // Add it
            adf.TypeDefinitions.Add(typeDef);
        }

        private static void ImportInstanceInfo(XPathNavigator instanceNode, AdfFile adf)
        {
            InstanceInfo instance = new InstanceInfo()
            {
                Name = instanceNode.GetAttribute("name", ""),
                NameHash = uint.Parse(instanceNode.GetAttribute("name-hash", "")),
            };

            TypeDefinitionType type;
            Enum.TryParse(instanceNode.GetAttribute("type", ""), out type);
            uint typeHash = uint.Parse(instanceNode.GetAttribute("type-hash", ""), CultureInfo.InvariantCulture);
            string typeString = instanceNode.GetAttribute("type-name", "");

            instance.Type = adf.Runtime.GetTypeDefinition(typeHash);
            instance.TypeHash = instance.Type.NameHash;

            if (!int.TryParse(instanceNode.GetAttribute("inline-array-copy-index", ""), out instance.InlineArrayIndex))
                instance.InlineArrayIndex = -1;
            if (!uint.TryParse(instanceNode.GetAttribute("inline-array-copy-minsz", ""), out instance.MinInlineArraySize))
                instance.MinInlineArraySize = 0;

            if (type != instance.Type.Type)
                throw new InvalidOperationException("do not touch things like type and type-name in the xml file, yo");

            instance.Members = new List<AdfFile.InstanceMemberInfo>();
            // parse members
            var membersList = instanceNode.Select("member");

            // normal array or structure
            foreach (XPathNavigator memberNode in membersList)
            {
                instance.Members.Add(ImportInstanceMemberInfo(memberNode, adf));
            }

            // add it
            adf.AddInstanceInfo(instance);
        }

        private static AdfFile.InstanceMemberInfo ImportInstanceMemberInfo(XPathNavigator memberNode, AdfFile adf)
        {
            AdfFile.InstanceMemberInfo member = new AdfFile.InstanceMemberInfo();

            member.Name = memberNode.GetAttribute("name", "");
            string stringId = memberNode.GetAttribute("id", "");
            string ownIACopy = memberNode.GetAttribute("own-copy-of-inline-arrays", "");
            if (!string.IsNullOrEmpty(stringId))
                member.Id = long.Parse(stringId, CultureInfo.InvariantCulture);
            member.isReferenceToId = !string.IsNullOrEmpty(memberNode.GetAttribute("is-reference", ""));
            if (!string.IsNullOrEmpty(ownIACopy))
                member.HasOwnCopyOfInlineArrays = true;

            Enum.TryParse(memberNode.GetAttribute("gbltype", ""), out member.Type);
            string typeString = memberNode.GetAttribute("type", "");

            // Load the value
            if (member.Type == TypeDefinitionType.Primitive)
            {
                switch (typeString)
                {
                    case "int8":
                        member.Data.WriteValueS8(sbyte.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Int8;
                        break;
                    case "uint8":
                        member.Data.WriteValueU8(byte.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.UInt8;
                        break;
                    case "int16":
                        member.Data.WriteValueS16(short.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Int16;
                        break;
                    case "uint16":
                        member.Data.WriteValueU16(ushort.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.UInt16;
                        break;
                    case "int32":
                        member.Data.WriteValueS32(int.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Int32;
                        break;
                    case "uint32":
                        member.Data.WriteValueU32(uint.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.UInt32;
                        break;
                    case "int64":
                        member.Data.WriteValueS64(long.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Int64;
                        break;
                    case "uint64":
                        member.Data.WriteValueU64(ulong.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.UInt64;
                        break;
                    case "float":
                        member.Data.WriteValueF32(float.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Float;
                        break;
                    case "double":
                        member.Data.WriteValueF64(double.Parse(memberNode.Value, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Double;
                        break;
                    case "string":
                        string strRef = memberNode.GetAttribute("reference-string-from", "");
                        if (!string.IsNullOrEmpty(strRef))
                        {
                            member.ReferenceToString = true;
                            member.StringTableId = int.Parse(strRef, CultureInfo.InvariantCulture);
                        }
                        member.StringData = memberNode.Value;
                        member.TypeHash = AdfTypeHashes.String;
                        break;
                }
            }
            else
            {
                uint typeHash = uint.Parse(memberNode.GetAttribute("type-hash", ""), CultureInfo.InvariantCulture);
                member.TypeDef = adf.Runtime.GetTypeDefinition(typeHash);
                member.TypeHash = typeHash;
                if (member.TypeDef.Type != member.Type)
                    throw new InvalidOperationException("do not touch things like type and type-name in the xml file, yo");

                if (member.Type == TypeDefinitionType.StringHash)
                    member.StringData = memberNode.Value;
            }

            // handle sub members
            if (memberNode.GetAttribute("int8-array-is-string-array", "") == "yay")
            {
                // we found an array of inline-strings
                var subMembersList = memberNode.Select("inline-string");
                foreach (XPathNavigator subMemberNode in subMembersList)
                {
                    // it is an inline string. Get its value and write the string
                    byte[] str = Encoding.UTF8.GetBytes(subMemberNode.Value);
                    foreach (byte b in str)
                    {
                        AdfFile.InstanceMemberInfo subMember = new AdfFile.InstanceMemberInfo();
                        subMember.TypeHash = AdfTypeHashes.Primitive.Int8;
                        subMember.Type = TypeDefinitionType.Primitive;
                        subMember.Data.WriteValueU8(b);
                        member.Members.Add(subMember);
                    }
                    AdfFile.InstanceMemberInfo lastByteMember = new AdfFile.InstanceMemberInfo();
                    lastByteMember.TypeHash = AdfTypeHashes.Primitive.Int8;
                    lastByteMember.Type = TypeDefinitionType.Primitive;
                    lastByteMember.Data.WriteValueU8(0);
                    member.Members.Add(lastByteMember);
                }
            }
            else
            {
                var subMembersList = memberNode.Select("member");
                foreach (XPathNavigator subMemberNode in subMembersList)
                {
                    member.Members.Add(ImportInstanceMemberInfo(subMemberNode, adf));
                }
            }

            return member;
        }

    }
}
