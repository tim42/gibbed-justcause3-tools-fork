using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Gibbed.JustCause3.FileFormats;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Batch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Batch: perform multiple action on multiple files with one program");
                Console.WriteLine("Usage: Batch file1 [file2 [file3 [....]]]");
                Console.WriteLine("No argument provided: exiting");
                return;
            }

            uint failCount = 0;

            // loop over all those files
            foreach (string file in args)
            {
                try
                {
                    string program = null;
                    if (Directory.Exists(file)) // a directory is a job for smallpack !
                        program = "Gibbed.JustCause3.SmallPack";
                    else if (Path.GetExtension(file).ToLowerInvariant() == ".xml")
                        program = GetProgramForXMLFile(file);
                    else if (File.Exists(file))
                        program = GetProgramForBinaryFile(file);

                    if (string.IsNullOrEmpty(program))
                    {
                        Console.WriteLine(" ++ Skipping {0}", file);
                        ++failCount;
                        continue;
                    }

                    // Run the program
                    Console.WriteLine(" -- {0} {1}", program, file);
                    if (!execNoOutput(program + ".exe", "\"" + file + "\""))
                    {
                        ++failCount;
                        Console.WriteLine(" ** {0} failed to handle {1}", program, file);
                    }
                }
                catch (Exception e)
                {
                    ++failCount;
                    Console.WriteLine(" ** Skipping {0}: {1} [{2}]", file, e.Message, e.Source);
                }
            }

            if (failCount != 0)
            {
                Console.WriteLine(" ** Failed or skipped {0} files", failCount);
                Console.ReadKey();
            }
        }

        private static readonly Dictionary<string, string> _XMLLookup =
            new Dictionary<string, string>()
            {
                { "texture", "Gibbed.JustCause3.ConvertTexture" },
                { "adf", "Gibbed.JustCause3.ConvertAdf" },
                { "string-lookup", "Gibbed.JustCause3.ConvertStringLookup" },
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
