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
using Gibbed.IO;

namespace Gibbed.JustCause3.PropertyFormats
{
    public class PropertyContainerFile : IPropertyFile
    {
        public const uint Signature = 0x43505452; // 'RTPC'

        private Endian _Endian;
        private Node _Root;

        public PropertyContainerFile()
        {
            this._Root = null;
        }

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public Node Root
        {
            get { return this._Root; }
            set { this._Root = value; }
        }

        public static bool CheckSignature(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            return magic == Signature || magic.Swap() == Signature;
        }

        // ReSharper disable InconsistentNaming
        internal enum VariantType : byte
        {
            Unassigned = 0,
            Integer = 1,
            Float = 2,
            String = 3,
            Vector2 = 4,
            Vector3 = 5,
            Vector4 = 6,

            [Obsolete]
            DoNotUse1 = 7, // Matrix3x3

            Matrix4x4 = 8,
            Integers = 9,
            Floats = 10,
            Bytes = 11,

            [Obsolete]
            DoNutUse2 = 12,

            ObjectId = 13,
            Events = 14,
        }

        // ReSharper restore InconsistentNaming

        internal interface IRawVariant
        {
            VariantType Type { get; }
            bool IsPrimitive { get; }
            uint Alignment { get; }

            void Serialize(Stream output, Endian endian);
            void Deserialize(Stream input, Endian endian);
        }

        public void Serialize(Stream output)
        {
            var endian = this._Endian;

            if (this._Root == null)
            {
                output.WriteValueU32(Signature, endian);
                output.WriteValueU32(1, endian); // version
                new RawNode(0, 8 + RawNode.Size, 0, 0).Write(output, endian);
                return;
            }

            var stringOffsets = new Dictionary<string, uint>();

            using (var data = new MemoryStream())
            {
                data.WriteValueU32(Signature, endian);
                data.WriteValueU32(1, endian); // version

                var rawNodes = new List<Tuple<long, RawNode>>();

                data.Position += RawNode.Size; // root node size
                rawNodes.Add(new Tuple<long, RawNode>(data.Position - RawNode.Size, new RawNode(this._Root.NameHash,
                                          (uint)data.Position,
                                          (ushort)this._Root.Properties.Count,
                                          (ushort)this._Root.Children.Count)));

                // Node are stored as read in the XML file
                WriteNode(data, this._Root, stringOffsets, rawNodes);

                foreach (var tuple in rawNodes)
                {
                    var rawPosition = tuple.Item1;
                    var rawNode = tuple.Item2;
                    data.Position = rawPosition;
                    rawNode.Write(data, endian);
                }

                data.Flush();
                data.Position = 0;
                output.WriteFromStream(data, data.Length);
            }
        }

        private void WriteNode(Stream data, Node node, Dictionary<string, uint> stringOffsets, List<Tuple<long, RawNode>> rawNodes)
        {
            var endian = this._Endian;

            var propertyPosition = data.Position;
            var childPosition = (propertyPosition + node.Properties.Count * RawProperty.Size).Align(4);
            var propertyDataPosition = childPosition + (node.Children.Count * RawNode.Size);

            data.Position = propertyDataPosition;
            var rawProperties = new List<RawProperty>();
            foreach (var kv in node.Properties.OrderBy(kv => kv.Key))
            {
                var rawVariant = (IRawVariant)kv.Value;

                RawProperty rawProperty;
                if (rawVariant.IsPrimitive == true)
                {
                    var bytes = new byte[4];
                    using (var temp = new MemoryStream(bytes))
                    {
                        rawVariant.Serialize(temp, endian);
                    }

                    rawProperty = new RawProperty(kv.Key, bytes, rawVariant.Type);
                }
                else if (rawVariant is Variants.StringVariant)
                {
                    var stringVariant = (Variants.StringVariant)rawVariant;

                    uint dataOffset;
                    if (stringOffsets.ContainsKey(stringVariant.Value) == false)
                    {
                        if (rawVariant.Alignment > 0)
                        {
                            data.Position = data.Position.Align(rawVariant.Alignment);
                        }

                        dataOffset = (uint)data.Position;
                        rawVariant.Serialize(data, endian);

                        stringOffsets.Add(stringVariant.Value, dataOffset);
                    }
                    else
                    {
                        dataOffset = stringOffsets[stringVariant.Value];
                    }

                    var bytes = new byte[4];
                    using (var temp = new MemoryStream(bytes))
                    {
                        temp.WriteValueU32(dataOffset, endian);
                    }

                    rawProperty = new RawProperty(kv.Key, bytes, rawVariant.Type);
                }
                else
                {
                    if (rawVariant.Alignment > 0)
                    {
                        data.Position = data.Position.Align(rawVariant.Alignment);
                    }

                    var dataOffset = (uint)data.Position;
                    rawVariant.Serialize(data, endian);

                    var bytes = new byte[4];
                    using (var temp = new MemoryStream(bytes))
                    {
                        temp.WriteValueU32(dataOffset, endian);
                    }

                    rawProperty = new RawProperty(kv.Key, bytes, rawVariant.Type);
                }

                rawProperties.Add(rawProperty);
            }

            var childDataPosition = data.Position.Align(4);

            data.Position = propertyPosition;
            foreach (var rawProperty in rawProperties.OrderBy(kv => kv.NameHash))
            {
                rawProperty.Write(data, endian);
            }

            data.Position = childDataPosition;
            foreach (var child in node.Children.OrderBy(c => c.NameHash))
            {

                var rawNode = new RawNode(child.NameHash,
                          (uint)data.Position,
                          (ushort)child.Properties.Count,
                          (ushort)child.Children.Count);
                rawNodes.Add(new Tuple<long, RawNode>(childPosition, rawNode));

                childPosition += RawNode.Size;

                WriteNode(data, child, stringOffsets, rawNodes);
            }
        }

        public void Deserialize(Stream input)
        {
            var basePosition = input.Position;

            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                throw new FormatException();
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU32(endian);
            if (version != 1)
            {
                throw new FormatException();
            }

            var rawRootNode = RawNode.Read(input, endian);
            var rootNode = new Node();
            rootNode.NameHash = rawRootNode.NameHash;
            rootNode.DataOffset = rawRootNode.DataOffset;

            var instanceQueue = new Queue<Tuple<Node, RawNode>>();
            instanceQueue.Enqueue(new Tuple<Node, RawNode>(rootNode, rawRootNode));

            var propertyQueue = new Queue<Tuple<Node, RawProperty[]>>();

            while (instanceQueue.Count > 0)
            {
                var item = instanceQueue.Dequeue();
                var node = item.Item1;
                var rawNode = item.Item2;

                input.Position = basePosition + rawNode.DataOffset;
                var rawProperties = new RawProperty[rawNode.PropertyCount];
                for (int i = 0; i < rawNode.PropertyCount; i++)
                {
                    rawProperties[i] = RawProperty.Read(input, endian);
                }

                input.Position = basePosition +
                                 (rawNode.DataOffset + (RawProperty.Size * rawNode.PropertyCount)).Align(4);
                for (int i = 0; i < rawNode.InstanceCount; i++)
                {
                    var rawChildNode = RawNode.Read(input, endian);
                    var childNode = new Node();
                    childNode.NameHash = rawChildNode.NameHash;
                    childNode.DataOffset = rawChildNode.DataOffset;

                    node.Children.Add(childNode);
                    instanceQueue.Enqueue(new Tuple<Node, RawNode>(childNode, rawChildNode));
                }

                propertyQueue.Enqueue(new Tuple<Node, RawProperty[]>(node, rawProperties));
            }

            while (propertyQueue.Count > 0)
            {
                var item = propertyQueue.Dequeue();
                var node = item.Item1;
                var rawProperties = item.Item2;

                foreach (var rawProperty in rawProperties)
                {
                    var variant = VariantFactory.GetVariant(rawProperty.Type);

                    if (variant.IsPrimitive == true)
                    {
                        using (var temp = new MemoryStream(rawProperty.Data, false))
                        {
                            variant.Deserialize(temp, endian);

                            if (temp.Position != temp.Length)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }
                    else
                    {
                        if (rawProperty.Data.Length != 4)
                        {
                            throw new InvalidOperationException();
                        }

                        uint offset;
                        using (var temp = new MemoryStream(rawProperty.Data, false))
                        {
                            offset = temp.ReadValueU32(endian);
                        }

                        input.Position = basePosition + offset;
                        variant.Deserialize(input, endian);
                    }

                    node.Properties.Add(rawProperty.NameHash, (IVariant)variant);
                }
            }

            this._Root = rootNode;
        }

        private struct RawNode
        {
            public const uint Size = 4 + 4 + 2 + 2;

            public readonly uint NameHash;
            public readonly uint DataOffset;
            public readonly ushort PropertyCount;
            public readonly ushort InstanceCount;

            public RawNode(uint nameHash, uint dataOffset, ushort propertyCount, ushort instanceCount)
            {
                this.NameHash = nameHash;
                this.DataOffset = dataOffset;
                this.PropertyCount = propertyCount;
                this.InstanceCount = instanceCount;
            }

            public static RawNode Read(Stream input, Endian endian)
            {
                var nameHash = input.ReadValueU32(endian);
                var dataOffset = input.ReadValueU32(endian);
                var propertyCount = input.ReadValueU16(endian);
                var instanceCount = input.ReadValueU16(endian);
                return new RawNode(nameHash, dataOffset, propertyCount, instanceCount);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }

            public static void Write(Stream output, RawNode instance, Endian endian)
            {
                output.WriteValueU32(instance.NameHash, endian);
                output.WriteValueU32(instance.DataOffset, endian);
                output.WriteValueU16(instance.PropertyCount, endian);
                output.WriteValueU16(instance.InstanceCount, endian);
            }
        }

        private struct RawProperty
        {
            public const uint Size = 4 + 4 + 1;

            private static byte[] _DummyData;

            public readonly uint NameHash;
            public readonly byte[] Data;
            public readonly VariantType Type;

            public RawProperty(uint nameHash, byte[] data, VariantType type)
            {
                if (data == null)
                {
                    throw new ArgumentNullException("data");
                }

                if (data.Length != 4)
                {
                    throw new ArgumentOutOfRangeException("data");
                }

                this.NameHash = nameHash;
                this.Data = data;
                this.Type = type;
            }

            public static RawProperty Read(Stream input, Endian endian)
            {
                var nameHash = input.ReadValueU32(endian);
                var data = input.ReadBytes(4);
                var type = (VariantType)input.ReadValueU8();
                return new RawProperty(nameHash, data, type);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }

            public static void Write(Stream output, RawProperty instance, Endian endian)
            {
                output.WriteValueU32(instance.NameHash, endian);
                output.WriteBytes(instance.Data ?? (_DummyData ?? (_DummyData = new byte[4]))); // lol
                output.WriteValueU8((byte)instance.Type);
            }

            public override string ToString()
            {
                return string.Format("{0:X} {1}", this.NameHash, this.Type);
            }
        }
    }
}
