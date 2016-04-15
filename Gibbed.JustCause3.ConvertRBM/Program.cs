
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Gibbed.IO;
using Gibbed.JustCause3.RenderBlockModel;
using NDesk.Options;

namespace Gibbed.JustCause3.ConvertRBM
{
    class Program
    {
        internal enum Mode
        {
            Unknown,
            Export,
            Import,
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static void SetOption<T>(string s, ref T variable, T value)
        {
            if (s == null)
            {
                return;
            }

            variable = value;
        }

        static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            bool showHelp = false;

            var options = new OptionSet
            {
                { "e|export", "convert from binary to OBJ", v => SetOption(v, ref mode, Mode.Export) },
                { "i|import", "convert from OBJ to binary", v => SetOption(v, ref mode, Mode.Import) },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };
            List<string> extras;

            try
            {
                extras = options.Parse(args);
                if (extras.Count < 1)
                    throw new NotSupportedException("you should provide a file as parameter");
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            string inputPath = extras[0];

            if (mode == Mode.Unknown)
            {
                if (Path.GetExtension(extras[0]) == ".rbm")
                    mode = Mode.Export;
            }

            if (mode == Mode.Export)
            {
                using (var input = File.OpenRead(inputPath))
                {
                    ModelFile mdl = new ModelFile();
                    mdl.Deserialize(input);
                }
            }
            else
                throw new NotImplementedException("TODO");
        }
    }
}
