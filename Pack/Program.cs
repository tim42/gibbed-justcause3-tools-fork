using System;
using System.IO;
using System.Collections.Generic;
using Gibbed.JustCause3.FileFormats;
using Gibbed.IO;
using NDesk.Options;

namespace Gibbed.JustCause3.Pack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            bool overwrite = false;
            bool showHelp = false;
            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwrite = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };
            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count != 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ game[0-9]+_unpack", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string path = Path.GetFullPath(extras[0]);
            string base_name = Path.GetFileName(path).Replace("_unpack", "");
            string base_path = Path.GetDirectoryName(path);
            string tabFile = Path.Combine(base_path, base_name + ".tab");
            string arcFile = Path.Combine(base_path, base_name + ".arc");

            if (File.Exists(arcFile) && !overwrite)
            {
                Console.Write("Refusing to do this: it will overwrite {0}", Path.GetFileName(arcFile));
                return;
            }

            //if (File.Exists(arcFile)) // TODO: add cli options for this
            //{
            //    Console.WriteLine("WARNING: {0} in {1} ALREADY EXISTS", Path.GetFileName(arcFile), base_path);
            //    Console.Write("WOULD YOU LIKE TO OVERWRITE {0} ? [y/N]: ", Path.GetFileName(arcFile));
            //    char c = char.ToLower((char)Console.Read());
            //    if (c != 'y')
            //        return;
            //    System.Threading.Thread.Sleep(2000);
            //}

            Console.WriteLine("Will generate {0} and {1}", Path.GetFileName(tabFile), Path.GetFileName(arcFile));

            var fileList = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            Uri baseUri = new Uri(path + Path.DirectorySeparatorChar);
            ArchiveTableFile tabData = new ArchiveTableFile();
            var arcStream = File.Create(arcFile);

            Console.WriteLine("Found {0} files", fileList.Length);
            Console.WriteLine("Writing the ARC file");
            uint count = 0;
            uint skipCount = 0;
            SortedSet<uint> hashSet = new SortedSet<uint>();

            foreach (var file in fileList)
            {
                if (count > 0 && count % 300 == 0)
                    Console.WriteLine("[{0:F1} %]", (float)count * 100 / (float)fileList.Length);
                ++count;

                Uri fileUri = new Uri(file);
                string relPath = baseUri.MakeRelativeUri(fileUri).ToString();
                string firstSegment = relPath.Split('/')[0];

                switch (Path.GetExtension(file))
                {
                    case ".xml":
                    case ".XML":
                    case ".dll":
                    case ".DLL":
                    case ".exe":
                    case ".EXE":
                        {
                            Console.WriteLine("Skipping {0} [that's a {1} file]", relPath, Path.GetExtension(file));
                            ++skipCount;
                            continue;
                        }
                }

                uint hash;
                if (firstSegment == "__UNKNOWN")
                {
                    try
                    {
                        hash = uint.Parse(Path.GetFileNameWithoutExtension(file), System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    catch
                    {
                        Console.WriteLine("Skipping {0} [wrong name: not a hash]", relPath);
                        ++skipCount;
                        continue;
                    }
                }
                else
                    hash = relPath.HashJenkins();

                if (hashSet.Contains(hash))
                {
                    Console.WriteLine("Skipping {0} [HASH COLLISION FOUND]", relPath);
                    ++skipCount;
                    continue;
                }
                hashSet.Add(hash);
                uint position = (uint)arcStream.Position;
                uint size;
                // Copy the file contents into the .arc file
                using (var fileStream = File.OpenRead(file))
                {
                    size = (uint)fileStream.Length;
                    arcStream.WriteFromStream(fileStream, fileStream.Length);
                }
                // Create the tab entry
                tabData.Entries.Add(new ArchiveTableFile.EntryInfo(hash, position, size));

                // Align the next entry (pad with '0' (0x30), like avalanche do)
                while ((arcStream.Position % tabData.Alignment) != 0)
                    arcStream.WriteByte(0x30);
            }
            arcStream.Close();
            Console.WriteLine("Skipped {0}/{1} files", skipCount, count);

            // write the tab file
            var tabStream = File.Create(tabFile);
            tabData.Serialize(tabStream);

            System.Threading.Thread.Sleep(3000);
        }
    }
}
