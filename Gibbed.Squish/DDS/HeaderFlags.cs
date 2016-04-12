/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.Squish.DDS
{
    [Flags]
    public enum HeaderFlags : uint
    {
        Texture = 0x00001007,	// DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT 
        Mipmap = 0x00020000,	// DDSD_MIPMAPCOUNT
        Volume = 0x00800000,	// DDSD_DEPTH
        Pitch = 0x00000008,	// DDSD_PITCH
        LinerSize = 0x00080000,	// DDSD_LINEARSIZE
    }
}
