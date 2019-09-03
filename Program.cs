using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using OctopusObfuscator.Core;
using OctopusObfuscator.Helper;
using OctopusObfuscator.Protections;
using OctopusObfuscator.Protections.AntiTamper;
using OctopusObfuscator.Protections.Mutations;
using OctopusObfuscator.Protections.RenamerProtection;
using OctopusObfuscator.Protections.StringEncoder;

namespace OctopusObfuscator
{
    class Program
    {
        private static List<IProtections> _protectionses =
            new List<IProtections>
            {
                // Your can support more protections
                // if want
                new StringEncoder(),
                new Mutation(),
                new Renamer(),
                new AntiTamper()
            };

        static void Main(string[] args)
        {
            var engine = new Engine(_protectionses);
            engine.Initialize();
            engine.Obfuscate();
            Console.ReadLine();
        }
    }

    class Logger
    {
        public enum TypeLine
        {
            Default,
            NewLine
        }

        public static void Push(object arg, TypeLine typeLine = TypeLine.NewLine)
        {
            switch (typeLine)
            {
                case TypeLine.Default:
                    Console.Write($"[{DateTime.Now}]: {arg}");
                    break;
                case TypeLine.NewLine:
                    Console.WriteLine($"[{DateTime.Now}]: {arg}");
                    break;
            }
        }
    }
}