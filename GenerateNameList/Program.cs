
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gibbed.IO;
using Gibbed.ProjectData;
using Gibbed.JustCause3.FileFormats;
using Gibbed.JustCause3.PropertyFormats;

namespace GenerateNameList
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

        static void Main(string[] args)
        {
            Console.BufferWidth = 150;
            Console.Clear();

            var manager = Manager.Load(); // load the current project
            var project = manager.ActiveProject;

            Assert(project != null, "Not active project selected");

            var installPath = project.InstallPath;
            var listsPath = project.ListsPath;
            string outputPath = Path.Combine(listsPath, "00_generated.namelist");
            string hashOutputPath = Path.Combine(listsPath, "generated.hashlist");

            Assert(installPath != null, "Could not detect install path.");
            Assert(listsPath != null, "Could not detect lists path.");

            bool overwrite = false;
            bool alreadyExists = File.Exists(outputPath) && File.Exists(hashOutputPath);

            // overwrite or create the generated.namelist
            if (overwrite || !alreadyExists)
            {
                Console.WriteLine("Will regenerate the list of all known names");
                Console.WriteLine("It will not extract anything nor write on the HDD.");
                Console.WriteLine();

                // the search part
                Console.WriteLine("Searching for archives...");
                var inputPaths = new List<string>();

                var locations = new Dictionary<string, string>()
                {
                    { "archives_win64", "game*.tab" },
                    { "dlc_win64", "*.tab" },
                    { "patch_win64", "*.tab" },
                };

                foreach (var kv in locations)
                {
                    var locationPath = Path.Combine(installPath, kv.Key);

                    if (Directory.Exists(locationPath) == true)
                        inputPaths.AddRange(Directory.GetFiles(locationPath, kv.Value, SearchOption.AllDirectories));
                }

                Console.WriteLine("Found {0} game archives", inputPaths.Count);

                uint processorCount = (uint)Environment.ProcessorCount;
                Console.WriteLine("Using {0} search threads", processorCount);

                string outputLookupPath = Path.Combine(listsPath, "90_generated.stringlookup.namelist");
                string outputTmpPath = Path.Combine(listsPath, "00_generated.tmp.namelist");

                var foundStrings = new SortedSet<string>(); // where I will put all those strings !
                var foundHashes = new SortedSet<uint>(); // where I will put all those hash !

                var searchThreads = new List<ThreadSearcher>();
                for (uint i = 0; i < processorCount; ++i)
                {
                    searchThreads.Add(new ThreadSearcher(i, processorCount));
                    searchThreads[(int)i].Search(inputPaths);
                }
                // wait for completion
                foreach (var searcher in searchThreads)
                    searcher.Wait();
                // merge results
                using (var stringOutputStream = File.Create(outputTmpPath))
                using (var stringLookupOutputStream = File.Create(outputLookupPath))
                using (var hashOutputStream = File.Create(hashOutputPath))
                {
                    foreach (var searcher in searchThreads)
                        searcher.MergeResults(stringOutputStream, foundStrings, hashOutputStream, foundHashes, stringLookupOutputStream);
                }

                Console.WriteLine("Found {0} unique hash and {1} unique strings", foundHashes.Count, foundStrings.Count);
            }
            else
            {
                Console.WriteLine("Found a generated namelist from a previous run. Will not overwrite it.");
            }

            Console.WriteLine("Will generate and reduce the namelist");
            Console.WriteLine();

            Console.WriteLine("Loading name hash list...");
            var hashList = new SortedSet<uint>(); // where I will put all those hash !

            using (var hashFile = File.OpenRead(hashOutputPath))
            {
                long length = hashFile.Length;
                for (long i = 0; i < length; i += 4)
                    hashList.Add(hashFile.ReadValueU32(Endian.Little));
            }

            // compile the result into one file (that's also named generated.namelist)
            Console.WriteLine("Compiling all the different namelist files into a single one...");
            var nameListFileList = Directory.GetFiles(listsPath, "*.*list", SearchOption.AllDirectories);
            Array.Sort(nameListFileList); // 'cause I use the [0-9]{2}_[a-zA-Z0-9_.-]+.namelist scheme to name files

            var ovrpNameList = new SortedSet<string>(); // the overpopulated nameList
            var ovrpHashSet = new SortedSet<uint>();    // the overpopulated hash set

            // get unique strings (that also have unique hashes)
            var discardRegexp = new System.Text.RegularExpressions.Regex("[^A-Za-z0-9_-]");
            var isOnlyNumberRegexp = new System.Text.RegularExpressions.Regex("^[+-]?[0-9]+$");
            foreach (var nameListFile in nameListFileList)
            {
                if (Path.GetExtension(nameListFile) == ".hashlist")
                    continue;
                Console.WriteLine(" -- loading {0}...", Path.GetFileName(nameListFile));

                var file = File.OpenRead(nameListFile);
                var input = new StreamReader(file);
                string line = null;
                while ((line = input.ReadLine()) != null)
                {
                    AddString(line, ovrpNameList, ovrpHashSet, hashList);
                    line = line.Trim();
                    AddString(line, ovrpNameList, ovrpHashSet, hashList);
                    AddString(ConvertToCamelCase(line), ovrpNameList, ovrpHashSet, hashList);
                    AddString(ConvertToSnakeCase(line), ovrpNameList, ovrpHashSet, hashList);

                    var splitted = line.Split(',', ' ', '|', '\t', '/', '\\');
                    foreach (var str in splitted)
                    {
                        if (string.IsNullOrWhiteSpace(str))
                            continue;
                        AddString(str, ovrpNameList, ovrpHashSet, hashList);
                        AddString(ConvertToCamelCase(str), ovrpNameList, ovrpHashSet, hashList);
                        AddString(ConvertToSnakeCase(str), ovrpNameList, ovrpHashSet, hashList);
                    }
                }
                file.Close();
            }
 
            Console.WriteLine("Total number of unique strings in the DB: {0} strings", ovrpNameList.Count);
            Console.WriteLine("Hash/Name DB completness: {0:F1}%", (float)(ovrpNameList.Count) * 100 / (float)(hashList.Count));

            var output = File.Create(outputPath);
            foreach (string str in ovrpNameList)
            {
                output.WriteString(str);
                output.WriteString(Environment.NewLine);
            }

            Console.WriteLine();
            Console.WriteLine("{0} saved. Please remove any other .namelist file !", Path.GetFileName(outputPath));

            System.Threading.Thread.Sleep(3000);
        }

        private static void AddString(string str, SortedSet<string> stringSet, SortedSet<uint> hashStringSet, SortedSet<uint> hashSet)
        {
            uint strHash = str.HashJenkins();
            if (hashSet.Contains(strHash) && !hashStringSet.Contains(strHash))
            {
                hashStringSet.Add(strHash);
                stringSet.Add(str);
            }

        }

        // Exec, discarding the ouput
        private static void execNoOutput(string command, string args)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(command, args);
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            Assert(p.ExitCode == 0, command + ' ' + args + ": failed");
        }

        private static string ConvertToCamelCase(string phrase)
        {
            string[] splittedPhrase = phrase.Split('-', '_', ' ', '.');
            var sb = new StringBuilder();

            foreach (string s in splittedPhrase)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                char[] splittedPhraseChars = s.ToCharArray();
                splittedPhraseChars[0] = char.ToUpperInvariant(splittedPhraseChars[0]);
                    //for (int i = 1; i < splittedPhraseChars.Length; ++i)
                    //    splittedPhraseChars[i] = char.ToLowerInvariant(splittedPhraseChars[i]);
                sb.Append(new String(splittedPhraseChars));
            }
            return sb.ToString();
        }

        // Convert to snake_case
        private static string ConvertToSnakeCase(string phrase)
        {
            var word = new StringBuilder();
            var res = new StringBuilder();

            phrase = phrase.Replace('-', '_').Replace(' ', '_').Replace('.', '_');

            char[] array = phrase.ToCharArray();
            int i = 0;
            for (; i < array.Length && array[i] == '_'; ++i) ; // skip leading '_'

            bool IsLowerCase = false;
            for (; i < array.Length; ++i)
            {
                IsLowerCase |= array[i] >= 'a' && array[i] <= 'z';
                if (array[i] == '_') // found an underscore
                {
                    res.Append(word.ToString().ToLowerInvariant());
                    word.Clear();
                    res.Append('_');
                    for (; i+1 < array.Length && array[i+1] == '_'; ++i) ; // skip other underscores
                    IsLowerCase = false;
                }
                else if (array[i] >= 'A' && array[i] <= 'Z' && IsLowerCase)
                {
                    // found an UPPER CASE letter following some lower case
                    res.Append(word.ToString().ToLowerInvariant());
                    res.Append('_');
                    word.Clear();
                    word.Append(array[i]);
                    IsLowerCase = false;
                }
                else
                    word.Append(array[i]);
            }
            res.Append(word.ToString().ToLowerInvariant());
            return res.ToString();
        }
    }
}
