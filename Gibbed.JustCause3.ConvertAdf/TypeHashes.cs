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

namespace Gibbed.JustCause3.ConvertAdf
{
    internal static class TypeHashes
    {
        public static class Primitive
        {
            public const uint Int8 = 0x580D0A62; // int8011
            public const uint UInt8 = 0x0CA2821D; // uint8011
            public const uint Int16 = 0xD13FCF93; // int16022
            public const uint UInt16 = 0x86D152BD; // uint16022
            public const uint Int32 = 0x192FE633; // int32044
            public const uint UInt32 = 0x075E4E4F; // uint32044
            public const uint Int64 = 0xAF41354F; // int64088
            public const uint UInt64 = 0xA139E01F; // uint64088
            public const uint Float = 0x7515A207; // float044
            public const uint Double = 0xC609F663; // double088
        }

        public const uint String = 0x8955583E; // string588
    }
}
