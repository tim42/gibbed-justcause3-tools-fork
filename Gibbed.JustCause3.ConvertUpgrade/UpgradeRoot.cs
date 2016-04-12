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
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.ConvertUpgrade
{
    internal class UpgradeRoot
    {
        public const uint TypeHash = 0x95C89EC7;

        #region Fields
        private readonly List<Upgrade> _Upgrades;
        #endregion

        public UpgradeRoot()
        {
            this._Upgrades = new List<Upgrade>();
        }

        #region Properties
        public List<Upgrade> Upgrades
        {
            get { return this._Upgrades; }
        }
        #endregion

        public void Serialize(Stream output, Endian endian)
        {
            var basePosition = output.Position;

            output.Seek(RawUpgradeRoot.Size, SeekOrigin.Current);
            var upgradePosition = output.Position;

            output.Seek(RawUpgrade.Size * this._Upgrades.Count, SeekOrigin.Current);
            var rawUpgrades = new RawUpgrade[this._Upgrades.Count];
            for (int i = 0; i < this._Upgrades.Count; i++)
            {
                var upgrade = this._Upgrades[i];
                rawUpgrades[i] = new RawUpgrade()
                {
                    Name = upgrade.Name,
                    TaskGroup = upgrade.TaskGroup,
                    StarThreshold = upgrade.StarThreshold,
                    PrerequisiteCount = upgrade.Prerequisites == null ? 0 : upgrade.Prerequisites.Length,
                    Cost = upgrade.Cost,
                    Ability = upgrade.Ability,
                    UIName = upgrade.UIName,
                    UIDescription = upgrade.UIDescription,
                    UIDisplayOrder = upgrade.UIDisplayOrder,
                    UIImage = upgrade.UIImage,
                    UITreeName = upgrade.UITreeName,
                    UITreeOrder = upgrade.UITreeOrder,
                };
            }

            for (int i = 0; i < this._Upgrades.Count; i++)
            {
                var upgrade = this._Upgrades[i];
                if (upgrade.Prerequisites != null && upgrade.Prerequisites.Length > 0)
                {
                    rawUpgrades[i].PrerequisiteOffset = output.Position - basePosition;
                    foreach (var feat in upgrade.Prerequisites)
                    {
                        output.WriteValueU32(feat, endian);
                    }
                }
            }

            for (int i = 0; i < this._Upgrades.Count; i++)
            {
                var upgrade = this._Upgrades[i];

                if (upgrade.UIVideo != null)
                {
                    //output.Position = output.Position.Align(8);
                    rawUpgrades[i].UIVideoOffset = output.Position - basePosition;
                    output.WriteStringZ(upgrade.UIVideo, Encoding.ASCII);
                }
            }

            output.Position = upgradePosition;
            foreach (var rawUpgrade in rawUpgrades)
            {
                rawUpgrade.Write(output, endian);
            }

            output.Position = basePosition;
            new RawUpgradeRoot()
            {
                UpgradeOffset = upgradePosition,
                UpgradeCount = this._Upgrades.Count,
            }.Write(output, endian);
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var basePosition = input.Position;

            var rawUpgradeRoot = RawUpgradeRoot.Read(input, endian);

            var upgrades = new Upgrade[rawUpgradeRoot.UpgradeCount];
            if (rawUpgradeRoot.UpgradeCount != 0)
            {
                if (rawUpgradeRoot.UpgradeCount < 0 || rawUpgradeRoot.UpgradeCount > int.MaxValue)
                {
                    throw new FormatException();
                }

                var rawUpgrades = new RawUpgrade[rawUpgradeRoot.UpgradeCount];
                input.Position = basePosition + rawUpgradeRoot.UpgradeOffset;
                for (long i = 0; i < rawUpgradeRoot.UpgradeCount; i++)
                {
                    rawUpgrades[i] = RawUpgrade.Read(input, endian);
                }

                for (long i = 0; i < rawUpgradeRoot.UpgradeCount; i++)
                {
                    var rawUpgrade = rawUpgrades[i];
                    var upgrade = new Upgrade()
                    {
                        Name = rawUpgrade.Name,
                        TaskGroup = rawUpgrade.TaskGroup,
                        StarThreshold = rawUpgrade.StarThreshold,
                        Prerequisites = new uint[rawUpgrade.PrerequisiteCount],
                        Cost = rawUpgrade.Cost,
                        Ability = rawUpgrade.Ability,
                        UIName = rawUpgrade.UIName,
                        UIDescription = rawUpgrade.UIDescription,
                        UIDisplayOrder = rawUpgrade.UIDisplayOrder,
                        UIImage = rawUpgrade.UIImage,
                        UITreeName = rawUpgrade.UITreeName,
                        UITreeOrder = rawUpgrade.UITreeOrder,
                    };

                    if (rawUpgrade.PrerequisiteCount != 0)
                    {
                        if (rawUpgrade.PrerequisiteCount < 0 || rawUpgrade.PrerequisiteCount > int.MaxValue)
                        {
                            throw new FormatException();
                        }

                        input.Position = basePosition + rawUpgrade.PrerequisiteOffset;
                        for (long j = 0; j < rawUpgrade.PrerequisiteCount; j++)
                        {
                            upgrade.Prerequisites[j] = input.ReadValueU32(endian);
                        }
                    }

                    if (rawUpgrade.UIVideoOffset != 0)
                    {
                        input.Position = basePosition + rawUpgrade.UIVideoOffset;
                        upgrade.UIVideo = input.ReadStringZ(Encoding.ASCII);
                    }

                    upgrades[i] = upgrade;
                }
            }

            this._Upgrades.Clear();
            this._Upgrades.AddRange(upgrades);
        }

        private struct RawUpgradeRoot
        {
            public const int Size = 16;

            public long UpgradeOffset;
            public long UpgradeCount;

            public static RawUpgradeRoot Read(Stream input, Endian endian)
            {
                var instance = new RawUpgradeRoot();
                instance.UpgradeOffset = input.ReadValueS64(endian);
                instance.UpgradeCount = input.ReadValueS64(endian);
                return instance;
            }

            public static void Write(Stream output, RawUpgradeRoot instance, Endian endian)
            {
                output.WriteValueS64(instance.UpgradeOffset, endian);
                output.WriteValueS64(instance.UpgradeCount, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        private struct RawUpgrade
        {
            public const int Size = 72;

            public uint Name;
            public uint TaskGroup;
            public int StarThreshold;
            public long PrerequisiteOffset;
            public long PrerequisiteCount;
            public int Cost;
            public uint Ability;
            public uint UIName;
            public uint UIDescription;
            public int UIDisplayOrder;
            public int UIImage;
            public long UIVideoOffset;
            public uint UITreeName;
            public int UITreeOrder;

            public static RawUpgrade Read(Stream input, Endian endian)
            {
                var instance = new RawUpgrade();
                instance.Name = input.ReadValueU32(endian);
                instance.TaskGroup = input.ReadValueU32(endian);
                instance.StarThreshold = input.ReadValueS32(endian);
                input.Seek(4, SeekOrigin.Current);
                instance.PrerequisiteOffset = input.ReadValueS64(endian);
                instance.PrerequisiteCount = input.ReadValueS64(endian);
                instance.Cost = input.ReadValueS32(endian);
                instance.Ability = input.ReadValueU32(endian);
                instance.UIName = input.ReadValueU32(endian);
                instance.UIDescription = input.ReadValueU32(endian);
                instance.UIDisplayOrder = input.ReadValueS32(endian);
                instance.UIImage = input.ReadValueS32(endian);
                instance.UIVideoOffset = input.ReadValueS64(endian);
                instance.UITreeName = input.ReadValueU32(endian);
                instance.UITreeOrder = input.ReadValueS32(endian);
                return instance;
            }

            public static void Write(Stream output, RawUpgrade instance, Endian endian)
            {
                output.WriteValueU32(instance.Name, endian);
                output.WriteValueU32(instance.TaskGroup, endian);
                output.WriteValueS32(instance.StarThreshold, endian);
                output.Seek(4, SeekOrigin.Current);
                output.WriteValueS64(instance.PrerequisiteOffset, endian);
                output.WriteValueS64(instance.PrerequisiteCount, endian);
                output.WriteValueS32(instance.Cost, endian);
                output.WriteValueU32(instance.Ability, endian);
                output.WriteValueU32(instance.UIName, endian);
                output.WriteValueU32(instance.UIDescription, endian);
                output.WriteValueS32(instance.UIDisplayOrder, endian);
                output.WriteValueS32(instance.UIImage, endian);
                output.WriteValueS64(instance.UIVideoOffset, endian);
                output.WriteValueU32(instance.UITreeName, endian);
                output.WriteValueS32(instance.UITreeOrder, endian);
            }

            public void Write(Stream output, Endian endian)
            {
                Write(output, this, endian);
            }
        }

        public struct Upgrade
        {
            public uint Name;
            public uint TaskGroup;
            public int StarThreshold;
            public uint[] Prerequisites;
            public int Cost;
            public uint Ability;
            public uint UIName;
            public uint UIDescription;
            public int UIDisplayOrder;
            public int UIImage;
            public string UIVideo;
            public uint UITreeName;
            public int UITreeOrder;
        }
    }
}
