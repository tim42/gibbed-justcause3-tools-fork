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

namespace Gibbed.JustCause3.PropertyFormats
{
    public class Node
    {
        private uint _NameHash;
        private readonly List<Node> _Children;
        private readonly Dictionary<uint, IVariant> _Properties;
        internal uint DataOffset;

        public Node()
        {
            this._Children = new List<Node>();
            this._Properties = new Dictionary<uint, IVariant>();
        }

        public uint NameHash
        {
            get { return this._NameHash; }
            set { this._NameHash = value; }
        }

        public List<Node> Children
        {
            get { return this._Children; }
        }

        public Dictionary<uint, IVariant> Properties
        {
            get { return this._Properties; }
        }

        public override string ToString()
        {
            return this._NameHash.ToString("X8");
        }
    }
}
