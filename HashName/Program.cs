using System;
using System.IO;
using Gibbed.JustCause3.FileFormats;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashName
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    var filename = Path.GetFileName(args[i]);
                    Console.WriteLine("{0:X8} {1}", args[i].HashJenkins(), args[i]);
                    Console.WriteLine("  {0:X8} {1}", filename.HashJenkins(), filename);
                }
                Console.ReadKey();
            }
            else
            {
                while (true)
                {
                    Console.Write("String to hash: ");
                    string line = Console.ReadLine();
                    Console.WriteLine("result: {0:X8}", line.HashJenkins());
                    if (string.IsNullOrEmpty(line))
                        break;
                }
            }
        }
    }
}
