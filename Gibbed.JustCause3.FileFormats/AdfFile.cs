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
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.FileFormats
{
    public class AdfFile
    {
        public const uint Signature = 0x41444620; // 'ADF '

        #region Fields
        private Endian _Endian;
        private string _Comment;
        private readonly List<TypeDefinition> _TypeDefinitions;
        private readonly List<InstanceInfo> _InstanceInfos;
        private readonly List<StringHashInfo> _StringHashInfos;
        private RuntimeTypeLibrary _Runtime;
        #endregion

        public AdfFile(string _extension = "")
        {
            this.extension = _extension;
            this._TypeDefinitions = new List<TypeDefinition>();
            this._InstanceInfos = new List<InstanceInfo>();
            this._StringHashInfos = new List<StringHashInfo>();
            this._Runtime = new RuntimeTypeLibrary();
            this._Comment = "";
        }
        public void AddInstanceInfo(InstanceInfo ii)
        {
            ii.Adf = this;
            this._InstanceInfos.Add(ii);
        }

        #region Properties
        public string extension;
        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public string Comment
        {
            get { return this._Comment; }
            set { this._Comment = value; }
        }

        public List<TypeDefinition> TypeDefinitions
        {
            get { return this._TypeDefinitions; }
        }

        public List<InstanceInfo> InstanceInfos
        {
            get { return this._InstanceInfos; }
        }

        public List<StringHashInfo> StringHashInfos
        {
            get { return this._StringHashInfos; }
        }
        public RuntimeTypeLibrary Runtime
        {
            get { return this._Runtime; }
        }
        #endregion

        public int EstimateHeaderSize()
        {
            return 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + (this._Comment.Length + 1)       + 8 /* ? */;
        }

        public void Serialize(Stream output, long headerPosition)
        {
            var endian = this._Endian;
            var names = new StringTable(null);
            var hashInfos = new List<StringHashInfo>();

            long instancePosition = 0;
            long typeDefinitionPosition = 0;
            long stringHashPosition = 0;
            long nameTablePosition = 0;

            if (this._InstanceInfos.Count > 0)
            {
                instancePosition = output.Position.Align(16); // should always be 80
                long dataPosition = instancePosition;
                foreach (var instanceInfo in this._InstanceInfos)
                {
                    dataPosition = dataPosition.Align(instanceInfo.Type.Alignment);
                    instanceInfo.PrepareForWritting(output, endian, names, hashInfos, (uint)dataPosition);
                    dataPosition += instanceInfo.Size;
                }
                instancePosition = dataPosition.Align(16);
                foreach (var instanceInfo in this._InstanceInfos)
                {
                    instanceInfo.WriteBody(output, endian);
                }
                output.Position = instancePosition;
                foreach (var instanceInfo in this._InstanceInfos)
                {
                    instanceInfo.WriteHeader(output, endian, names, hashInfos, (uint)dataPosition);
                }
            }

            if (this._TypeDefinitions.Count > 0)
            {
                typeDefinitionPosition = output.Position;
                foreach (var typeDefinition in this._TypeDefinitions)
                {
                    typeDefinition.Write(output, endian, names);
                }
            }
            else
                throw new FormatException("Missing types of the ADF format");

            if (hashInfos.Count > 0)
            {
                hashInfos = hashInfos.Distinct().ToList();
                stringHashPosition = output.Position;
                foreach (var stringHashInfo in hashInfos.OrderBy(shi => shi.ValueHash))
                {
                    stringHashInfo.Write(output, endian);
                }
            }
            else
                Console.WriteLine("no string hash in this file");

            if (names.Items.Count > 0)
            {
                nameTablePosition = output.Position;
                var nameBytes = new byte[names.Items.Count][];
                for (int i = 0; i < names.Items.Count; i++)
                {
                    nameBytes[i] = Encoding.ASCII.GetBytes(names.Items[i]);
                    var nameLength = nameBytes[i].Length;
                    if (nameLength > byte.MaxValue)
                    {
                        throw new FormatException();
                    }
                    output.WriteValueU8((byte)nameLength);
                }
                for (int i = 0; i < nameBytes.Length; i++)
                {
                    output.WriteBytes(nameBytes[i]);
                    output.WriteValueU8(0);
                }
            }
            var endPosition = output.Position;

            output.Position = headerPosition;
            output.WriteValueU32(Signature, endian);
            output.WriteValueU32(4, endian); // version

            output.WriteValueU32((uint)this._InstanceInfos.Count, endian); // instanceCount
            output.WriteValueU32((uint)(instancePosition - headerPosition), endian); // instanceOffset
            //Console.WriteLine("> " + this._InstanceInfos.Count);
            //Console.WriteLine("> " + instancePosition);
            output.WriteValueU32((uint)this._TypeDefinitions.Count, endian); // typeDefinitionCount
            output.WriteValueU32((uint)(typeDefinitionPosition - headerPosition), endian); // typeDefinitionOffset
            //Console.WriteLine("> " + typeDefinitionPosition);
            output.WriteValueU32((uint)hashInfos.Count, endian); // stringHashCount
            output.WriteValueU32((uint)(stringHashPosition - headerPosition), endian); // stringHashOffset
            //Console.WriteLine("> " + stringHashPosition);
            output.WriteValueU32((uint)names.Items.Count, endian); // nameTableCount
            output.WriteValueU32((uint)(nameTablePosition - headerPosition), endian); // nameTableOffset
            //Console.WriteLine("> " + nameTablePosition);
            output.WriteValueU32((uint)(endPosition - headerPosition), endian); // totalSize

            output.WriteValueU32(0, endian); // unknown
            output.WriteValueU32(0, endian); // unknown
            output.WriteValueU32(0, endian); // unknown
            output.WriteValueU32(0, endian); // unknown
            output.WriteValueU32(0, endian); // unknown
            output.WriteStringZ(this._Comment, Encoding.ASCII); // comment
        }

        // forParsingOnly should remain false, except if you will NEVER try to reserialize
        // the generated data. ('cause it will generate a broken file)
        // This parameter is only here to speedup batch parsing
        public void Deserialize(Stream input, bool forParsingOnly = false)
        {
            var basePosition = input.Position;

            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU32(endian);
            if (version != 4)
            {
                throw new FormatException();
            }

            var instanceCount = input.ReadValueU32(endian);
            var instanceOffset = input.ReadValueU32(endian);
            //Console.WriteLine("> " + instanceCount);
            //Console.WriteLine("> " + instanceOffset);
            var typeDefinitionCount = input.ReadValueU32(endian);
            var typeDefinitionOffset = input.ReadValueU32(endian);
            //Console.WriteLine("> " + typeDefinitionOffset);
            var stringHashCount = input.ReadValueU32(endian);
            var stringHashOffset = input.ReadValueU32(endian);
            //Console.WriteLine("> " + stringHashOffset);
            var nameTableCount = input.ReadValueU32(endian);
            var nameTableOffset = input.ReadValueU32(endian);
            //Console.WriteLine("> " + nameTableOffset);
            var totalSize = input.ReadValueU32(endian);
            var unknown2C = input.ReadValueU32(endian);
            var unknown30 = input.ReadValueU32(endian);
            var unknown34 = input.ReadValueU32(endian);
            var unknown38 = input.ReadValueU32(endian);
            var unknown3C = input.ReadValueU32(endian);
            var comment = input.ReadStringZ(Encoding.ASCII);

            if (unknown2C != 0 || unknown30 != 0 || unknown34 != 0 || unknown38 != 0 || unknown3C != 0)
            {
                throw new FormatException("DEBUG: one of the unknown fields has an unknown value");
            }

            if (basePosition + totalSize > input.Length)
            {
                throw new EndOfStreamException();
            }

            var rawNames = new string[nameTableCount];
            if (nameTableCount > 0)
            {
                input.Position = basePosition + nameTableOffset;
                var nameLengths = new byte[nameTableCount];
                for (uint i = 0; i < nameTableCount; i++)
                {
                    nameLengths[i] = input.ReadValueU8();
                }
                for (uint i = 0; i < nameTableCount; i++)
                {
                    rawNames[i] = input.ReadString(nameLengths[i], true, Encoding.ASCII);
                    input.Seek(1, SeekOrigin.Current);
                }
            }
            var names = new StringTable(rawNames);

            var typeDefinitions = new TypeDefinition[typeDefinitionCount];
            if (typeDefinitionCount > 0)
            {
                input.Position = basePosition + typeDefinitionOffset;
                for (uint i = 0; i < typeDefinitionCount; i++)
                {
                    typeDefinitions[i] = TypeDefinition.Read(input, endian, names);
                }
            }

            var stringHashInfos = new StringHashInfo[stringHashCount];
            if (stringHashCount > 0)
            {
                input.Position = basePosition + stringHashOffset;
                for (uint i = 0; i < stringHashCount; i++)
                {
                    stringHashInfos[i] = StringHashInfo.Read(input, endian);
                }
            }

            this._Endian = endian;
            this._Comment = comment;
            this._TypeDefinitions.Clear();
            this._TypeDefinitions.AddRange(typeDefinitions);
            this._StringHashInfos.Clear();
            this._StringHashInfos.AddRange(stringHashInfos);
            this._Runtime.Clear();
            this._Runtime.AddTypeDefinitions(this);

            var instanceInfos = new InstanceInfo[instanceCount];
            if (instanceCount > 0)
            {
                input.Position = basePosition + instanceOffset;
                for (uint i = 0; i < instanceCount; i++)
                {
                    instanceInfos[i] = InstanceInfo.Read(input, endian, names, this, forParsingOnly);
                }
            }

            this._InstanceInfos.Clear();
            this._InstanceInfos.AddRange(instanceInfos);
        }

        public enum TypeDefinitionType : uint
        {
            Primitive = 0,
            Structure = 1,
            Pointer = 2,
            Array = 3,
            InlineArray = 4,
            String = 5,
            BitField = 7,
            Enumeration = 8,
            StringHash = 9,
        }

        public struct TypeDefinition
        {
            public TypeDefinitionType Type;
            public uint Size;
            public uint Alignment;
            public uint NameHash;
            public string Name;
            public uint Flags;
            public uint ElementTypeHash;
            public uint ElementLength;
            public MemberDefinition[] Members;

            internal static TypeDefinition Read(Stream input, Endian endian, StringTable stringTable)
            {
                var instance = new TypeDefinition();
                instance.Type = (TypeDefinitionType)input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                instance.Alignment = input.ReadValueU32(endian);
                instance.NameHash = input.ReadValueU32(endian);
                var nameIndex = input.ReadValueS64(endian);
                instance.Name = stringTable.Get(nameIndex);
                instance.Flags = input.ReadValueU32(endian);
                instance.ElementTypeHash = input.ReadValueU32(endian);
                instance.ElementLength = input.ReadValueU32(endian);

                switch (instance.Type)
                {
                    case TypeDefinitionType.Structure:
                    {
                        var memberCount = input.ReadValueU32(endian);
                        instance.Members = new MemberDefinition[memberCount];
                        for (uint i = 0; i < memberCount; i++)
                        {
                            instance.Members[i] = MemberDefinition.Read(input, endian, stringTable);
                        }
                        break;
                    }

                    case TypeDefinitionType.Array:
                    {
                        var memberCount = input.ReadValueU32(endian);
                        if (memberCount != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.InlineArray:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.Pointer:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    case TypeDefinitionType.StringHash:
                    {
                        var unknown = input.ReadValueU32(endian);
                        if (unknown != 0)
                        {
                            throw new FormatException();
                        }
                        break;
                    }

                    default:
                    {
                        throw new NotSupportedException();
                    }
                }

                return instance;
            }

            internal void Write(Stream output, Endian endian, StringTable stringTable)
            {
                output.WriteValueU32((uint)this.Type, endian);
                output.WriteValueU32(this.Size, endian);
                output.WriteValueU32(this.Alignment, endian);
                output.WriteValueU32(this.NameHash, endian);
                output.WriteValueS64(stringTable.GetIndex(this.Name), endian);
                output.WriteValueU32(this.Flags, endian);
                output.WriteValueU32(this.ElementTypeHash, endian);
                output.WriteValueU32(this.ElementLength, endian);

                switch (this.Type)
                {
                    case TypeDefinitionType.Structure:
                        {
                            output.WriteValueU32((uint)this.Members.Length, endian);
                            foreach (MemberDefinition md in this.Members)
                            {
                                md.Write(output, endian, stringTable);
                            }
                            break;
                        }

                    case TypeDefinitionType.Array:
                        {
                            output.WriteValueU32(0, endian);
                            break;
                        }

                    case TypeDefinitionType.InlineArray:
                        {
                            output.WriteValueU32(0, endian);
                            break;
                        }

                    case TypeDefinitionType.Pointer:
                        {
                            output.WriteValueU32(0, endian);
                            break;
                        }

                    case TypeDefinitionType.StringHash:
                        {
                            output.WriteValueU32(0, endian);
                            break;
                        }

                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X})", this.Name, this.NameHash);
            }
        }

        public struct MemberDefinition
        {
            public string Name;
            public uint TypeHash;
            public uint Size;
            public uint Offset;
            public uint DefaultType;
            public ulong DefaultValue;

            internal static MemberDefinition Read(Stream input, Endian endian, StringTable stringTable)
            {
                var instance = new MemberDefinition();
                var nameIndex = input.ReadValueS64(endian);
                instance.Name = stringTable.Get(nameIndex);
                instance.TypeHash = input.ReadValueU32(endian);
                instance.Size = input.ReadValueU32(endian);
                instance.Offset = input.ReadValueU32(endian);
                instance.DefaultType = input.ReadValueU32(endian);
                instance.DefaultValue = input.ReadValueU64(endian);
                return instance;
            }

            internal void Write(Stream output, Endian endian, StringTable stringTable)
            {
                var nameIndex = stringTable.GetIndex(this.Name);
                output.WriteValueS64(nameIndex, endian);
                output.WriteValueU32(this.TypeHash, endian);
                output.WriteValueU32(this.Size, endian);
                output.WriteValueU32(this.Offset, endian);
                output.WriteValueU32(this.DefaultType, endian);
                output.WriteValueU64(this.DefaultValue, endian);
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X}) @ {2:X} ({3}, {4})",
                                     this.Name,
                                     this.TypeHash,
                                     this.Offset,
                                     this.DefaultType,
                                     this.DefaultValue);
            }
        }

        // -----------------------------
        //  INSTANCE DATA (16b aligned -- always offset 80)
        //      - ELEMENT (16b aligned)
        //          - Copy of all previous inline arrays, in reverse element order
        //            Only present if the element is the last element containing -- at any depth -- inline arrays
        //            EXCEPT if this last element is referenced at the end of the previous one:
        //            in that case the copy is placed with the previous element (I think, but still, that's not sure).
        //            It can sometime be only the copy of the inline arrays of the current element ONLY.
        //            Or sometime it could not be present at all.
        //            There's nothing you can do to know this except bruteforcing possibilities...
        //            There's even intermediate possibilities, i guess, but for these your hex editor may work better than this
        //          - String table of the element (8bit aligned)
        //            Strings are in the order of declaration, duplicates are allowed
        //            It is possible for an element to reference strings from any other element's string tables
        //      - ...
        // -----------------------------
        //  INSTANCE HEADERS
        // -----------------------------
        public class InstanceInfo
        {
            public uint NameHash;
            public uint TypeHash;
            public uint Offset;
            public uint Size;
            public string Name;
            public MemoryStream Data; // instance data (from offset to offset+size)
            public List<InstanceMemberInfo> Members; // members of instance info
            public TypeDefinition Type;
            public int InlineArrayIndex; // The index of the member after which to put the inline array
            public uint MinInlineArraySize; // I don't like this, but that's the only solution for some ADF files...

            internal List<InstanceStringTable> _MembersStrings;
            private Queue<InstanceMemberInfo> _WorkQueue; // members of instance info
            private List<long> _MembersStringsOffsets;
            private AdfFile _Adf;
            private uint _InlineArrayCopyBasePosition;
            private bool _ForParsingOnly;

            public AdfFile Adf
            {
                get { return this._Adf; }
                set { this._Adf = value; }
            }

            // flat list of members, listed by offset (only used to create the list, then is cleared)
            internal Dictionary<long, InstanceMemberInfo> OffsetMembers;

            internal static uint GetHeaderSize()
            {
                return 4 + 4 + 4 + 4 + 8;
            }

            // This is called on the header
            internal static InstanceInfo Read(Stream input, Endian endian, StringTable stringTable, AdfFile adf, bool forParsingOnly)
            {
                var instance = new InstanceInfo();
                instance.NameHash = input.ReadValueU32(endian);
                instance.TypeHash = input.ReadValueU32(endian);
                instance.Offset = input.ReadValueU32(endian);
                //Console.WriteLine("$ " + instance.Offset);
                instance.Size = input.ReadValueU32(endian);
                //Console.WriteLine("$ " + instance.Size);
                var nameIndex = input.ReadValueS64(endian);

                instance.Name = stringTable.Get(nameIndex);

                var oldPosition = input.Position;
                input.Position = instance.Offset;
                instance.Data = input.ReadToMemoryStream(instance.Size);
                instance.Data.Position = 0;
                input.Position = oldPosition;

                instance.Type = adf._Runtime.GetTypeDefinition(instance.TypeHash);
                instance._Adf = adf;
                instance._ForParsingOnly = forParsingOnly;

                instance._WorkQueue = new Queue<InstanceMemberInfo>();
                instance.OffsetMembers = new Dictionary<long, InstanceMemberInfo>();
                instance.Members = new List<InstanceMemberInfo>();
                instance.MembersRead();

                if (forParsingOnly == false) // a little speedup ?
                {
                    instance.SetupStringReferences();
                    instance.GuessInlineArrayCopyPosition();
                }
                return instance;
            }


            // create the structure
            private void MembersRead()
            {
                this._WorkQueue.Clear();
                this.OffsetMembers.Clear();

                // (automatically added to the workQueue by the constructor)
                this.Members.Add(new InstanceMemberInfo(0, this.Name, this.Type, this.Data.Position, this));

                while (this._WorkQueue.Count > 0)
                {
                    var imi = this._WorkQueue.Dequeue();
                    switch (imi.Type)
                    {
                        case TypeDefinitionType.Structure:
                            MemberSetupStruct(imi);
                            break;
                        case TypeDefinitionType.Array:
                        case TypeDefinitionType.InlineArray:
                            MemberSetupArray(imi);
                            break;
                        case TypeDefinitionType.StringHash:
                            MemberSetupStringHash(imi);
                            break;
                        default:
                            throw new NotImplementedException("unknow type, yo" + imi.Type); // NOPE
                    }
                }
            }

            // setup a InstanceMemberInfo (a non-primitive only)
            internal void MemberSetup(InstanceMemberInfo imi)
            {
                this._WorkQueue.Enqueue(imi);
            }

            internal void MemberSetupStruct(InstanceMemberInfo imi)
            {
                foreach (var memberDefinition in imi.TypeDef.Members)
                {
                    MemberSetupStructMember(imi, memberDefinition.TypeHash, memberDefinition.Name, imi.Offset + memberDefinition.Offset);
                }
            }
            // Members, structs, struct's memebers, ...
            internal void MemberSetupStructMember(InstanceMemberInfo structImi, uint typeHash, string name, long offset)
            {
                Data.Position = offset;
                switch (typeHash) // split primitives from weird types (non-primitives)
                {
                    case AdfTypeHashes.Primitive.Int8:
                    case AdfTypeHashes.Primitive.UInt8:
                    case AdfTypeHashes.Primitive.Int16:
                    case AdfTypeHashes.Primitive.UInt16:
                    case AdfTypeHashes.Primitive.Int32:
                    case AdfTypeHashes.Primitive.UInt32:
                    case AdfTypeHashes.Primitive.Int64:
                    case AdfTypeHashes.Primitive.UInt64:
                    case AdfTypeHashes.Primitive.Float:
                    case AdfTypeHashes.Primitive.Double:
                        {
                            structImi.Members.Add(new InstanceMemberInfo(-1, name, typeHash, Data.Position, this));
                            break;
                        }
                    case AdfTypeHashes.String:
                        {
                            long destOffset = Data.ReadValueS64(this._Adf.Endian);
                            InstanceMemberInfo imi = new InstanceMemberInfo(-1, name, typeHash, destOffset, this);
                            imi.LocalOffset = offset - structImi.Offset;
                            structImi.Members.Add(imi);
                            break;
                        }
                    default:
                        {
                            var typeDefinition = this._Adf._Runtime.GetTypeDefinition(typeHash);
                            switch (typeDefinition.Type)
                            {
                                case TypeDefinitionType.InlineArray:
                                case TypeDefinitionType.Structure:
                                case TypeDefinitionType.StringHash:
                                    {
                                        var new_imi = new InstanceMemberInfo(-1, name, typeDefinition, Data.Position, this, typeDefinition.ElementLength);
                                        new_imi.TypeHash = typeHash;
                                        structImi.Members.Add(new_imi);
                                        break;
                                    }
                                case TypeDefinitionType.Array:
                                    {
                                        long destOffset = Data.ReadValueS64(this._Adf.Endian);
                                        long elemCount = Data.ReadValueS64(this._Adf.Endian);
                                        // handle the special case of a non-existing array
                                        if (destOffset == 0 && elemCount == 0)
                                        {
                                            var dummyImi = new InstanceMemberInfo(-1, name, typeDefinition);
                                            dummyImi.TypeHash = typeHash;
                                            structImi.Members.Add(dummyImi); // and the ref

                                            break;
                                        }
                                        // handle the other cases
                                        Data.Position = destOffset;
                                        long id = 0;
                                        if (OffsetMembers.ContainsKey(destOffset)) // we already have something for this id...
                                            id = OffsetMembers[destOffset].Id;
                                        else // not yet
                                        {
                                            id = this.Members.Count;
                                            //Console.WriteLine("D" + destOffset + "\t:" + elemCount + "\t:" + id + "\t:" + typeHash);

                                            var new_imi = new InstanceMemberInfo(id, name, typeDefinition, destOffset, this, elemCount);
                                            new_imi.TypeHash = typeHash;

                                            // get the correct index
                                            int insertionIndex = this.Members.Count;
                                            for (int i = 0; i < insertionIndex; ++i)
                                            {
                                                if (destOffset < this.Members[i].Offset)
                                                    insertionIndex = i;
                                            }
                                            this.Members.Insert(insertionIndex, new_imi);
                                            OffsetMembers.Add(destOffset, new_imi);
                                        }
                                        var refImi = new InstanceMemberInfo(id, name, typeDefinition);
                                        refImi.TypeHash = typeHash;
                                        structImi.Members.Add(refImi); // and the ref
                                        break;
                                    }
                                case TypeDefinitionType.Pointer:
                                    structImi.Members.Add(new InstanceMemberInfo(-1, name, AdfTypeHashes.Primitive.UInt64, Data.Position, this));
                                    break;
                                default:
                                    throw new NotImplementedException("unknown struct member type, yo" + typeDefinition.Type.ToString());
                            }
                            break;
                        }
                }
            }

            internal void MemberSetupArray(InstanceMemberInfo imi)
            {
                long elemSize = 0;
                bool isPrimitive = true;
                switch (imi.TypeDef.ElementTypeHash)
                {
                    case AdfTypeHashes.Primitive.Int8:
                    case AdfTypeHashes.Primitive.UInt8:
                        elemSize = 1;
                        break;
                    case AdfTypeHashes.Primitive.Int16:
                    case AdfTypeHashes.Primitive.UInt16:
                        elemSize = 2;
                        break;
                    case AdfTypeHashes.Primitive.Int32:
                    case AdfTypeHashes.Primitive.UInt32:
                    case AdfTypeHashes.Primitive.Float:
                        elemSize = 4;
                        break;
                    case AdfTypeHashes.String:
                        isPrimitive = false;
                        goto case AdfTypeHashes.Primitive.Double;
                    case AdfTypeHashes.Primitive.Int64:
                    case AdfTypeHashes.Primitive.UInt64:
                    case AdfTypeHashes.Primitive.Double:
                        elemSize = 8;
                        break;
                    default:
                        {
                            isPrimitive = false;
                            elemSize = this._Adf._Runtime.GetTypeDefinition(imi.TypeDef.ElementTypeHash).Size;
                            break;
                        }
                }
                if (elemSize <= 0)
                    throw new IndexOutOfRangeException("Negative or null element size");

                if (isPrimitive == false)
                {
                    for (long i = 0; i < imi.ExpectedElementCount; i++)
                        MemberSetupStructMember(imi, imi.TypeDef.ElementTypeHash, null, imi.Offset + elemSize * i);
                }
                else
                {
                    for (long i = 0; i < imi.ExpectedElementCount; i++)
                        imi.Members.Add(new InstanceMemberInfo(-1, null, imi.TypeDef.ElementTypeHash, imi.Offset + elemSize * i, this));
                }

            }

            internal void MemberSetupStringHash(InstanceMemberInfo imi)
            {
                Data.Position = imi.Offset;
                var value = Data.ReadValueU32(this._Adf.Endian);
                var stringHashInfo = this._Adf
                                         .StringHashInfos
                                         .FirstOrDefault(shi => shi.ValueHash == value);
                if (stringHashInfo != default(AdfFile.StringHashInfo))
                {
                    imi.StringData = (stringHashInfo.Value);
                }
                else
                {
                    imi.StringData = null;
                    imi.Hash = value;
                }
            }

            // a thing to handle the trick with strings
            internal void SetupStringReferences()
            {
                foreach (var member in this.Members)
                    member.SetupStrings(this, (int)member.Id);
            }

            // I don't want to guess the condition for the position of the copy
            // So this will search for a gap large enough to hold the copy
            // There's a lot of cases to handle and I think I handle most of them:
            //      - the case where inline arrays are present, but not copy
            //      - the case where inline arrays are present, but there's a copy somewhere arbitrary
            //      - the case where inline arrays are present, but only a subset of them have a copy right below the member
            // Any other cases are probably too dificult to guess with only the size maybe a bruteforce data check would resolve it
            // but that's a bit too much rite now.
            internal void GuessInlineArrayCopyPosition()
            {
                List<long> originalOffsets = new List<long>();
                List<StringHashInfo> hashInfos = new List<StringHashInfo>();
                uint inlineArrayMinSize = 0; // raw size (excluding any padding)

                this._MembersStrings = new List<InstanceStringTable>();
                for (int i = 0; i < this.Members.Count; i++)
                    this._MembersStrings.Add(new InstanceStringTable());

                // populate the lists
                foreach (var member in this.Members)
                {
                    originalOffsets.Add(member.Offset);
                    InstanceStringTable ist = new InstanceStringTable();
                    member.PrepareForWritting(this, ist, hashInfos, member.Offset);

                    inlineArrayMinSize += member.GetInlineArraySize();
                }

                originalOffsets.Add(this.Size); // the last offset is the end
                this._MembersStrings = null;
                //Console.WriteLine("iams {0}", inlineArrayMinSize);

                if (inlineArrayMinSize != 0)
                {
                    List<int> gapIndexes = new List<int>();
                    List<uint> gapSizes = new List<uint>();

                    int index = 0;
                    // search for the copy index
                    foreach (var member in this.Members)
                    {
                        InstanceStringTable ist = new InstanceStringTable();
                        member.PrepareForWritting(this, ist, hashInfos, member.Offset);

                        uint elemSize = member.Size + inlineArrayMinSize;
                        uint normalElemSize = member.Size;
                        // compute the size of the string chunk
                        for (int i = 0; i < ist.Items.Count; i++)
                        {
                            elemSize = ((uint)member.Offset + elemSize).Align(8) - (uint)member.Offset;
                            normalElemSize = ((uint)member.Offset + normalElemSize).Align(8) - (uint)member.Offset;
                            elemSize += 1 + (uint)Encoding.ASCII.GetBytes(ist.Items[i].Item2).Length;
                            normalElemSize += 1 + (uint)Encoding.ASCII.GetBytes(ist.Items[i].Item2).Length;
                        }
                        elemSize = ((uint)member.Offset + elemSize).Align(16) - (uint)member.Offset;
                        normalElemSize = ((uint)member.Offset + normalElemSize).Align(16) - (uint)member.Offset;

                        //Console.WriteLine(" -- {0} + {1} <= {2}", member.Offset,  elemSize, originalOffsets[index + 1]);
                        //Console.WriteLine("   -- {0} + {1} <= {2}", member.Offset,  normalElemSize, originalOffsets[index + 1]);

                        if (member.Offset + elemSize <= originalOffsets[index + 1])
                        {
                            this.InlineArrayIndex = index;
                            //Console.WriteLine("found {0}", this.InlineArrayIndex);
                            this.MinInlineArraySize = (uint)(originalOffsets[index + 1] - (member.Offset + normalElemSize));
                            // happy ending:
                            return;
                        }
                        if (member.Offset + normalElemSize < originalOffsets[index + 1])
                        {
                            uint cGapSize = (uint)(originalOffsets[index + 1] - (member.Offset + normalElemSize));
                            gapIndexes.Add(index);
                            gapSizes.Add(cGapSize);
                            //Console.WriteLine("found a gap of {0} at {1}", -member.Offset + -normalElemSize + originalOffsets[index + 1], index);
                        }
                        ++index;
                    }

                    // we've found gaps, so maybe the current element inline arrays will fit in
                    if (gapIndexes.Count > 0)
                    {
                        //Console.WriteLine("using gap at {0}", gapIndex);

                        for (int i = 0; i < gapIndexes.Count; ++i)
                        {
                            uint currentIASize = this.Members[gapIndexes[i]].GetInlineArraySize();
                            if (currentIASize <= gapSizes[i])
                            {
                                // unlock gap ending:
                                this.Members[gapIndexes[i]].HasOwnCopyOfInlineArrays = true;
                                //Console.WriteLine("gap at {0} is large enough for element's own copy", gapIndexes[i]);
                            }
                        }
                    }
                }
                // gap ending may also need this
                this.InlineArrayIndex = -1;

                //Console.WriteLine("fallback to {0}", this.InlineArrayIndex);
                // gap/ghost ending:
                return;
            }


            internal static void WriteHeader(Stream output, InstanceInfo instance, Endian endian, StringTable stringTable, List<StringHashInfo> hashInfos, uint offset)
            {
                instance.WriteHeader(output, endian, stringTable, hashInfos, offset);
            }

            internal void PrepareForWritting(Stream output, Endian endian, StringTable stringTable, List<StringHashInfo> hashInfos, uint offset)
            {
                this.Offset = offset.Align(this.Type.Alignment);

                this.Size = 0;
                this._MembersStrings = new List<InstanceStringTable>();
                this._MembersStringsOffsets = new List<long>();

                // pre-init loop
                for (int i = 0; i < this.Members.Count; ++i)
                {
                    this._MembersStrings.Add(new InstanceStringTable());
                    this._MembersStringsOffsets.Add(0);
                }
                for (int i = 0; i < this.Members.Count; ++i)
                {
                    InstanceStringTable tmpST = new InstanceStringTable();
                    this.Members[i].PrepareForWritting(this, tmpST, hashInfos, 0); 
                }

                // init loop
                int index = 0;
                foreach (var member in Members)
                {
                    this.Size = this.Size.Align(16);
                    // the member (the weird thing with the string table is to keep strings in the correct order)
                    int stringTableIndex = (int)member.Id;
                    InstanceStringTable prevST = this._MembersStrings[stringTableIndex];
                    this._MembersStrings[stringTableIndex] = new InstanceStringTable();
                    member.PrepareForWritting(this, this._MembersStrings[stringTableIndex], hashInfos, this.Size);
                    for (int sti = 0; sti < prevST.Items.Count; ++sti)
                        this._MembersStrings[stringTableIndex].Put(prevST.Items[sti].Item2);
                    this.Size += member.Size;

                    // this is a weird thing in the ADF format. inline arrays have some étrange behaviours...
                    if (member.HasOwnCopyOfInlineArrays)
                        this.Size += member.GetInlineArraySize();
                    else if (index == this.InlineArrayIndex)
                    {
                        this._InlineArrayCopyBasePosition = this.Size;
                        bool first = true;
                        uint iaGblSize = 0;
                        // (notice the reverse order)
                        for (int i = 0; i < this.Members.Count; ++i)
                        {
                            uint iaSize = this.Members[this.Members.Count - i - 1].GetInlineArraySize();
                            if (iaSize != 0)
                            {
                                // align
                                if (!first)
                                    this.Size = this.Size.Align(this.Members[i].TypeDef.Alignment);
                                first = false;
                                // add
                                this.Size += iaSize;
                                iaGblSize += iaSize;
                            }
                        }
                        if (iaGblSize < this.MinInlineArraySize && this.MinInlineArraySize > 0)
                            this.Size += this.MinInlineArraySize - iaGblSize;
                    }

                    // the string table (after inline arrays)
                    if (this._MembersStrings[stringTableIndex].Items.Count > 0)
                    {
                        this.Size = this.Size.Align(8);
                        this._MembersStringsOffsets[stringTableIndex] = this.Size;

                        // compute the size of the string chunk
                        for (int i = 0; i < this._MembersStrings[stringTableIndex].Items.Count; i++)
                        {
                            this.Size = this.Size.Align(8);
                            if (this._MembersStrings[stringTableIndex].Items[i].Item2.Length > 0)
                                this.Size += 1 + (uint)Encoding.ASCII.GetBytes(this._MembersStrings[stringTableIndex].Items[i].Item2).Length;
                            else
                                this.Size += 4;
                        }
                    }

                    // increment
                    ++index;
                }

                this.Size = this.Size.Align(4); // NOT 8.
            }

            internal void WriteHeader(Stream output, Endian endian, StringTable stringTable, List<StringHashInfo> hashInfos, uint offset)
            {
                // write the header
                output.WriteValueU32(this.NameHash, endian);
                output.WriteValueU32(this.TypeHash, endian);
                output.WriteValueU32(this.Offset, endian);
                //Console.WriteLine("$" + this.Offset);
                output.WriteValueU32(this.Size, endian);
                //Console.WriteLine("$" + this.Size);
                var nameIndex = stringTable.Put(this.Name);
                output.WriteValueS64(nameIndex, endian);
            }

            internal static void WriteBody(Stream output, InstanceInfo instance, Endian endian)
            {
                instance.WriteBody(output, endian);
            }
            internal void WriteBody(Stream output, Endian endian)
            {
                // write strings (so we now have their offsets)
                for (int k = 0; k < this.Members.Count; ++k)
                {
                    int id = (int)this.Members[k].Id;
                    if (this._MembersStrings[id].Items.Count > 0)
                    {
                        output.Position = this._MembersStringsOffsets[id] + this.Offset;
                        for (int j = 0; j < this._MembersStrings[id].Items.Count; j++)
                        {
                            var relPos = output.Position - this.Offset;
                            output.Position = relPos.Align(8) + this.Offset;
                            this._MembersStrings[id].SetPosition(output.Position - this.Offset, this._MembersStrings[id].Items[j].Item2, this._MembersStrings[id].Items[j].Item1);
                            output.WriteStringZ(this._MembersStrings[id].Items[j].Item2, Encoding.ASCII);
                        }
                    }
                }

                // write the members
                output.Position = this.Offset;
                for (int i = 0; i < this.Members.Count; ++i)
                {
                    this.Members[i].Write(this, output, endian, this._MembersStrings[(int)this.Members[i].Id]);
                    if (this.Members[i].HasOwnCopyOfInlineArrays)
                    {
                        output.Position = this.Offset + this.Members[i].Offset + this.Members[i].Size;
                        this.Members[i].WriteInlineArrays(this, output, endian, this._MembersStrings[(int)this.Members[i].Id]);
                    }
                }

                // write the inline array things
                // (notice the reverse order)
                if (this.InlineArrayIndex < this.Members.Count && this.InlineArrayIndex >= 0)
                {
                    output.Position = this._InlineArrayCopyBasePosition + this.Offset;
                    for (int i = 0; i < this.Members.Count; ++i)
                    {
                        int index = this.Members.Count - 1 - i;
                        if (this.Members[index].HasInlineArray())
                        {
                            if (i != 0)
                                output.Position = output.Position.Align(this.Members[index].TypeDef.Alignment);
                            this.Members[index].WriteInlineArrays(this, output, endian, this._MembersStrings[(int)this.Members[index].Id]);
                        }
                    }
                }
            }

            public override string ToString()
            {
                return string.Format("{0} ({1:X})", this.Name, this.TypeHash);
            }
        }

        public class InstanceMemberInfo
        {
            public bool isReferenceToId;
            // only for first order structs and arrays (is -1 if not applicable, >0 if valid)
            // another special case: if a string hash has no string data, it may be the hash value
            public long Id;
            public string Name;
            public TypeDefinitionType Type;
            public uint TypeHash; // if Type is a primitive (also set when is a non-primitive member of a structure/array)
            public MemoryStream Data; // if Type is a primitive
            public string StringData; // if a string or a string hash
            public bool ReferenceToString; // if the string is not meant to be stored before it but after, somewhere. (weird case).
            public int StringTableId;     // The ID of the string table to lookup for the string (used only if ReferenceToString is true)
            public List<InstanceMemberInfo> Members; // members (type is either a structure or an array)
            public TypeDefinition TypeDef; // if a non-primitive and a non-reference
            public bool HasOwnCopyOfInlineArrays; // true if the element has its own and personal copy of its inline arrays
            public long FileOffset; // indicative only, the offset of the member within the whole file

            internal long ExpectedElementCount;
            internal long Offset;
            internal long LocalOffset; // for strings
            internal uint Size;
            internal uint Hash; // for HashStrings

            // empty constructor (used by the XML import)
            public InstanceMemberInfo()
            {
                this.Members = new List<InstanceMemberInfo>();
                this.Data = new MemoryStream();
                this.ReferenceToString = false;
                this.HasOwnCopyOfInlineArrays = false;
                this.FileOffset = 0;
            }

            // reference constructor
            public InstanceMemberInfo(long id, string name, TypeDefinition type)
            {
                this.Members = new List<InstanceMemberInfo>();
                this.isReferenceToId = true;
                this.ReferenceToString = false;
                this.HasOwnCopyOfInlineArrays = false;
                this.Id = id;
                this.Name = name;
                this.TypeDef = type;
                this.Type = type.Type;
                this.FileOffset = 0;
            }
            // non-primitive constructor
            public InstanceMemberInfo(long id, string name, TypeDefinition type, long offset, InstanceInfo ii, long expectedElementCount = 0)
            {
                this.Members = new List<InstanceMemberInfo>();
                this.isReferenceToId = false;
                this.ReferenceToString = false;
                this.HasOwnCopyOfInlineArrays = false;
                this.Type = type.Type;
                this.TypeDef = type;
                this.Name = name;
                this.Id = id;
                this.Offset = offset;
                this.ExpectedElementCount = expectedElementCount;
                this.FileOffset = 0;

                // A bit special how this works, but I'm lazy tonight
                ii.MemberSetup(this);
            }
            // primitive constructor
            public InstanceMemberInfo(long id, string name, uint typeHash, long offset, InstanceInfo ii)
            {
                this.Members = new List<InstanceMemberInfo>();
                this.isReferenceToId = false;
                this.ReferenceToString = false; // not set here
                this.HasOwnCopyOfInlineArrays = false;
                this.Type = TypeDefinitionType.Primitive;
                this.TypeHash = typeHash;
                this.Name = name;
                this.Id = id;
                this.Offset = offset;
                this.FileOffset = offset + ii.Offset;

                ii.Data.Position = offset;
                var size = 0;
                this.Data = new MemoryStream();
                switch (typeHash)
                {
                    case AdfTypeHashes.Primitive.Int8:
                    case AdfTypeHashes.Primitive.UInt8:
                        this.Data.WriteByte((byte)ii.Data.ReadByte());
                        size = 1;
                        break;
                    case AdfTypeHashes.Primitive.Int16:
                    case AdfTypeHashes.Primitive.UInt16:
                        this.Data.WriteValueU16(ii.Data.ReadValueU16());
                        size = 2;
                        break;
                    case AdfTypeHashes.Primitive.Int32:
                    case AdfTypeHashes.Primitive.UInt32:
                    case AdfTypeHashes.Primitive.Float:
                        this.Data.WriteValueU32(ii.Data.ReadValueU32());
                        size = 4;
                        break;
                    case AdfTypeHashes.Primitive.Int64:
                    case AdfTypeHashes.Primitive.UInt64:
                    case AdfTypeHashes.Primitive.Double:
                        this.Data.WriteValueU64(ii.Data.ReadValueU64());
                        size = 8;
                        break;
                }
                if (size != 0)
                {
                    //this.Data.WriteBytes(ii.Data.ReadBytes(size)); // that's slow :/
                    this.Data.Position = 0;
                }
                else if (typeHash == AdfTypeHashes.String) // very special case
                {
                    this.StringData = ii.Data.ReadStringZ(Encoding.ASCII);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            // Called to setup string references
            internal void SetupStrings(InstanceInfo ii, int instanceId)
            {
                if (this.Type == TypeDefinitionType.Primitive && this.TypeHash == AdfTypeHashes.String)
                {
                    this.ReferenceToString = false; // default state
                    for (int i = 0; i < ii.Members.Count - 1; ++i)
                    {
                        if (ii.Members[i].Id != instanceId
                            && this.Offset > ii.Members[i].Offset
                            && this.Offset < ii.Members[i + 1].Offset)
                        {
                            this.ReferenceToString = true;
                            this.StringTableId = (int)ii.Members[i].Id;
                            return;
                        }
                    }
                }
                else foreach (var member in this.Members)
                        member.SetupStrings(ii, instanceId);
            }

            // this won't actually write, I need a two pass thing to handle the strings
            // and all the information needed (offset, size, ...)
            internal void PrepareForWritting(InstanceInfo ii, InstanceStringTable stringTable, List<StringHashInfo> hashInfos, long offset)
            {
                this.Size = 0;
                this.Offset = offset;

                if (this.isReferenceToId) // ref to array
                    this.Size = 16; // 8 (array offset) + 8 (element count)
                else if (this.Type == TypeDefinitionType.Primitive) // we are a primitive (+ string)
                {
                    switch (this.TypeHash)
                    {
                        case AdfTypeHashes.Primitive.Int8:
                        case AdfTypeHashes.Primitive.UInt8:
                            this.Size = 1;
                            break;
                        case AdfTypeHashes.Primitive.Int16:
                        case AdfTypeHashes.Primitive.UInt16:
                            this.Size = 2;
                            break;
                        case AdfTypeHashes.Primitive.Int32:
                        case AdfTypeHashes.Primitive.UInt32:
                        case AdfTypeHashes.Primitive.Float:
                            this.Size = 4;
                            break;
                        case AdfTypeHashes.Primitive.Int64:
                        case AdfTypeHashes.Primitive.UInt64:
                        case AdfTypeHashes.Primitive.Double:
                            this.Size = 8;
                            break;
                        case AdfTypeHashes.String:
                            this.Size = 8; // string is in fact a 64b offset
                            if (this.ReferenceToString == false)
                                stringTable.Put(this.StringData, this.StringData.Length > 0 ? this.Offset : this.LocalOffset);
                            else
                                ii._MembersStrings[this.StringTableId].Put(this.StringData);
                            break;
                        default:
                            throw new NotImplementedException("Primitive Type not supported");
                    }
                }
                else
                {
                    switch (this.Type)
                    {
                        case TypeDefinitionType.InlineArray:
                            if (this.Members.Count != this.TypeDef.ElementLength)
                                throw new InvalidOperationException("Invalid count of item in inline array");
                            goto case TypeDefinitionType.Array; // fallthrough next case
                        case TypeDefinitionType.Array:
                            if (this.Type == TypeDefinitionType.Array && this.Members.Count == 0)
                            {
                                this.Offset = 0;
                                this.Size = 0;
                                return;
                            }
                            foreach (var member in this.Members)
                            {
                                if ((member.TypeHash != this.TypeDef.ElementTypeHash))
                                    throw new InvalidOperationException("Using array to store items of different type");
                                member.LocalOffset = this.Size;
                                member.PrepareForWritting(ii, stringTable, hashInfos, this.Offset + this.Size);
                                this.Size += member.Size;
                            }
                            break;
                        case TypeDefinitionType.Structure:
                            if (this.Members.Count != this.TypeDef.Members.Length)
                                throw new InvalidOperationException("Invalid count of member in structure");
                            for (int i = 0; i < this.Members.Count; ++i)
                            {
                                // TODO: commented for supporting some pointers (fix this if the game breaks).
                                //if (this.Members[i].TypeHash != this.TypeDef.Members[i].TypeHash)
                                    //throw new InvalidOperationException("Invalid member type in structure (member name: " + this.Members[i].Name + ")");
                                this.Members[i].LocalOffset = this.TypeDef.Members[i].Offset;
                                this.Members[i].PrepareForWritting(ii, stringTable, hashInfos, this.Offset + this.TypeDef.Members[i].Offset);
                            }
                            this.Size = this.TypeDef.Size;
                            break;
                        case TypeDefinitionType.StringHash:
                            this.Size = this.TypeDef.Size;
                            if (!String.IsNullOrEmpty(this.StringData))
                            {
                                StringHashInfo shi = StringHashInfo.FromString(this.StringData);
                                this.Hash = shi.ValueHash;
                                hashInfos.Add(shi);
                            }
                            break;
                        default:
                            throw new NotImplementedException("Type not supported");
                    }
                }
            }

            // NOTE: The inline array thing is in reverse order (from last to first)
            // and positioned after the last member of the instance that have an inline array.
            internal bool HasInlineArray()
            {
                if (this.Type == TypeDefinitionType.InlineArray)
                    return true;
                foreach (var member in this.Members)
                {
                    if (member.HasInlineArray())
                        return true;
                }
                return false;
            }

            // NOTE: I did not found a case where an inline array contained some inline arrays.
            // I only account the first inline array found. If JC3 crashes, then you may think that's because of
            // that assumption is false.
            // This assumes that PrepareForWritting() has been called
            internal uint GetInlineArraySize()
            {
                if (this.Type == TypeDefinitionType.InlineArray)
                    return this.Size;
                uint sum = 0;
                foreach (var member in this.Members)
                    sum += member.GetInlineArraySize();
                return sum;
            }

            // Write the inline array stuff
            internal void WriteInlineArrays(InstanceInfo ii, Stream output, Endian endian, InstanceStringTable stringOffsets)
            {
                if (this.Type == TypeDefinitionType.InlineArray)
                    this.Write(ii, output, endian, stringOffsets, false);
                else foreach (var member in this.Members)
                        member.WriteInlineArrays(ii, output, endian, stringOffsets);
            }

            // displace is a special thing used by the inline array stuff
            internal void Write(InstanceInfo ii, Stream output, Endian endian, InstanceStringTable stringOffsets, bool displace = true)
            {
                if (displace)
                    output.Position = ii.Offset + this.Offset;
                if (this.isReferenceToId) // ref to array
                {
                    if (this.Id == -1) // empty array
                    {
                        output.WriteValueS64(0, endian);
                        output.WriteValueS64(0, endian);
                    }
                    else // non-emtpy array
                    {
                        InstanceMemberInfo mRef = null;
                        foreach (var member in ii.Members)
                        {
                            if (member.Id == this.Id)
                            {
                                mRef = member;
                                break;
                            }
                        }
                        //Console.WriteLine("d" + mRef.Offset + "\t:" + mRef.Members.Count + "\t:" + mRef.Id);
                        output.WriteValueS64(mRef.Offset, endian);
                        output.WriteValueS64(mRef.Members.Count, endian);
                    }
                }
                else if (this.Type == TypeDefinitionType.Primitive) // we are a primitive (+ string)
                {
                    this.Data.Position = 0;
                    switch (this.TypeHash)
                    {
                        case AdfTypeHashes.Primitive.Int8:
                            output.WriteValueS8(this.Data.ReadValueS8());
                            break;
                        case AdfTypeHashes.Primitive.UInt8:
                            output.WriteValueU8(this.Data.ReadValueU8());
                            break;
                        case AdfTypeHashes.Primitive.Int16:
                            output.WriteValueS16(this.Data.ReadValueS16(), endian);
                            break;
                        case AdfTypeHashes.Primitive.UInt16:
                            output.WriteValueU16(this.Data.ReadValueU16(), endian);
                            break;
                        case AdfTypeHashes.Primitive.Int32:
                            output.WriteValueS32(this.Data.ReadValueS32(), endian);
                            break;
                        case AdfTypeHashes.Primitive.UInt32:
                            output.WriteValueU32(this.Data.ReadValueU32(), endian);
                            break;
                        case AdfTypeHashes.Primitive.Float:
                            output.WriteValueF32(this.Data.ReadValueF32(), endian);
                            break;
                        case AdfTypeHashes.Primitive.Int64:
                            output.WriteValueS64(this.Data.ReadValueS64(), endian);
                            break;
                        case AdfTypeHashes.Primitive.UInt64:
                            output.WriteValueU64(this.Data.ReadValueU64(), endian);
                            break;
                        case AdfTypeHashes.Primitive.Double:
                            output.WriteValueF64(this.Data.ReadValueF64(), endian);
                            break;
                        case AdfTypeHashes.String:
                            if (this.ReferenceToString == false)
                            {
                                var offset = this.StringData.Length > 0 ? this.Offset : this.LocalOffset;
                                output.WriteValueS64(stringOffsets.GetPosition(this.StringData, offset), endian);
                            }
                            else // reference to a string from another table
                                output.WriteValueS64(ii._MembersStrings[this.StringTableId].GetPosition(this.StringData), endian);
                            break;
                        default:
                            throw new NotImplementedException("Primitive Type not supported");
                    }
                }
                else
                {
                    switch (this.Type)
                    {
                        case TypeDefinitionType.InlineArray:
                        case TypeDefinitionType.Array:
                            foreach (var member in this.Members)
                                member.Write(ii, output, endian, stringOffsets, displace);
                            break;
                        case TypeDefinitionType.Structure:
                            long basePosition = output.Position;
                            foreach (var member in this.Members)
                            {
                                if (!displace) // replace the absolute offset by a relative one
                                    output.Position = basePosition + member.Offset - this.Offset;
                                member.Write(ii, output, endian, stringOffsets, displace);
                            }
                            break;
                        case TypeDefinitionType.StringHash:
                            output.WriteValueU32(this.Hash, endian);
                            break;
                        default:
                            throw new NotImplementedException("Type not supported (??)");
                    }
                }
            }
        }

        public struct StringHashInfo : IEquatable<StringHashInfo>
        {
            public string Value;
            public uint ValueHash;
            public uint Unknown;

            internal static StringHashInfo FromString(string value)
            {
                var valueHash = value.HashJenkins();
                return new StringHashInfo()
                {
                    Value = value,
                    ValueHash = valueHash,
                    Unknown = 0,
                };
            }
            internal static StringHashInfo FromHash(uint valueHash)
            {
                return new StringHashInfo()
                {
                    Value = null,
                    ValueHash = valueHash,
                    Unknown = 0,
                };
            }

            internal static StringHashInfo Read(Stream input, Endian endian)
            {
                var instance = new StringHashInfo();
                instance.Value = input.ReadStringZ(Encoding.ASCII);
                instance.ValueHash = input.ReadValueU32(endian);
                instance.Unknown = input.ReadValueU32(endian);
                return instance;
            }

            internal static void Write(Stream output, StringHashInfo instance, Endian endian)
            {
                output.WriteStringZ(instance.Value ?? "");
                output.WriteValueU32(instance.ValueHash, endian);
                output.WriteValueU32(instance.Unknown, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }

            public override string ToString()
            {
                return string.Format("{1:X} = {0} ({2})", this.Value, this.ValueHash, this.Unknown);
            }

            public bool Equals(StringHashInfo other)
            {
                return string.Equals(this.Value, other.Value) == true &&
                       this.Unknown == other.Unknown &&
                       this.ValueHash == other.ValueHash;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj) == true)
                {
                    return false;
                }

                return obj is StringHashInfo && Equals((StringHashInfo)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (int)this.Unknown;
                    hashCode = (hashCode * 397) ^ (int)this.ValueHash;
                    hashCode = (hashCode * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(StringHashInfo left, StringHashInfo right)
            {
                return left.Equals(right) == true;
            }

            public static bool operator !=(StringHashInfo left, StringHashInfo right)
            {
                return left.Equals(right) == false;
            }
        }

        internal class StringTable
        {
            private readonly List<string> _Items;

            public StringTable(string[] names)
            {
                this._Items = names == null ? new List<string>() : new List<string>(names);
            }

            public List<string> Items
            {
                get { return this._Items; }
            }

            public string Get(long index)
            {
                if (index < 0 || index >= this._Items.Count || index > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this._Items[(int)index];
            }

            public long GetIndex(string name)
            {
                if (String.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException("name");
                }

                long index = this._Items.IndexOf(name);
                if (index == -1)
                    return Put(name);
                    //throw new InvalidOperationException("'" + name + "' is not in the string table");
                return index;
            }

            public long Put(string text)
            {
                var index = this._Items.IndexOf(text);
                if (index >= 0)
                {
                    return index;
                }
                index = this._Items.Count;
                this._Items.Add(text);
                return index;
            }
        }
    }

    internal class InstanceStringTable
    {
        private readonly List<Tuple<long, string>> _Items;
        private readonly Dictionary<Tuple<long, string>, long> _OffsetPosDict;
        private readonly Dictionary<string, long> _ItemPosDict;

        public InstanceStringTable()
        {
            this._Items = new List<Tuple<long, string>>();
            this._OffsetPosDict = new Dictionary<Tuple<long, string>, long>();
            this._ItemPosDict = new Dictionary<string, long>();
        }

        public List<Tuple<long, string>> Items
        {
            get { return this._Items; }
        }

        public void SetPosition(long position, string name, long offset)
        {
            if (offset != -1)
                this._OffsetPosDict.Add(new Tuple<long, string>(offset, name), position);

            if (this._ItemPosDict.ContainsKey(name) == false)
                this._ItemPosDict.Add(name, position);
        }

        public long GetPosition(string name, long offset = -1)
        {
            if (offset != -1 && name.Length > 0)
                return this._OffsetPosDict[new Tuple<long, string>(offset, name)];
            return this._ItemPosDict[name];
        }

        // put with offset
        public long Put(string text, long offset)
        {
            var index = this._Items.IndexOf(new Tuple<long, string>(offset, text));
            if (index >= 0)
                return index;

            index = this._Items.IndexOf(new Tuple<long, string>(-1, text));
            if (index >= 0)
            {
                this._Items[index] = new Tuple<long, string>(offset, text);
                return index;
            }
            index = this._Items.Count;
            this._Items.Add(new Tuple<long, string>(offset, text));
            return index;
        }
        // put without offset
        public long Put(string text)
        {
            var index = -1;
            for (int i = 0; i < this._Items.Count; ++i)
            {
                if (this._Items[i].Item2 == text)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
                return index;
            index = this._Items.Count;
            this._Items.Add(new Tuple<long, string>(-1, text));
            return index;
        }
    }
}
