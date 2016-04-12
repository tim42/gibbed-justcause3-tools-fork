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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gibbed.JustCause3.ConvertUpgrade
{
    internal class UpgradeRootJson
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Upgrade
        {
            #region Fields
            private string _Name;
            private string _TaskGroup;
            private int _StarThreshold;
            private readonly List<string> _Prerequisites;
            private int _Cost;
            private string _Ability;
            private string _UIName;
            private string _UIDescription;
            private int _UIDisplayOrder;
            private int _UIImage;
            private string _UIVideo;
            private string _UITreeName;
            private int _UITreeOrder;
            #endregion

            public Upgrade()
            {
                this._Prerequisites = new List<string>();
            }

            #region Properties
            [JsonProperty("name")]
            public string Name
            {
                get { return this._Name; }
                set { this._Name = value; }
            }

            [JsonProperty("task_group")]
            public string TaskGroup
            {
                get { return this._TaskGroup; }
                set { this._TaskGroup = value; }
            }

            [JsonProperty("star_threshold")]
            public int StarThreshold
            {
                get { return this._StarThreshold; }
                set { this._StarThreshold = value; }
            }

            [JsonProperty("prerequisites")]
            public List<string> Prerequisites
            {
                get { return this._Prerequisites; }
            }

            [JsonProperty("cost")]
            public int Cost
            {
                get { return this._Cost; }
                set { this._Cost = value; }
            }

            [JsonProperty("ability")]
            public string Ability
            {
                get { return this._Ability; }
                set { this._Ability = value; }
            }

            [JsonProperty("ui_name")]
            public string UIName
            {
                get { return this._UIName; }
                set { this._UIName = value; }
            }

            [JsonProperty("ui_description")]
            public string UIDescription
            {
                get { return this._UIDescription; }
                set { this._UIDescription = value; }
            }

            [JsonProperty("ui_display_order")]
            public int UIDisplayOrder
            {
                get { return this._UIDisplayOrder; }
                set { this._UIDisplayOrder = value; }
            }

            [JsonProperty("ui_image")]
            public int UIImage
            {
                get { return this._UIImage; }
                set { this._UIImage = value; }
            }

            [JsonProperty("ui_video")]
            public string UIVideo
            {
                get { return this._UIVideo; }
                set { this._UIVideo = value; }
            }

            [JsonProperty("ui_tree_name")]
            public string UITreeName
            {
                get { return this._UITreeName; }
                set { this._UITreeName = value; }
            }

            [JsonProperty("ui_tree_order")]
            public int UITreeOrder
            {
                get { return this._UITreeOrder; }
                set { this._UITreeOrder = value; }
            }
            #endregion
        }
    }
}
