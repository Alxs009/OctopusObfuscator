using System;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using OctopusObfuscator.Core;
using OctopusObfuscator.Helper;
using Decoder = OctopusObfuscator.Protections.StringEncoder.Runtime.Decoder;

namespace OctopusObfuscator.Protections.StringEncoder
{
    class StringEncoder : IProtections
    {
        public string Name => "String Encoder";
        public string Description => "Encode all strings in assembly";

        public void Run(ModuleDefMD moduleDefMd)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Push($"Running {nameof(StringEncoder)}");
            Console.ForegroundColor = ConsoleColor.Gray;

            var module = ModuleDefMD.Load(typeof(Decoder).Module);
            var type = module.ResolveTypeDef(MDToken.ToRID(typeof(Decoder).MetadataToken));
            var decoderMethod =
                InjectHelper.Inject(type, moduleDefMd.GlobalType, moduleDefMd).SingleOrDefault() as MethodDef;

            using var cryptoRandom = new CryptoRandom();
            foreach (var typeDef in moduleDefMd.GetTypes().Where(x => x.HasMethods))
            {
                foreach (var methodDef in typeDef.Methods.Where(x => x.HasBody))
                {
                    var instructions = methodDef.Body.Instructions;
                    for (var i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].OpCode == OpCodes.Ldstr &&
                            !string.IsNullOrEmpty(instructions[i].Operand.ToString()))
                        {
                            var key = methodDef.Name.Length + cryptoRandom.Next();

                            var encryptedString =
                                EncryptString(new Tuple<string, int>(instructions[i].Operand.ToString(), key));

                            instructions[i].OpCode = OpCodes.Ldstr;
                            instructions[i].Operand = encryptedString;
                            instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(key));
                            instructions.Insert(i + 2, OpCodes.Call.ToInstruction(decoderMethod));
                            i += 2;
                        }
                    }

                    methodDef.Body.SimplifyMacros(methodDef.Parameters);
                }
            }
        }

        private string EncryptString(Tuple<string, int> values)
        {
            var stringBuilder = new StringBuilder();
            int key = values.Item2;
            foreach (var symbol in values.Item1)
                stringBuilder.Append((char) (symbol ^ key));
            return stringBuilder.ToString();
        }
    }
}