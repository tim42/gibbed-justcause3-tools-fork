using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyFortressPacker
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
            Console.ReadKey();
            System.Environment.Exit(1);
        }

        private static bool HasWritePermissionOnDir(string path)
        {
            var file = Path.Combine(path, ".sky_fortress_stupid.file");
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
            // some checks
            Assert(Directory.Exists("..\\dropzone_sky_fortress"), "Unable to find dropzone_sky_fortress in the parent folder");
            Assert(Directory.Exists("..\\dlc_win64\\sky_fortress"), "Unable to find dlc_win64/sky_fortress in the parent folder (do you have sky fortress ?)");
            Assert(HasWritePermissionOnDir("..\\"), "You don't have write permission on the parent folder (execute as admin)");
            Assert(HasWritePermissionOnDir("..\\dlc_win64\\sky_fortress"), "You don't have write permission on the sky_fortress folder (execute as admin)");

            // the actual code (should be a shell script, if only windows would have bash/zsh and some working GNU tools)
            Console.WriteLine(" -- Repacking the Skyfortress dropzone...");
            execNoOutput("Gibbed.JustCause3.Pack.exe", "-o ..\\dropzone_sky_fortress");

            Console.WriteLine(" -- Installing the dropzone...");
            if (File.Exists("..\\dlc_win64\\sky_fortress\\game3.arc"))
            {
                File.Replace("..\\dropzone_sky_fortress.arc", "..\\dlc_win64\\sky_fortress\\game3.arc", "..\\dlc_win64\\sky_fortress\\game3.arc.bak");
                File.Replace("..\\dropzone_sky_fortress.tab", "..\\dlc_win64\\sky_fortress\\game3.tab", "..\\dlc_win64\\sky_fortress\\game3.tab.bak");
            }
            else
            {
                File.Move("..\\dropzone_sky_fortress.arc", "..\\dlc_win64\\sky_fortress\\game3.arc");
                File.Move("..\\dropzone_sky_fortress.tab", "..\\dlc_win64\\sky_fortress\\game3.tab");
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
