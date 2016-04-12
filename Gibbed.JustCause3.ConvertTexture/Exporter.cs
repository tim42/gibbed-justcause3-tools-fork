using System;
using System.Globalization;
using System.IO;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using System.Xml;

namespace Gibbed.JustCause3.ConvertTexture
{
    class Exporter
    {
        public static void Export(string ddscTextureFile, string outputBaseName)
        {
            var texture = new TextureFile();
            string xmlOutFile = outputBaseName + ".xml";
            string hmddscFile = outputBaseName + ".hmddsc";
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                CheckCharacters = false,
            };

            bool haveHMDDSCFile = File.Exists(hmddscFile);
            using (var input = File.OpenRead(ddscTextureFile))
            {
                texture.Deserialize(input);

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

                if (texture.Elements[biggestIndex].IsExternal == false)
                    SaveDDSFile(outputBaseName, biggestIndex, texture, input); // load internal texture
                else
                    SaveDDSFile(outputBaseName, biggestIndex, texture, null); // load external texture (from hmddsc file)
            }


            // the XML metadata
            using (var xmlOutput = File.Create(xmlOutFile))
            using (var writer = XmlWriter.Create(xmlOutput, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("write-to-hmddsc", haveHMDDSCFile.ToString(CultureInfo.InvariantCulture));

                writer.WriteStartElement("header");
                writer.WriteAttributeString("unknown-06", texture.Unknown06.ToString("X8"));
                writer.WriteAttributeString("unknown-1C", texture.Unknown1C.ToString("X8"));
                writer.WriteAttributeString("dimension", texture.Dimension.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("depth", texture.Depth.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("mip-count", texture.MipCount.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("hdr-mip-count", texture.HeaderMipCount.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("flags", texture.Flags.ToString("X8"));
                writer.WriteEndElement();

                // Will emit a warning (or sometime fail) if not the same
                writer.WriteStartElement("check");
                writer.WriteAttributeString("width", texture.Height.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("height", texture.Width.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("format", texture.Format.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndElement();

                writer.WriteStartElement("elements");
                foreach (var element in texture.Elements)
                {
                    writer.WriteStartElement("element");
                    writer.WriteAttributeString("offset", element.Offset.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("size", element.Size.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("external", element.IsExternal.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("unknown8", element.Unknown8.ToString("X8"));
                    writer.WriteAttributeString("unknownA", element.UnknownA.ToString("X8"));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private static bool SaveDDSFile(string outputBaseName, uint elementIndex, TextureFile texture, Stream ddscFile)
        {
            string hmddscFile = outputBaseName + ".hmddsc";
            string fileName = outputBaseName + elementIndex.ToString() + ".dds";
            bool doClose = false;
            if (ddscFile == null)
            {
                doClose = true;
                if (!File.Exists(hmddscFile))
                    return false;
                ddscFile = File.OpenRead(hmddscFile);
            }

            int rank = 0;
            for (uint i = 0; i < texture.Elements.Length; ++i)
            {
                if (i == elementIndex) continue;
                if (texture.Elements[i].Size > texture.Elements[elementIndex].Size)
                    ++rank;
            }

            // create the DDS the header
            var header = new Squish.DDS.Header()
            {
                Size = 124,
                Flags = Squish.DDS.HeaderFlags.Texture | Squish.DDS.HeaderFlags.Mipmap,
                Width = texture.Width >> rank,
                Height = texture.Height >> rank,
                PitchOrLinearSize = 0,
                Depth = texture.Depth,
                MipMapCount = 1, // always 1
                PixelFormat = GetPixelFormat(texture),
                SurfaceFlags = 8 | 0x1000,
                CubemapFlags = 0,
            };

            using (var output = File.Create(fileName))
            {
                // write the DDS header
                var endian = Endian.Little;
                output.WriteValueU32(0x20534444, endian);
                header.Serialize(output, endian);

                // DX10 header
                if (header.PixelFormat.FourCC == 0x30315844)
                {
                    output.WriteValueU32(texture.Format, endian);
                    output.WriteValueU32(3, endian); // was 2. should be 3 as we most likely will export 2D textures
                    output.WriteValueU32(0, endian);
                    output.WriteValueU32(1, endian);
                    output.WriteValueU32(0, endian);
                }

                // body
                ddscFile.Position = texture.Elements[elementIndex].Offset;
                output.WriteFromStream(ddscFile, texture.Elements[elementIndex].Size);
            }
            if (doClose)
                ddscFile.Close();
            return true;
        }

        private static Squish.DDS.PixelFormat GetPixelFormat(TextureFile texture)
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/bb173059.aspx "DXGI_FORMAT enumeration"
            // https://msdn.microsoft.com/en-us/library/windows/desktop/cc308051.aspx "Legacy Formats: Map Direct3D 9 Formats to Direct3D 10"

            switch (texture.Format)
            {
                case 71: // DXGI_FORMAT_BC1_UNORM
                    {
                        var pixelFormat = new Squish.DDS.PixelFormat();
                        pixelFormat.Initialise(Squish.DDS.FileFormat.DXT1);
                        return pixelFormat;
                    }

                case 74: // DXGI_FORMAT_BC2_UNORM
                    {
                        var pixelFormat = new Squish.DDS.PixelFormat();
                        pixelFormat.Initialise(Squish.DDS.FileFormat.DXT3);
                        return pixelFormat;
                    }

                case 77: // DXGI_FORMAT_BC3_UNORM
                    {
                        var pixelFormat = new Squish.DDS.PixelFormat();
                        pixelFormat.Initialise(Squish.DDS.FileFormat.DXT5);
                        return pixelFormat;
                    }
                case 87: // DXGI_FORMAT_B8G8R8A8_UNORM
                    {
                        var pixelFormat = new Squish.DDS.PixelFormat();
                        pixelFormat.Initialise(Squish.DDS.FileFormat.A8R8G8B8);
                        return pixelFormat;
                    }

                case 61: // DXGI_FORMAT_R8_UNORM
                case 80: // DXGI_FORMAT_BC4_UNORM
                case 83: // DXGI_FORMAT_BC5_UNORM
                case 98: // DXGI_FORMAT_BC7_UNORM
                    {
                        var pixelFormat = new Squish.DDS.PixelFormat();
                        pixelFormat.Size = pixelFormat.GetSize();
                        pixelFormat.FourCC = 0x30315844; // 'DX10'
                        return pixelFormat;
                    }
            }

            throw new NotSupportedException();
        }
    }
}
