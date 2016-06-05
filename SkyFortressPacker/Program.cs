using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace DLCPacker
{
    class Program
    {
        static void Assert(bool condition, string mesg)
        {
            if (condition)
                return;
            Console.WriteLine();
            Console.WriteLine(" ** {0}", mesg);
            Console.WriteLine();

            throw new InvalidOperationException(mesg);
        }

        private static bool HasWritePermissionOnDir(string path)
        {
            var file = Path.Combine(path, ".dlc_stupid.file");
            try
            {
                var f = File.Create(file);
                f.Close();
                File.Delete(file);

                return true;
            }
            catch
            {
                return false;
            }
        }

        static void Main(string[] args)
        {
            bool has_error = false;

            using (var input = File.OpenRead("./dropzone-scheme.xml"))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/dropzones");
                if (root == null)
                    throw new FormatException();

                var dlcList = root.Select("dlc-dropzone");

                foreach (XPathNavigator dlcNode in dlcList)
                {
                    string dropzone_folder = dlcNode.GetAttribute("name", "");
                    string destination_folder = dlcNode.GetAttribute("game-folder", "");
                    string archive_name = dlcNode.GetAttribute("archive", "");

                    if (!Directory.Exists(dropzone_folder))
                    {
                        Console.WriteLine(" ++ Skipping {0} as it does not exist", Path.GetFileName(dropzone_folder));
                        continue;
                    }

                    Console.WriteLine(" -- {0}:", Path.GetFileName(dropzone_folder));
                    try
                    {
                        PackSingleFolder(dropzone_folder, destination_folder, archive_name);
                    }
                    catch (Exception e)
                    {
                        has_error = true;
                        Console.WriteLine(" ** {0}", e.ToString());
                        Console.WriteLine(" ++ Skipping {0} because of some error(s)", Path.GetFileName(dropzone_folder));
                    }
                }
            }

            if (has_error)
            {
                Console.WriteLine();
                Console.WriteLine(" * press any key to exit *");
                Console.ReadKey();
            }
        }

        // folder is the PATH of the folder (like "../dropzone_sky_fortress")
        private static void PackSingleFolder(string dropzone_folder, string destination_folder, string archive_name)
        {
            // some checks
            Assert(Directory.Exists(dropzone_folder), "Unable to find " + Path.GetFileName(dropzone_folder) + " in the parent folder");
            Assert(Directory.Exists(destination_folder), "Unable to find " + destination_folder + " (do you have the DLC ?)");
            Assert(HasWritePermissionOnDir(Path.GetDirectoryName(dropzone_folder)), "You don't have write permission on the parent folder (execute as admin)");
            Assert(HasWritePermissionOnDir(destination_folder), "You don't have write permission on " + destination_folder + " (execute as admin)");

            // the actual code (should be a shell script, if only windows would have bash/zsh and some working GNU tools)
            Console.WriteLine("   -- Repacking {0}...", Path.GetFileName(dropzone_folder));
            execNoOutput("Gibbed.JustCause3.Pack.exe", "-o " + dropzone_folder);

            Console.WriteLine("   -- Installing the dropzone...");
            if (File.Exists(Path.Combine(destination_folder, archive_name + ".arc")))
            {
                File.Replace(dropzone_folder + ".arc", Path.Combine(destination_folder, archive_name + ".arc"), Path.Combine(destination_folder, archive_name + ".arc.bak"));
                File.Replace(dropzone_folder + ".tab", Path.Combine(destination_folder, archive_name + ".tab"), Path.Combine(destination_folder, archive_name + ".tab.bak"));
            }
            else
            {
                File.Move(dropzone_folder + ".arc", Path.Combine(destination_folder, archive_name + ".arc"));
                File.Move(dropzone_folder + ".tab", Path.Combine(destination_folder, archive_name + ".tab"));
            }
        }

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
    }
}
