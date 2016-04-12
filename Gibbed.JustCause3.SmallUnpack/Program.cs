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
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using NDesk.Options;

namespace Gibbed.JustCause3.SmallUnpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool verbose = false;
            string filterPattern = null;
            bool overwriteFiles = false;
            bool dontUseFullPaths = false;
            bool showHelp = false;

            var options = new OptionSet()
            {
                { "v|verbose", "be verbose (list files)", v => verbose = v != null },
                { "f|filter=", "only extract files using pattern", v => filterPattern = v },
                { "o|overwrite", "overwrite files if they already exist", v => overwriteFiles = v != null },
                { "nf|no-full-paths", "don't extract using full paths", v => dontUseFullPaths = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
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
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_sarc [output_directory]", GetExecutableName());
                Console.WriteLine("Unpack specified small archive.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extra[0];
            string outputPath = extra.Count > 1 ? extra[1] : Path.ChangeExtension(inputPath, null) + "_unpack";
            string tmpOutputPath = extra.Count > 1 ? extra[1] : Path.ChangeExtension(inputPath, ".sarc");
            Regex filter = null;
            if (string.IsNullOrEmpty(filterPattern) == false)
            {
                filter = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            using (var temp = File.OpenRead(inputPath))
            using (var cool = CreateCoolArchiveStream(temp))
            {
                //if (cool != null)
                //{
                //    using (var output = File.Create(tmpOutputPath))
                //    {
                //        var res = cool.ReadBytes((uint)cool.Length);
                //        output.WriteBytes(res);
                //        cool.Position = 0;
                //    }
                //}
                var input = cool ?? temp;

                var smallArchive = new SmallArchiveFile();
                smallArchive.Deserialize(input);

                long current = 0;
                long total = smallArchive.Entries.Count;
                var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                Directory.CreateDirectory(outputPath);

                var xmlPath = Path.Combine(outputPath, "@files.xml");
                var xmlSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };
                using (var xml = XmlWriter.Create(xmlPath, xmlSettings))
                {
                    xml.WriteStartDocument();
                    xml.WriteStartElement("files");

                    foreach (var entry in smallArchive.Entries)
                    {
                        current++;

                        if (string.IsNullOrEmpty(entry.Name) == true)
                        {
                            throw new InvalidOperationException();
                        }

                        var entryName = entry.Name;

                        //if (filter != null && filter.IsMatch(entryName) == false)
                        //{
                        //    continue;
                        //}

                        var entryPath = entryName;
                        if (entryPath[0] == '/' || entryPath[0] == '\\')
                        {
                            entryPath = entryPath.Substring(1);
                        }
                        entryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);

                        if (dontUseFullPaths == true)
                        {
                            entryPath = Path.GetFileName(entryPath);
                        }

                        entryPath = Path.Combine(outputPath, entryPath);
                        //if (overwriteFiles == false && File.Exists(entryName) == true)
                        //{
                        //    continue;
                        //}

                        if (verbose == true)
                        {
                            Console.WriteLine("[{0}/{1}] {2}",
                                              current.ToString(CultureInfo.InvariantCulture).PadRight(padding),
                                              total,
                                              entryName);
                        }

                        if (entry.Offset == 0)
                        {
                            xml.WriteStartElement("file");
                            xml.WriteStartAttribute("name");
                            xml.WriteValue(entryName);
                            xml.WriteEndAttribute();
                            xml.WriteStartAttribute("size");
                            xml.WriteValue(entry.Size);
                            xml.WriteEndAttribute();
                            xml.WriteEndElement();
                        }
                        else
                        {
                            xml.WriteStartElement("file");
                            xml.WriteStartAttribute("name");
                            xml.WriteValue(entryName);
                            xml.WriteEndAttribute();
                            xml.WriteValue(GetRelativePathForFile(xmlPath, entryPath));
                            xml.WriteEndElement();

                            var parentOutputPath = Path.GetDirectoryName(entryPath);
                            if (string.IsNullOrEmpty(parentOutputPath) == false)
                            {
                                Directory.CreateDirectory(parentOutputPath);
                            }

                            using (var output = File.Create(entryPath))
                            {
                                input.Seek(entry.Offset, SeekOrigin.Begin);
                                output.WriteFromStream(input, entry.Size);
                            }
                        }
                    }
                    Console.WriteLine("exported {0} files", current);
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                }
            }
        }

        private static string GetRelativePathForFile(string fromPath, string toPath)
        {
            var toName = Path.GetFileName(toPath);
            if (string.IsNullOrEmpty(toName) == true)
            {
                throw new ArgumentNullException("toPath");
            }

            fromPath = Path.GetDirectoryName(fromPath);
            toPath = Path.GetDirectoryName(toPath);
            var relativePath = GetRelativePath(fromPath, toPath);
            return Path.Combine(relativePath, toName);
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (fromPath == null)
            {
                throw new ArgumentNullException("fromPath");
            }

            if (toPath == null)
            {
                throw new ArgumentNullException("toPath");
            }

            Func<string, string, int> compare =
                (a, b) => string.Compare(a, b, CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase);

            if (Path.IsPathRooted(fromPath) == true && Path.IsPathRooted(toPath) == true)
            {
                if (compare(Path.GetPathRoot(fromPath), Path.GetPathRoot(toPath)) != 0)
                {
                    return toPath;
                }
            }

            var relativePath = new List<string>();
            var fromDirectories = fromPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var toDirectories = toPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            int length = Math.Min(fromDirectories.Length, toDirectories.Length);
            int lastCommonRoot = -1;

            // find common root
            for (int x = 0; x < length; x++)
            {
                if (compare(fromDirectories[x], toDirectories[x]) != 0)
                {
                    break;
                }
                lastCommonRoot = x;
            }

            if (lastCommonRoot < 0)
            {
                return toPath;
            }

            // add relative directories in from path
            for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
            {
                if (fromDirectories[x].Length > 0)
                {
                    relativePath.Add("..");
                }
            }

            // add directories to path
            for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
            {
                relativePath.Add(toDirectories[x]);
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
                               relativePath.ToArray());
        }

        private static Stream CreateCoolArchiveStream(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);
            var isCoolArchive = CoolArchiveFile.CheckHeader(input);
            input.Seek(0, SeekOrigin.Begin);

            if (isCoolArchive == false)
            {
                return null;
            }

            var archive = new CoolArchiveFile();
            archive.Deserialize(input);

            return new CoolStream(archive, input);
        }
    }
}
