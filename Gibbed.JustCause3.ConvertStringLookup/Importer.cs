using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.XPath;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using MemberDefinition = Gibbed.JustCause3.FileFormats.AdfFile.MemberDefinition;
using TypeDefinition = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinition;
using TypeDefinitionType = Gibbed.JustCause3.FileFormats.AdfFile.TypeDefinitionType;
using InstanceInfo = Gibbed.JustCause3.FileFormats.AdfFile.InstanceInfo;

namespace Gibbed.JustCause3.ConvertStringLookup
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

            // Hold the contents of the last Array (A[int8])
            List<byte> stringArray = new List<byte>();

            // parse members
            instance.Members.Add(null); // prepare the future
            var memberNode = instanceNode.SelectSingleNode("member");
            instance.Members[0] = (ImportInstanceMemberInfo(memberNode, instance, adf, stringArray));

            var stringArrayMemberIndex = 0;
            foreach (var member in instance.Members)
            {
                if (member.Id == 2)
                    break;
                ++stringArrayMemberIndex;
            }
            var stringArrayMember = instance.Members[stringArrayMemberIndex];

            // Generate the string array (type is array of int8)
            foreach (byte b in stringArray) // the foreach is sooo overkill...
            {
                var subMember = new AdfFile.InstanceMemberInfo()
                {
                    Type = TypeDefinitionType.Primitive,
                    TypeHash = AdfTypeHashes.Primitive.Int8,
                    Name = null,
                };
                subMember.Data.WriteByte(b);
                stringArrayMember.Members.Add(subMember);
            }

            // add instance to ADF structure
            adf.AddInstanceInfo(instance);
        }

        private static AdfFile.InstanceMemberInfo ImportInstanceMemberInfo(XPathNavigator memberNode, AdfFile.InstanceInfo ii, AdfFile adf, List<byte> stringArray)
        {
            AdfFile.InstanceMemberInfo member = new AdfFile.InstanceMemberInfo();

            member.Name = memberNode.GetAttribute("name", "");
            string stringId = memberNode.GetAttribute("id", "");
            string ownIACopy = memberNode.GetAttribute("own-copy-of-inline-arrays", "");
            if (!string.IsNullOrEmpty(stringId))
                member.Id = long.Parse(stringId, CultureInfo.InvariantCulture);
            else
                member.Id = -1;
            member.isReferenceToId = !string.IsNullOrEmpty(memberNode.GetAttribute("is-reference", ""));
            if (!string.IsNullOrEmpty(ownIACopy))
                member.HasOwnCopyOfInlineArrays = true;

            Enum.TryParse(memberNode.GetAttribute("gbltype", ""), out member.Type);
            string typeString = memberNode.GetAttribute("type", "");

            if (member.Name == "TextOffset" || member.Name == "NameOffset") // hardcoded :( thingy to handle string lookup
                return ImportTextOffset(memberNode, member, ii, adf, stringArray);

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
                        member.Data.WriteValueS32(int.Parse(memberNode.Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                        member.TypeHash = AdfTypeHashes.Primitive.Int32;
                        break;
                    case "uint32":
                        member.Data.WriteValueU32(uint.Parse(memberNode.Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
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

                if (member.Type == TypeDefinitionType.Array && member.isReferenceToId == true && member.Id != -1)
                {
                    // No child, create an empty element
                    if (!memberNode.HasChildren)
                    {
                        var subMember = new AdfFile.InstanceMemberInfo()
                        {
                            Name = member.Name,
                            Id = member.Id,
                            isReferenceToId = false,
                            Type = member.Type,
                            TypeHash = member.TypeHash,
                            TypeDef = member.TypeDef,
                             HasOwnCopyOfInlineArrays = false, // maybe _that_ will cause some issues...
                        };
                        ii.Members.Add(subMember);
                        return member;
                    }
                    // the child node is in fact a global element.
                    XPathNavigator subMemberNode = memberNode.SelectSingleNode("member");
                    ii.Members.Add(ImportInstanceMemberInfo(subMemberNode, ii, adf, stringArray));
                    return member;
                }
            }

            // handle sub members
            var subMembersList = memberNode.Select("member");
            foreach (XPathNavigator subMemberNode in subMembersList)
            {
                var subMember = ImportInstanceMemberInfo(subMemberNode, ii, adf, stringArray);
                if (member.Name != "SortedPairs") // another hardcoded thing
                    member.Members.Add(subMember);
                else // insert the new member at the right place
                {
                    subMember.Members[0].Data.Position = 0;
                    uint hash = subMember.Members[0].Data.ReadValueU32();
                    subMember.Members[0].Data.Position = 0;
                    int i = 0;
                    for (; i < member.Members.Count; ++i)
                    {
                        member.Members[i].Members[0].Data.Position = 0;
                        uint cmpHash = member.Members[i].Members[0].Data.ReadValueU32();
                        member.Members[i].Members[0].Data.Position = 0;
                        if (cmpHash > hash)
                            break;
                    }
                    member.Members.Insert(i, subMember);
                }
            }

            return member;
        }

        private static AdfFile.InstanceMemberInfo ImportTextOffset(XPathNavigator memberNode, AdfFile.InstanceMemberInfo member, InstanceInfo ii, AdfFile adf, List<byte> stringArray)
        {
            member.TypeHash = AdfTypeHashes.Primitive.UInt32;
            member.Data.WriteValueU32((uint)stringArray.Count); // write the offset

            byte[] str = Encoding.UTF8.GetBytes(memberNode.Value);
            stringArray.AddRange(str);
            stringArray.Add(0); // the null byte to terminate the string

            return member;
        }
    }
}
