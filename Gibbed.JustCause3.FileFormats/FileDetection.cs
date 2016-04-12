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

namespace Gibbed.JustCause3.FileFormats
{
    public static class FileDetection
    {
        private struct FileTypeInfo
        {
            public readonly string Name;
            public readonly string Extension;

            public FileTypeInfo(string name, string extension)
            {
                this.Name = name;
                this.Extension = extension;
            }
        }

        private static readonly Dictionary<uint, FileTypeInfo> _Simple4Lookup =
            new Dictionary<uint, FileTypeInfo>()
            {
                { 0x20534444, new FileTypeInfo("texture", "dds") },
                { 0x58545641, new FileTypeInfo("texture", "ddsc") },
                { 0x41444620, new FileTypeInfo("arbitrary data format", "adf") },
                { 0x43505452, new FileTypeInfo("runtime property container?", "rtpc") },
                { 0x57E0E057, new FileTypeInfo("animation", "ban") },
                { 0x35425346, new FileTypeInfo("audio", "fsb5") },
                { 0x00464141, new FileTypeInfo("archive", "aaf") },
                { 0x6932424B, new FileTypeInfo("video", "bink") },
                { 0x46464952, new FileTypeInfo("audio", "riff") },
                { 0x00424154, new FileTypeInfo("tab", "tab") }
            };

        private static readonly Dictionary<ulong, FileTypeInfo> _Simple8Lookup =
            new Dictionary<ulong, FileTypeInfo>()
            {
                { 0x000000300000000EUL, new FileTypeInfo("ai", "btc") },
                { 0x444E425200000005UL, new FileTypeInfo("RBN", "rbn") },
                { 0x4453425200000005UL, new FileTypeInfo("RBS", "rbs") },
                { 0x444D425200000005UL, new FileTypeInfo("RBM", "rbm") }, // the magic seens to be more that 8 bit though
            };

        public static string Detect(byte[] guess, int read)
        {
            if (read == 0)
            {
                return "null";
            }

            if (read >= 4)
            {
                var magic = BitConverter.ToUInt32(guess, 0);
                if (_Simple4Lookup.ContainsKey(magic) == true)
                {
                    return _Simple4Lookup[magic].Extension;
                }
            }

            if (read >= 8)
            {
                var magic = BitConverter.ToUInt64(guess, 0);
                if (_Simple8Lookup.ContainsKey(magic) == true)
                {
                    return _Simple8Lookup[magic].Extension;
                }
            }

            if (read >= 3)
            {
                if (guess[0] == 1 &&
                    guess[1] == 4 &&
                    guess[2] == 0)
                {
                    return "bin";
                }
            }

            // some FSB5 files have the "FSB5" (0x35425346) magic at offset 0x10
            if (read >= 16 + 4)
            {
                uint magic = BitConverter.ToUInt32(guess, 16);
                if (0x35425346 == magic)
                    return "fsb5";
            }
            return "unknown";
        }
    }
}
