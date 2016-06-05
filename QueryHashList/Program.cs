using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
                    Console.WriteLine("WARNING: Found {0} conflicts in the cross result file", conflicts);
                try
                {
                    if (max + 20 > Console.WindowHeight)
                        Console.BufferHeight = max + 20;
                }
                catch { } // depending the console you use, this may throw
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
                Console.Write("QUERY> ");
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    continue;

                List<uint> hash = new List<uint>();
                int beginIndex = 0;
                bool printFileList = false;
                bool isInvalid = false;
                System.Text.RegularExpressions.Regex regexpFilterString = null;

                while (true)
                {
                    if (beginIndex > 0)
                    {
                        if (beginIndex >= line.Length)
                        {
                            line = "";
                            break;
                        }
                        else
                            line = line.Substring(beginIndex);
                        beginIndex = 0;
                    }

                    switch (line[0])
                    {
                        case ':':
                            printFileList = true;
                            beginIndex = 1;
                            if (line.Length > 2 && line[1] == '@') // we have a regexp
                            {
                                for (beginIndex = 2; line[beginIndex] != '@' && beginIndex < line.Length; ++beginIndex);
                                try
                                {
                                    regexpFilterString = new System.Text.RegularExpressions.Regex(line.Substring(2, beginIndex - 2), System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Compiled);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed to compile the regular expression: {0}", e.ToString());
                                    isInvalid = true;
                                }
                                beginIndex += 1;
                            }
                            continue;
                        default:
                            break;
                    }
                    break;
                }

                if (isInvalid)
                    continue;

                // get the hash from the line
                bool fail = false;
                string[] splitline = line.Split('&');

                foreach (string tline in splitline)
                {
                    if (tline == "help")
                        fail = true;
                    else if (tline[0] != '#')
                    {
                        uint thash = 0;

                        if ((tline[0] == '-' || tline[0] == '+') && tline.Length > 1)
                        {
                            if (tline[0] == '-')
                            {
                                int ihash = 0;
                                if (int.TryParse(tline, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out ihash) == false)
                                    fail = true;
                                else
                                    thash = ((uint)ihash);
                            }
                            else if (uint.TryParse(tline, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out thash) == false)
                                fail = true;
                        }
                        else if (uint.TryParse(tline, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out thash) == false)
                            fail = true;

                        hash.Add(thash);
                    }
                    else
                        hash.Add(tline.Substring(1).HashJenkins());

                    if (fail)
                        break;
                }

                if (fail == true || hash.Count == 0)
                {
                    Console.WriteLine("Please enter a hexadecimal number.");
                    Console.WriteLine("You can also enter a decimal number prefixed by either + or -");
                    Console.WriteLine("You can also prefix any string with # to use the hash of the string");
                    Console.WriteLine("If you prefix the query with : it will print the name of all the files it appears in");

                    Console.WriteLine("");

                    Console.WriteLine("'reverse hash' tells you that when changing the endianness of the value/hash");
                    Console.WriteLine("there's some results. (useful when copy/pasting data from hexadecimal editors).");

                    Console.WriteLine("");

                    Console.WriteLine("File paths have this form: ");
                    Console.WriteLine("  folder/gameX.arc:path/to/AAF_File.ee:path/to/final/file.bla");
                    Console.WriteLine("  folder/gameX.arc:path/to/final/file.bla");
                    Console.WriteLine("The path contain enough information for you to retrieve it.");

                    Console.WriteLine("");

                    Console.WriteLine("You can apply regex filter on files names to narrow the results:");
                    Console.WriteLine("The regex filter must directly follow the : and be delimited by @");
                    Console.WriteLine("QUERY> :@patch_win64.*/02_two_handed/.*\\.wtunec@#damage");
                    Console.WriteLine("This query will only print files that contains the string damage");
                    Console.WriteLine("and whose path matches with patch_win64.*/02_two_handed/.*\\.wtunec");
                    Console.WriteLine("(notice the / used as a directory separator)");

                    Console.WriteLine("");

                    Console.WriteLine("You can also only print files that matches multiple conditions");
                    Console.WriteLine("To achieve this, you'll have to separate the different condition by &, like in");
                    Console.WriteLine("QUERY> #WeaponTuning&#rpg_target_priority&+10");
                    Console.WriteLine("that will only print files that contain both the strings 'WeaponTuning' and 'rpg_target_priority' as well as the ");
                    Console.WriteLine("as well as the decimal value of 10");
                    Console.WriteLine("please note that when searching files that matches multiple conditions,");
                    Console.WriteLine("the reverse search is not performed.");

                    Console.WriteLine("");

                    Console.WriteLine("QUERY> :@patch_win64/.*@#WeaponTuning&#rpg_target_priority&+10");

                    Console.WriteLine("");

                    continue;
                }

                // print the results
                bool res = false;
                if (hash.Count == 1)
                {
                    res = SearchHash(hash[0], false, printFileList, regexpFilterString);
                    if (hash[0].Swap() != hash[0])
                        res |= SearchHash(hash[0].Swap(), true, printFileList, regexpFilterString);
                }
                else
                    res = SearchHashes(hash, printFileList, regexpFilterString);

                if (!res)
                    Console.WriteLine("  no result found");
            }
        }

        static bool SearchHash(uint hash, bool reverse, bool printFileList, System.Text.RegularExpressions.Regex regexpFilterString)
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
                        string fileName = _FileHashDict[h];
                        if (regexpFilterString != null && regexpFilterString.IsMatch(fileName) == false)
                            continue;
                        if (reverse == false)
                            Console.WriteLine("  used in {0}", fileName);
                        else
                            Console.WriteLine("  reverse used in {0}", fileName);
                    }
                }
                else if (l.Count > 0)
                {
                    if (reverse == false)
                        Console.WriteLine("  appears in {0} files. (prefix the query with : to print the list)", l.Count, hash);
                    else
                        Console.WriteLine("  reverse appears in {0} files. (prefix the query with : to print the list)", l.Count, hash);
                }
            }
            return found;
        }

        static bool SearchHashes(List<uint> hashList, bool printFileList, System.Text.RegularExpressions.Regex regexpFilterString)
        {
            uint count = 0;

            // lookup the file list for those hashes:
            if (_HashFileDict.ContainsKey(hashList[0]))
            {
                var l = _HashFileDict[hashList[0]];
                foreach (var h in l)
                {
                    string fileName = _FileHashDict[h];
                    if (regexpFilterString != null && regexpFilterString.IsMatch(fileName) == false)
                        continue;
                    bool isMatching = true;
                    for (int i = 1; i < hashList.Count; ++i)
                    {
                        if (_HashFileDict[hashList[i]].Contains(h) == false)
                        {
                            isMatching = false;
                            break;
                        }
                    }
                    if (isMatching)
                    {
                        ++count;
                        if (count > 10 && !printFileList)
                            continue;
                        Console.WriteLine("  matching: {0}", fileName);
                    }
                }
            }

            if (count > 10 && !printFileList)
                Console.WriteLine("  {0} more file matches (prefix the query with : to print the complete list)", count - 10);

            return count > 0;
        }
    }
}
