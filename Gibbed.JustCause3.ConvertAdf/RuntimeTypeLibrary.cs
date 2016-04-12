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

namespace Gibbed.JustCause3.ConvertAdf
{
    internal class RuntimeTypeLibrary
    {
        private readonly Dictionary<uint, FileFormats.AdfFile.TypeDefinition> _TypeDefinitions;

        public RuntimeTypeLibrary()
        {
            this._TypeDefinitions = new Dictionary<uint, FileFormats.AdfFile.TypeDefinition>();
        }

        public FileFormats.AdfFile.TypeDefinition GetTypeDefinition(uint nameHash)
        {
            if (this._TypeDefinitions.ContainsKey(nameHash) == false)
            {
                throw new InvalidOperationException(string.Format("type definition for {0:X} not found", nameHash));
            }

            return this._TypeDefinitions[nameHash];
        }

        public void AddTypeDefinitions(FileFormats.AdfFile adf)
        {
            foreach (var typeDefinition in adf.TypeDefinitions)
            {
                if (this._TypeDefinitions.ContainsKey(typeDefinition.NameHash) == true)
                {
                    continue;
                    throw new InvalidOperationException();
                }

                this._TypeDefinitions.Add(typeDefinition.NameHash, typeDefinition);
            }
        }
    }
}
