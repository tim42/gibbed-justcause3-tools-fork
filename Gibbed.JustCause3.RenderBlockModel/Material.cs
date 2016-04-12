using System;
using Gibbed.IO;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.RenderBlockModel
{
    public struct Material : IFormat
    {
        public string UndeformedDiffuseTexture;
        public string UndeformedNormalMap;
        public string UndeformedPropertiesMap;
        public string DeformedDiffuseTexture;
        public string DeformedNormalMap;
        public string DeformedPropertiesMap;
        public string NormalMapEx3;
        public string ShadowMapTexture;
        public uint Unknown8;

        public void Serialize(Stream output, Endian endian)
        {
            output.WriteStringU32(this.UndeformedDiffuseTexture, endian);
            output.WriteStringU32(this.UndeformedNormalMap, endian);
            output.WriteStringU32(this.UndeformedPropertiesMap, endian);
            output.WriteStringU32(this.DeformedDiffuseTexture, endian);
            output.WriteStringU32(this.DeformedNormalMap, endian);
            output.WriteStringU32(this.DeformedPropertiesMap, endian);
            output.WriteStringU32(this.NormalMapEx3, endian);
            output.WriteStringU32(this.ShadowMapTexture, endian);
            output.WriteValueU32(this.Unknown8, endian);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.UndeformedDiffuseTexture = input.ReadStringU32(endian);
            this.UndeformedNormalMap = input.ReadStringU32(endian);
            this.UndeformedPropertiesMap = input.ReadStringU32(endian);
            this.DeformedDiffuseTexture = input.ReadStringU32(endian);
            this.DeformedNormalMap = input.ReadStringU32(endian);
            this.DeformedPropertiesMap = input.ReadStringU32(endian);
            this.NormalMapEx3 = input.ReadStringU32(endian);
            this.ShadowMapTexture = input.ReadStringU32(endian);
            this.Unknown8 = input.ReadValueU32(endian);
        }
    }
}
