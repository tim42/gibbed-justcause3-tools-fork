using System;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Threading;
using Gibbed.JustCause3.FileFormats;

namespace GenerateFileName
{
    class BruteForcer
    {
        static private char[] AllowedChars =
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'y', 'v', 'w', 'x', 'y', 'z',
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '_', '-', '[', ']', '+', '=', '{', '}',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'Y', 'V', 'W', 'X', 'Y', 'Z'
            };
        private const uint StringLen = 12;
        private uint Hash;
        private string BaseString;
        private string Extension;

        public bool Found;
        public string GeneratedString;

        public BruteForcer(uint hash, string baseString, string extension)
        {
            this.Hash = hash;
            this.BaseString = baseString;
            this.Extension = extension;
            this.Found = false;
        }

        public void bruteForce()
        {
            int maxLen = AllowedChars.Length;
            ulong iterCount = 0;

            byte[] name = new byte[StringLen];
            var rng = RandomNumberGenerator.Create();
            while (this.GenerateString(name).HashJenkins() != this.Hash)
            {
                ++iterCount;

                rng.GetBytes(name);

                if ((iterCount % 4000000) == 0)
                    Console.WriteLine("{0} iterations... [size: {1}] -- {2}", iterCount, name.Length, this.GenerateString(name));
            }

            this.Found = true;
            this.GeneratedString = this.GenerateString(name);
            Console.WriteLine(" -- found !! {0}", this.GeneratedString);
        }

        private string GenerateString(byte[] name)
        {
            string ret = this.BaseString.Length != 0 ? this.BaseString + '\\' : "";
            foreach (uint index in name)
                ret += AllowedChars[(index % AllowedChars.Length)];
            if (Extension.Length > 0)
                ret += '.' + Extension;
            return ret;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This utility bruteforce hashes in order to find some file path matching this hash.");
            Console.Write("Enter the hash: ");
            string hash = Console.ReadLine();
            uint uintHash = uint.Parse(hash, NumberStyles.AllowHexSpecifier);
            Console.Write("Enter the base path for the file (without trailing backslash): ");
            string basePath = Console.ReadLine();
            Console.Write("Enter the extension of the file (without the dot): ");
            string extension = Console.ReadLine();

            List<BruteForcer> brfList = new List<BruteForcer>();
            List<Thread> threadList = new List<Thread>();
            int proco = Environment.ProcessorCount;
            Console.WriteLine("performing {0} parralel searchs", proco);
            for (int i = 0; i < proco; ++i)
            {
                brfList.Add(new BruteForcer(uintHash, basePath, extension));
                threadList.Add(new Thread(brfList[i].bruteForce));
                threadList[i].Start();
            }
            string result = null;

            while (true)
            {
                bool stop = false;
                for (int i = 0; i < proco; ++i)
                {
                    if (brfList[i].Found)
                    {
                        stop = true;
                        result = brfList[i].GeneratedString;
                    }
                }
                if (stop)
                    break;
                Thread.Sleep(2000);
            }
            for (int i = 0; i < proco; ++i)
                threadList[i].Abort();

            Console.WriteLine();
            Console.WriteLine("Generated name: {0}", result);
            Console.ReadKey();
        }
    }
}
