using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using Gibbed.JustCause3.FileFormats;

namespace Gibbed.JustCause3.ConvertStringLookup
{
    class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  {0}  file.stringlookup", GetExecutableName());
                Console.WriteLine("  {0}  file.xml", GetExecutableName());
                return;
            }

            string inputPath = args[0];

            if (Path.GetExtension(inputPath).ToLowerInvariant() == ".xml") // IMPORT
            {
                using (var input = File.OpenRead(inputPath))
                {
                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/string-lookup");
                    if (root == null)
                        throw new FormatException();

                    // retrieve the original extension as stored in the XML file
                    var outputExtension = root.GetAttribute("extension", "");
                    string outputPath = Path.ChangeExtension(inputPath, outputExtension);


                    AdfFile adf = Importer.Import(root);

                    using (var output = File.Create(outputPath))
                    {
                        output.Position = adf.EstimateHeaderSize();
                        adf.Serialize(output, 0);
                    }
                }
            }
            else // EXPORT (to XML)
            {
                string outputPath = Path.ChangeExtension(inputPath, ".xml");
                using (var input = File.OpenRead(inputPath))
                {
                    var adf = new AdfFile(Path.GetExtension(inputPath));
                    adf.Deserialize(input);
                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "    ",
                        CheckCharacters = false,
                    };

                    using (var output = File.Create(outputPath))
                    {
                        var writer = XmlWriter.Create(output, settings);
                        Exporter.Export(adf, writer);
                        writer.Flush();
                    }
                }
            }
        }
    }
}
