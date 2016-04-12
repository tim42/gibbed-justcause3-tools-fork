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
using System.Xml;
using System.Xml.XPath;
using NDesk.Options;
using Gibbed.IO;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.ConvertAdf
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static void SetOption<T>(string s, ref T variable, T value)
        {
            if (s == null)
            {
                return;
            }

            variable = value;
        }

        internal enum Mode
        {
            Unknown,
            Export,
            Import,
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            bool showHelp = false;
            var typeLibraryPaths = new List<string>();

            var options = new OptionSet
            {
                // ReSharper disable AccessToModifiedClosure
                { "e|export", "convert from binary to XML", v => SetOption(v, ref mode, Mode.Export) },
                { "i|import", "convert from XML to binary", v => SetOption(v, ref mode, Mode.Import) },
                // ReSharper restore AccessToModifiedClosure
                { "t|type-library=", "load type library from file", v => typeLibraryPaths.Add(v) },
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

            if (mode == Mode.Unknown && extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);
                if (extension != null && extension.ToLowerInvariant() == ".xml")
                {
                    mode = Mode.Import;
                }
                else
                {
                    mode = Mode.Export;
                }
            }

            if (extras.Count < 1 || extras.Count > 2 ||
                showHelp == true ||
                mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ [-e] input_adf [output_xml]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ [-i] input_xml [output_adf]", GetExecutableName());
                Console.WriteLine("Convert an ADF file between binary and XML format.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var runtime = new RuntimeTypeLibrary();
            foreach (var typeLibraryPath in typeLibraryPaths)
            {
                var adf = new FileFormats.AdfFile("");

                using (var input = File.OpenRead(typeLibraryPath))
                {
                    adf.Deserialize(input);
                }

                runtime.AddTypeDefinitions(adf);
            }

            if (mode == Mode.Export)
            {
                string inputPath = extras[0];
                string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".xml");

                using (var input = File.OpenRead(inputPath))
                {
                    var adf = new AdfFile(Path.GetExtension(inputPath));
                    adf.Deserialize(input);
                    runtime.AddTypeDefinitions(adf);


                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "    ",
                        CheckCharacters = false,
                    };

                    using (var output = File.Create(outputPath))
                    {
                        var writer = XmlWriter.Create(output, settings);
                        Exporter.Export(adf, writer/*, runtime, input, writer*/);
                        writer.Flush();
                    }
                }
            }
            else if (mode == Mode.Import)
            {
                string inputPath = extras[0];

                using (var input = File.OpenRead(inputPath))
                {
                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/adf");
                    if (root == null)
                        throw new FormatException();

                    // retrieve the original extension as stored in the XML file
                    var outputExtension = root.GetAttribute("extension", "");
                    string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, outputExtension);


                    AdfFile adf = Importer.Import(root);

                    using (var output = File.Create(outputPath))
                    {
                        output.Position = adf.EstimateHeaderSize();
                        adf.Serialize(output, 0);
                    }
                }
            }
        }
    }
}
