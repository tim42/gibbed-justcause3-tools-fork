using System;
using System.Globalization;
using System.IO;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Gibbed.JustCause3.ConvertTexture
{
    class Importer
    {
        public static void Import(string xmlFile, string outputBaseName)
        {
            var texture = new TextureFile();
            string ddscOutputFile = outputBaseName + ".ddsc";
            string hmddscOutputFile = outputBaseName + ".hmddsc";
            bool haveHMDDSCFile = false;
            byte[][] contents;

            // load the XML file
            using (var XmlInput = File.OpenRead(xmlFile))
            {
                var doc = new XPathDocument(XmlInput);
                var nav = doc.CreateNavigator();

                // create our three nodes
                var headerNode = nav.SelectSingleNode("/texture/header");
                var checkNode = nav.SelectSingleNode("/texture/check");
                var elementsNode = nav.SelectSingleNode("/texture/elements");
                var textureNode = nav.SelectSingleNode("/texture");

                // get the hmddsc file setting
                haveHMDDSCFile = bool.Parse(textureNode.GetAttribute("write-to-hmddsc", ""));

                // load the header
                texture.Unknown06 = byte.Parse(headerNode.GetAttribute("unknown-06", ""), NumberStyles.AllowHexSpecifier,  CultureInfo.InvariantCulture);
                texture.Unknown1C = uint.Parse(headerNode.GetAttribute("unknown-1C", ""), NumberStyles.AllowHexSpecifier,  CultureInfo.InvariantCulture);
                texture.Dimension = byte.Parse(headerNode.GetAttribute("dimension", ""), CultureInfo.InvariantCulture);
                texture.Depth = ushort.Parse(headerNode.GetAttribute("depth", ""), CultureInfo.InvariantCulture);
                texture.MipCount = byte.Parse(headerNode.GetAttribute("mip-count", ""), CultureInfo.InvariantCulture);
                texture.HeaderMipCount = byte.Parse(headerNode.GetAttribute("hdr-mip-count", ""), CultureInfo.InvariantCulture);
                texture.Flags = ushort.Parse(headerNode.GetAttribute("flags", ""), NumberStyles.AllowHexSpecifier,  CultureInfo.InvariantCulture);

                // load the check node (may be overriden or may cause the import to fail)
                texture.Width = ushort.Parse(checkNode.GetAttribute("width", ""), CultureInfo.InvariantCulture);
                texture.Height = ushort.Parse(checkNode.GetAttribute("height", ""), CultureInfo.InvariantCulture);
                texture.Format = ushort.Parse(checkNode.GetAttribute("format", ""), CultureInfo.InvariantCulture);

                // load the elementsNode (+ get the index)
                var elementList = elementsNode.Select("element");
                int i = 0;
                foreach (XPathNavigator elementNode in elementList)
                {
                    if (i >= texture.Elements.Length)
                        throw new IndexOutOfRangeException("there's too many <element> in the XML file");
                    texture.Elements[i] = new TextureFile.Element()
                    {
                        Offset = uint.Parse(elementNode.GetAttribute("offset", ""), CultureInfo.InvariantCulture),
                        Size = uint.Parse(elementNode.GetAttribute("size", ""), CultureInfo.InvariantCulture),
                        IsExternal = bool.Parse(elementNode.GetAttribute("external", "")),
                        Unknown8 = ushort.Parse(elementNode.GetAttribute("unknown8", ""), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
                        UnknownA = byte.Parse(elementNode.GetAttribute("unknownA", ""), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
                    };

                    ++i;
                }
            }

            uint biggestIndex = 0;
            uint biggestSize = 0;
            // The DDS file (look for the biggest one)
            for (uint i = 0; i < texture.Elements.Length; ++i)
            {
                if (texture.Elements[i].Size == 0)
                    continue;
                if ((haveHMDDSCFile && texture.Elements[i].IsExternal) || (texture.Elements[i].IsExternal == false)
                    && texture.Elements[i].Size > biggestSize)
                {
                    biggestSize = texture.Elements[i].Size;
                    biggestIndex = i;
                }
            }

            // create the first order array
            contents = new byte[texture.Elements.Length][];

            // Load the DDS file:
            string ddsFile = outputBaseName + biggestIndex.ToString() + ".dds";
            if (!ReadDDSFile(ddsFile, (int)biggestIndex, texture, contents, haveHMDDSCFile))
                throw new InvalidOperationException("Unable to load dds file: " + ddsFile);

            // Serialize the thing
            using (var output = File.Create(ddscOutputFile))
            {
                texture.Serialize(output);
                // write the content of the ddsc file:
                for (int i = 0; i < texture.Elements.Length; ++i)
                {
                    if (texture.Elements[i].IsExternal || texture.Elements[i].Size == 0) //only write internal elements
                        continue;
                    if (contents[i] == null)
                        continue;
                    output.Position = texture.Elements[i].Offset;
                    output.WriteBytes(contents[i]);
                }
            }

            if (haveHMDDSCFile)
            {
                using (var output = File.Create(hmddscOutputFile))
                {
                    // write the content of the hmddsc file:
                    for (int i = 0; i < texture.Elements.Length; ++i)
                    {
                        if (texture.Elements[i].IsExternal == false || texture.Elements[i].Size == 0) //only write external elements
                            continue;
                        if (contents[i] == null)
                            continue;
                        output.Position = texture.Elements[i].Offset;
                        output.WriteBytes(contents[i]);
                    }
                }
            }
        }

        private static bool ReadDDSFile(string ddsFile, int elementIndex, TextureFile texture, byte[][] contents, bool haveHMDDSCFile)
        {
            int rank = 0;
            for (uint i = 0; i < texture.Elements.Length; ++i)
            {
                if (i == elementIndex) continue;
                if (texture.Elements[i].Size > texture.Elements[elementIndex].Size)
                    ++rank;
            }

            byte[] ddsContents;
            using (var ddsInput = File.OpenRead(ddsFile))
            {
                uint magic = ddsInput.ReadValueU32();
                if (magic != 0x20534444)
                    throw new NotSupportedException(ddsFile + " is not a valid DDS file [magic does not match]");

                // load the DDS header
                var header = new Squish.DDS.Header();
                header.Deserialize(ddsInput, Endian.Little);

                // if any, load the DX10 header
                if (header.PixelFormat.FourCC == 0x30315844)
                {
                    uint format = ddsInput.ReadValueU32();
                    if (ddsInput.ReadValueU32() != 3)
                        throw new NotImplementedException("1D/3D texture not implementeds");
                    if (ddsInput.ReadValueU32() != 0)
                        throw new NotImplementedException();
                    if (ddsInput.ReadValueU32() != 1)
                        throw new NotImplementedException();
                    if (ddsInput.ReadValueU32() != 0)
                        throw new NotImplementedException();

                    if (format != texture.Format)
                    {
                        Console.WriteLine("FORMAT IS DIFFERENT FROM THE ORIGINAL FILE");
                        return false;
                    }
                    texture.Format = format;
                }

                // some checks
                if (header.Height << rank != texture.Height || header.Width << rank != texture.Width)
                {
                    Console.WriteLine("DIMENSIONS ARE DIFFERENTS FROM THE ORIGINAL FILE");
                    return false;
                }

                // load the content of the texture
                ddsContents = ddsInput.ReadBytes((int)(ddsInput.Length - ddsInput.Position));

                if (!haveHMDDSCFile) // the easy way
                    contents[elementIndex] = ddsContents;
                else // load the contents byte by byte
                {
                    // the list of elements, sorted by rank (biggest first)
                    List<int> rankIndexes = new List<int>();
                    for (int i = 0; i < texture.Elements.Length; ++i)
                    {
                        int insertIndex = 0;
                        for (; insertIndex < rankIndexes.Count; ++insertIndex)
                        {
                            if (texture.Elements[rankIndexes[insertIndex]].Size < texture.Elements[i].Size)
                                break;
                        }
                        rankIndexes.Insert(insertIndex, i);
                    }
                    // the for all those indexes, insert ranges of the dds file
                    uint currentOffset = 0;
                    foreach (int index in rankIndexes)
                    {
                        uint sz = texture.Elements[index].Size;
                        contents[index] = new byte[sz];
                        for (int i = 0; i < sz; ++i)
                            contents[index][i] = ddsContents[currentOffset + i];
                        currentOffset += sz;
                    }
                }

                if ((uint)(texture.Elements[elementIndex].Size/50) != (uint)(ddsContents.Length/50) && !haveHMDDSCFile)
                {
                    Console.WriteLine("SIZE IS DIFFERENT FROM THE ORIGINAL FILE ({0} vs {1})",
                                        ddsContents.Length, texture.Elements[elementIndex].Size);
                    return false;
                }
            }

            return true;
        }
    }
}
