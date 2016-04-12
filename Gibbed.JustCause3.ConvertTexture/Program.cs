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
using System.IO;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;
using NDesk.Options;

namespace Gibbed.JustCause3.ConvertTexture
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = true;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                { "v|verbose", "be verbose", v => verbose = v != null },
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

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.ddsc [output_base_filename]", GetExecutableName());
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.xml  [output_file.ddsc]", GetExecutableName());
                Console.WriteLine("If there's a .hmddsc with the same name in the same folder, they will be used too");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null);

            if (Path.GetExtension(inputPath) == ".ddsc") // export to DDS + xml
                Exporter.Export(inputPath, outputPath);
            else if (Path.GetExtension(inputPath) == ".xml") // import from DDS + xml
                Importer.Import(inputPath, outputPath);
        }
    }
}
