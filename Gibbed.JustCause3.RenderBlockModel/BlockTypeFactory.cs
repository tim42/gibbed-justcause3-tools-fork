/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
using System.Linq;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.RenderBlockModel
{
    public static class BlockTypeFactory
    {
        private static Dictionary<uint, string> _HashesToNames;
        private static Dictionary<uint, Type> _HashesToTypes;

        private static void Register<TBlockType>(string name)
            where TBlockType : IRenderBlock
        {
            var hash = name.HashJenkins();
            _HashesToTypes.Add(hash, typeof(TBlockType));
        }

        public static IRenderBlock Create(string type)
        {
            return Create(type.HashJenkins());
        }

        public static string GetName(uint type)
        {
            if (_HashesToNames.ContainsKey(type) == false)
            {
                return null;
            }

            return _HashesToNames[type];
        }

        public static IRenderBlock Create(uint type)
        {
            if (_HashesToTypes.ContainsKey(type) == false)
            {
                return null;
            }

            return Activator.CreateInstance(_HashesToTypes[type]) as IRenderBlock;
        }

        static BlockTypeFactory()
        {
            _HashesToNames = _Names
                .Select(n => new KeyValuePair<uint, string>(n.HashJenkins(), n))
                .ToDictionary(i => i.Key,
                              i => i.Value);
            _HashesToTypes = new Dictionary<uint, Type>();

            Register<Blocks.CarPaint>("CarPaint");
            Register<Blocks.CarPaintSimple>("CarPaintSimple");
            Register<Blocks.DeformableWindow>("DeformableWindow");
            Register<Blocks.General>("General");
            Register<Blocks.Lambert>("Lambert");
            Register<Blocks.SkinnedGeneral>("SkinnedGeneral");
        }

        private static readonly string[] _Names = new[]
        {
            "2DTex1",
            "2DTex2",
            "3DText",
            "AOBox",
            "Beam",
            "BillboardFoliage",
            "Box",
            "Bullet",
            "CarPaint",
            "CarPaintSimple",
            "CirrusClouds",
            "Clouds",
            "Creatures",
            "DecalDeformable",
            "DecalSimple",
            "DecalSkinned",
            "DeformableWindow",
            "Facade",
            "Flag",
            "FogGradient",
            "Font",
            "General",
            "Grass",
            "GuiAnark",
            "Halo",
            "Lambert",
            "Leaves",
            "Lights",
            "Line",
            "Merged",
            "NvWaterHighEnd",
            "Occluder",
            "Open",
            "Particle",
            "Skidmarks",
            "SkinnedGeneral",
            "SkyGradient",
            "SoftClouds",
            "SplineRoad",
            "Stars",
            "Terrain",
            "TerrainForest",
            "TerrainForestFin",
            "TreeImpostorTrunk",
            "TreeImpostorTop",
            "Triangle",
            "VegetationBark",
            "VegetationFoliage",
            "WaterGodrays",
            "WaterHighEnd",
            "WaterWaves",
            "Weather",
            "Window",
        };
    }
}
