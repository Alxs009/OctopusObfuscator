using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using OctopusObfuscator.Core;
using TypeData = OctopusObfuscator.Protections.RenamerProtection.Utils.TypeData;

namespace OctopusObfuscator.Protections.RenamerProtection
{
    public class Renamer : IProtections
    {
        public string Name => "Rename Protection";
        public string Description => "Rename all types, methods, fields & property";

        public void Run(ModuleDefMD moduleDefMd)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Push($"Running {nameof(Renamer)}");
            Console.ForegroundColor = ConsoleColor.Gray;

            var utils = new Utils();
            utils.Initialize();

            foreach (var typeDef in moduleDefMd.GetTypes()
                .Where(x => x.HasMethods && !x.IsSerializable /* We exclude class with this attribute */
                                         && !x.IsGlobalModuleType))
            {
                typeDef.Name = utils.GetName(TypeData.Type);
                typeDef.Methods.Where(x => x.HasBody && !x.IsConstructor).ToList()
                    .ForEach(y => y.Name = utils.GetName(TypeData.Method));
                typeDef.Fields.ToList().ForEach(x => x.Name = utils.GetName(TypeData.Field));
                typeDef.Properties.ToList().ForEach(x => x.Name = utils.GetName(TypeData.Property));
            }
        }
    }
}