using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Gibbed.IO;
using Gibbed.ProjectData;
using Gibbed.JustCause3.PropertyFormats;
using Gibbed.JustCause3.FileFormats;

namespace QueryHashList
{
    class Program
    {
        static void Assert(bool condition, string mesg)
        {
            if (condition)
                return;
            Console.WriteLine();
            Console.WriteLine(mesg);
            Console.WriteLine();
            Environment.Exit(1);
        }

        private static HashList<uint> _Names;
        private static HashList<uint> _Files;

        private static List<KeyValuePair<string, HashSet<uint>>> _HashFiles =
                        new List<KeyValuePair<string, HashSet<uint>>>();

        private static Dictionary<uint, List<uint>> _HashFileDict = new Dictionary<uint, List<uint>>();
        private static Dictionary<uint, string> _FileHashDict = new Dictionary<uint, string>();

        static void Main(string[] args)
        {
            Console.BufferWidth = 250;

            var manager = Manager.Load(); // load the current project
            var project = manager.ActiveProject;

            Assert(project != null, "Not active project selected");

            var listsPath = project.ListsPath;
            Assert(listsPath != null, "Could not detect lists path.");

            // load the hash->file dictionary
            string hashFilePath = Path.Combine(listsPath, "generated.hashfiledict");

            if (!File.Exists(hashFilePath))
                Console.WriteLine("WARNING: Cross results are disabled. Please run GenerateNameList to fix that.");
            else
            {
                Console.WriteLine("Loading cross result file...");
                uint conflicts = 0;
                int max = 0;
                using (var input = File.OpenRead(hashFilePath))
                {
                    uint hcount = input.ReadValueU32();
                    for (uint i = 0; i < hcount; ++i)
                    {
                        uint hash = input.ReadValueU32();
                        uint count = input.ReadValueU32();
                        var list = new List<uint>();
                        for (uint j = 0; j < count; ++j)
                            list.Add(input.ReadValueU32());
                        if (list.Count > max)
                            max = list.Count;
                        _HashFileDict.Add(hash, list);
                    }

                    uint scount = input.ReadValueU32();
                    for (uint i = 0; i < scount; ++i)
                    {
                        string s = input.ReadStringU32(Endian.Little);
                        uint h = s.HashJenkins();
                        if (!_FileHashDict.ContainsKey(h))
                            _FileHashDict.Add(h, s);
                        else
                            ++conflicts;
                    }
                }
                if (conflicts != 0)
                    Console.WriteLine("WARNING: Found {0} conflicts in the cross result file");
                if (max + 20 > Console.WindowHeight)
                    Console.BufferHeight = max + 20;
            }

            // load the hashlists
            var hashListFileList = Directory.GetFiles(listsPath, "*.hashlist", SearchOption.AllDirectories);
            foreach (string file in hashListFileList)
            {
                HashSet<uint> hashlist = new HashSet<uint>();
                using (var input = File.OpenRead(file))
                {
                    long length = input.Length;
                    for (long i = 0; i < length; i += 4)
                        hashlist.Add(input.ReadValueU32(Endian.Little));
                }
                var pair = new KeyValuePair<string, HashSet<uint>>(file, hashlist);
                _HashFiles.Add(pair);
            }

            // ask the project to load the namelist
            _Names = manager.LoadPropertyNames();
            _Files = manager.LoadFileLists(null);

            // everything is loaded, run the interactive part
            while (true)
            {
                Console.Write("HASH> ");
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    continue;

                uint hash = 0;
                bool printFileList = false;
                bool useJenkinsHash = false;
                while (true)
                {
                    switch (line[0])
                    {
                        case ':':
                            line = line.Substring(1);
                            printFileList = true;
                            continue;
                        case '#':
                            line = line.Substring(1);
                            useJenkinsHash = true;
                            continue;
                        default:
                            break;
                    }
                    break;
                }


                // get the hash from the line
                bool fail = false;
                if (useJenkinsHash == false)
                {
                    if ((line[0] == '-' || line[0] == '+') && line.Length > 1)
                    {
                        if (line[0] == '-')
                        {
                            int ihash = 0;
                            if (int.TryParse(line, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out ihash) == false)
                                fail = true;
                            else
                                hash = (uint)ihash;
                        }
                        else if (uint.TryParse(line, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out hash) == false)
                            fail = true;
                    }
                    else if (uint.TryParse(line, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out hash) == false)
                        fail = true;
                }
                else
                {
                    hash = line.HashJenkins();
                }

                if (fail == true)
                {
                    Console.WriteLine("This is not a valid hash. Please enter a hexadecimal number");
                    Console.WriteLine("You can also enter [+-][0-9]+ for a decimal number");
                    Console.WriteLine("You can also prefix any string with # to use the hash of the string");
                    Console.WriteLine("If you prefix the hash or the string with : it will print the name of all the RTPC file it appears in");
                    Console.WriteLine("");
                    continue;
                }

                // print the results
                bool res = SearchHash(hash, false, printFileList);
                res |= SearchHash(hash.Swap(), true, printFileList);

                if (!res)
                    Console.WriteLine("  hash not found");
            }
        }

        static bool SearchHash(uint hash, bool reverse, bool printFileList)
        {
            bool found = false;

            // walk the hash lists
            foreach (var pair in _HashFiles)
            {
                if (pair.Value.Contains(hash))
                {
                    if (reverse == false)
                        Console.WriteLine("  hash found in {0}", Path.GetFileNameWithoutExtension(pair.Key));
                    else
                        Console.WriteLine("  reverse hash found in {0}", Path.GetFileNameWithoutExtension(pair.Key));
                    found = true;
                }
            }

            // ask _Files for a name
            if (_Files.Contains(hash))
            {
                if (reverse == false)
                    Console.WriteLine("  found file that matches: {0}", _Files[hash]);
                else
                    Console.WriteLine("  found file that matches for reverse hash: {0}", _Files[hash]);
                found = true;
            }

            // ask _Name for a name
            if (_Names.Contains(hash))
            {
                if (reverse == false)
                    Console.WriteLine("  found string: {0}", _Names[hash]);
                else
                    Console.WriteLine("  found string for reverse hash: {0}", _Names[hash]);
                found = true;
            }

            // lookup the file list for this hash:
            if (_HashFileDict.ContainsKey(hash))
            {
                var l = _HashFileDict[hash];
                if (printFileList || l.Count <= 3)
                {
                    foreach (var h in l)
                    {
                        if (reverse == false)
                            Console.WriteLine("  used in {0}", _FileHashDict[h]);
                        else
                            Console.WriteLine("  reverse used in {0}", _FileHashDict[h]);
                    }
                }
                else if (l.Count > 0)
                {
                    if (reverse == false)
                        Console.WriteLine("  appears in {0} files. (prefix the hash with : to print the list)", l.Count, hash);
                    else
                        Console.WriteLine("  reverse appears in {0} files. (prefix the hash with : to print the list)", l.Count, hash);
                }
            }
            return found;
        }
    }
}
