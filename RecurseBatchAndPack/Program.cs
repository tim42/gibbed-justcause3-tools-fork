using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Gibbed.JustCause3.FileFormats;

using System.Collections.Generic;


namespace RecurseBatchAndPack
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("RecurseBatchAndPack: Lookup for any .xml files in a list of directory, run the corresponding program and pack the folder using SmallPack");
                Console.WriteLine("Usage: Batch directory1 [directory2 [directory3 [....]]]");
                Console.WriteLine("No argument provided: exiting");
                return;
            }

            uint failCount = 0;

            // loop over all those files
            foreach (string dir in args)
            {
                var printDir = Path.Combine(Path.GetFileName(Path.GetDirectoryName(dir)), Path.GetFileName(dir));

                try
                {
                    if (Directory.Exists(dir))
                    {
                        var xmlFiles = Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories);
                        if (!BatchXMLFiles(xmlFiles))
                        {
                            ++failCount;
                            Console.WriteLine(" ++ Skipping {0} due to some errors above", printDir);
                            continue;
                        }
                    }
                    else
                    {
                        ++failCount;
                        Console.WriteLine(" ++ Skipping {0}: not a directory / doesn't exists", printDir);
                    }
                    Console.WriteLine(" -- Packing {0}", dir);
                    if (!execNoOutput("Gibbed.JustCause3.SmallPack.exe", "\"" + dir + "\""))
                    {
                        ++failCount;
                        Console.WriteLine(" ** failed to pack {0}", printDir);
                    }
                }
                catch (Exception e)
                {
                    ++failCount;
                    Console.WriteLine(" ** Skipping {0}: {1} [{2}]", printDir, e.Message, e.Source);
                }
            }

            if (failCount != 0)
            {
                Console.WriteLine(" ** Failed or skipped {0} directories", failCount);
                Console.ReadKey();
            }
        }

        private static bool BatchXMLFiles(string[] xmlFiles)
        {
            // loop over all those files
            uint failCount = 0;
            foreach (string file in xmlFiles)
            {
                if (Path.GetFileName(file).ToLowerInvariant() == "@files.xml")
                    continue;
                var printFile = Path.Combine(Path.GetFileName(Path.GetDirectoryName(file)), Path.GetFileName(file));
                try
                {
                    string program = GetProgramForXMLFile(file);

                    if (string.IsNullOrEmpty(program))
                    {
                        Console.WriteLine("   ++ Skipping {0}", printFile);
                        continue;
                    }

                    // Run the program
                    Console.WriteLine("   -- {0} {1}", program, printFile);
                    if (!execNoOutput(program + ".exe", "\"" + file + "\""))
                    {
                        ++failCount;
                        Console.WriteLine("   ** {0} failed to handle {1}", program, printFile);
                    }
                }
                catch (Exception e)
                {
                    ++failCount;
                    Console.WriteLine("   ** Skipping {0}: {1} [{2}]", printFile, e.Message, e.Source);
                }
            }
            return (failCount == 0);
        }

        private static readonly Dictionary<string, string> _XMLLookup =
            new Dictionary<string, string>()
            {
                { "texture", "Gibbed.JustCause3.ConvertTexture" },
                { "string-lookup", "Gibbed.JustCause3.ConvertStringLookup" },
                { "adf", "Gibbed.JustCause3.ConvertAdf" },
                { "container", "Gibbed.JustCause3.ConvertProperty" },
            };

        static private string GetProgramForXMLFile(string file)
        {
            using (var input = File.OpenRead(file))
            using (var reader = XmlReader.Create(input))
            {
                reader.MoveToContent();
                if (_XMLLookup.ContainsKey(reader.Name))
                    return _XMLLookup[reader.Name];
            }
            return null;
        }

        private static readonly Dictionary<string, string> _ExtensionLookup =
            new Dictionary<string, string>()
            {
                { "ddsc", "Gibbed.JustCause3.ConvertTexture" },
                { "adf", "Gibbed.JustCause3.ConvertAdf" },
                { "rtpc", "Gibbed.JustCause3.ConvertProperty" },
                { "aaf", "Gibbed.JustCause3.SmallUnpack" },
                { "tab", "Gibbed.JustCause3.Unpack" },
            };

        static private string GetProgramForBinaryFile(string file)
        {
            using (var input = File.OpenRead(file))
            {
                var guess = new byte[32];

                var read = input.Read(guess, 0, (int)Math.Min(guess.Length, input.Length));
                string extension = FileDetection.Detect(guess, read);

                if (_ExtensionLookup.ContainsKey(extension))
                    return _ExtensionLookup[extension];
            }
            return null;
        }

        private static bool execNoOutput(string command, string args)
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

            return (p.ExitCode == 0);
        }
    }
}
