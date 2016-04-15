﻿using System;
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

        private static List<KeyValuePair<string, HashSet<uint>>> _HashFiles =
                        new List<KeyValuePair<string, HashSet<uint>>>();

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
            var hashFileDict = new Dictionary<uint, List<uint>>();
            var fileHashDict = new Dictionary<uint, string>();

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
                        hashFileDict.Add(hash, list);
                    }

                    uint scount = input.ReadValueU32();
                    for (uint i = 0; i < scount; ++i)
                    {
                        string s = input.ReadStringU32(Endian.Little);
                        uint h = s.HashJenkins();
                        if (!fileHashDict.ContainsKey(h))
                            fileHashDict.Add(h, s);
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

            // everything is loaded, run the interactive part
            while (true)
            {
                Console.Write("HASH> ");
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                uint hash = 0;
                bool fail = false;
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
                if (fail == true)
                {
                    Console.WriteLine("This is not a valid hash. Please enter a hexadecimal number.");
                    Console.WriteLine("You can also enter [+-][0-9]+ for a decimal number.");
                    continue;
                }

                bool res = SearchHash(hash, false, hashFileDict, fileHashDict);
                res |= SearchHash(hash.Swap(), true, hashFileDict, fileHashDict);

                if (!res)
                    Console.WriteLine("  hash not found");
            }
        }

        static bool SearchHash(uint hash, bool reverse, Dictionary<uint, List<uint>> hashFileDict, Dictionary<uint, string> fileHashDict)
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

            // ask _Name for a name
            if (_Names.Contains(hash))
            {
                if (reverse == false)
                    Console.WriteLine("  found string: {0}", _Names[hash]);
                else
                    Console.WriteLine("  found string for reverse hash: {0}", _Names[hash]);
            }

            // lookup the file list for this hash:
            if (hashFileDict.ContainsKey(hash))
            {
                var l = hashFileDict[hash];
                foreach (var h in l)
                    Console.WriteLine("  used in {0}", fileHashDict[h]);
            }
            return found;
        }
    }
}
