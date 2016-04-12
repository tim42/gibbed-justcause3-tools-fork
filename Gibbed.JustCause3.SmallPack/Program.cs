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
using System.Linq;
using System.Xml.XPath;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using NDesk.Options;

namespace Gibbed.JustCause3.SmallPack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void SetOption<T>(string s, ref T variable, T value)
        {
            if (s == null)
            {
                return;
            }

            variable = value;
        }

        private struct PendingEntry
        {
            public string Name;
            public uint? Size;
            public string Path;
        }

        public static void Main(string[] args)
        {
            var endian = Endian.Little;
            bool verbose = false;
            bool compress = false;
            bool showHelp = false;

            var options = new OptionSet
            {
                { "v|verbose", "be verbose (list files)", v => verbose = v != null },
                { "l|little-endian", "write in little endian mode", v => SetOption(v, ref endian, Endian.Little) },
                { "b|big-endian", "write in big endian mode", v => SetOption(v, ref endian, Endian.Big) },
                { "c|compress", "compress small archive with zlib.", v => compress = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null }
            };

            List<string> extra;

            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extra.Count < 1 || extra.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_directory [output_sarc]", GetExecutableName());
                Console.WriteLine("Pack specified directory.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPath = Path.GetFullPath(extra[0]);

            if (Directory.Exists(inputPath)) // create the AAF file from a folder
            {
                string xmlPath;

                if (Directory.Exists(inputPath) == true)
                {
                    xmlPath = Path.Combine(inputPath, "@files.xml");
                }
                else
                {
                    xmlPath = inputPath;
                    inputPath = Path.GetDirectoryName(inputPath);
                }
                var outputPath = extra.Count > 1 ? extra[1] : inputPath + ".aaf";

                var pendingEntries = new List<PendingEntry>();
                using (var xml = File.OpenRead(xmlPath))
                {
                    var doc = new XPathDocument(xml);
                    var nav = doc.CreateNavigator();
                    var root = nav.SelectSingleNode("/files");

                    var rawFiles = root.Select("file");
                    foreach (XPathNavigator rawFile in rawFiles)
                    {
                        if (rawFile.MoveToAttribute("name", "") == false)
                        {
                            throw new FormatException();
                        }
                        var entryName = rawFile.Value;
                        rawFile.MoveToParent();

                        if (rawFile.MoveToAttribute("size", "") == true)
                        {
                            uint entrySize;
                            if (uint.TryParse(rawFile.Value, out entrySize) == false)
                            {
                                throw new FormatException();
                            }

                            pendingEntries.Add(new PendingEntry()
                            {
                                Name = entryName,
                                Size = entrySize,
                            });
                            rawFile.MoveToParent();
                            continue;
                        }

                        string entryPath;
                        if (Path.IsPathRooted(rawFile.Value) == false)
                        {
                            entryPath = Path.Combine(inputPath, rawFile.Value);
                        }
                        else
                        {
                            entryPath = rawFile.Value;
                        }

                        pendingEntries.Add(new PendingEntry()
                        {
                            Name = entryName,
                            Path = entryPath,
                        });
                    }
                }

                using (var aafOutput = File.Create(outputPath))
                using (var output = new MemoryStream())
                {
                    var headerSize = SmallArchiveFile.EstimateHeaderSize(pendingEntries.Select(pe => pe.Name));

                    var smallArchive = new SmallArchiveFile();

                    output.Position = headerSize;
                    foreach (var pendingEntry in pendingEntries)
                    {
                        if (pendingEntry.Size != null)
                        {
                            smallArchive.Entries.Add(new SmallArchiveFile.Entry(pendingEntry.Name,
                                                                                0,
                                                                                pendingEntry.Size.Value));
                            continue;
                        }

                        using (var input = File.OpenRead(pendingEntry.Path))
                        {
                            output.Position = output.Position.Align(4);
                            smallArchive.Entries.Add(new SmallArchiveFile.Entry(pendingEntry.Name,
                                                                                (uint)output.Position,
                                                                                (uint)input.Length));
                            output.WriteFromStream(input, input.Length);
                        }
                    }

                    output.Position = 0;
                    smallArchive.Endian = endian;
                    smallArchive.Serialize(output);

                    // create the AAF file
                    CoolArchiveFile cool = new CoolArchiveFile();

                    // infos
                    cool.BlockSize = (uint)output.Length;
                    cool.Endian = Endian.Little;

                    // create a single chunk
                    output.Position = 0;
                    cool.ChunkInfos.Add(new CoolArchiveFile.ChunkInfo(output.ReadBytes((uint)output.Length)));

                    // serialize
                    cool.Serialize(aafOutput);
                }
            }
            else // input file IS SARC
            {
                Console.WriteLine("Will create a cool archive");
                var outputPath = extra.Count == 2 ? extra[1] : Path.ChangeExtension(inputPath, "aaf");
                using (var input = File.OpenRead(inputPath))
                using (var outputFile = File.Create(outputPath))
                {
                    CoolArchiveFile cool = new CoolArchiveFile();

                    // infos
                    cool.BlockSize = (uint)input.Length;
                    cool.Endian = Endian.Little;

                    // create a single chunk
                    input.Position = 0;
                    cool.ChunkInfos.Add(new CoolArchiveFile.ChunkInfo(input.ReadBytes((uint)input.Length)));

                    // serialize
                    cool.Serialize(outputFile);
                }
            }
        }
    }
}
