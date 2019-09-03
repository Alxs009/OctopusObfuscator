using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using OctopusObfuscator.Core;
using MethodBody = System.Reflection.MethodBody;
using OpCodes = dnlib.DotNet.Emit.OpCodes;

namespace OctopusObfuscator.Protections.AntiTamper
{
    public class AntiTamper : IProtections
    {
        public string Name => "Anti Tamper Protection";
        public string Description => "Prevent change .exe";

        public void Run(ModuleDefMD moduleDefMd)
        {
            var memoryStream = new MemoryStream {Position = 0};
            var antiTamperData = new AntiTamperData(moduleDefMd);
            antiTamperData.Initialize();
            var released = new List<Tuple<MethodDef, List<Instruction>>>();
            foreach (var typeDef in moduleDefMd.GetTypes().Where(x => x.HasMethods && !x.IsGlobalModuleType))
            {
                foreach (var methodDef in typeDef.Methods.Where(x =>
                    x.HasBody && x.ReturnType == moduleDefMd.ImportAsTypeSig(typeof(void))))
                {
                    var boolean = new Local(moduleDefMd.ImportAsTypeSig(typeof(bool)));
                    methodDef.Body.Variables.Add(boolean);
                    var instructions = methodDef.Body.Instructions;
                    instructions.Insert(0,
                        OpCodes.Newobj.ToInstruction(antiTamperData.GetMethod(".ctor")));
                    instructions.Insert(1, OpCodes.Ldc_I4.ToInstruction(0));
                    instructions.Insert(2, OpCodes.Callvirt.ToInstruction(antiTamperData.GetMethod("GetFrame")));
                    instructions.Insert(3, OpCodes.Callvirt.ToInstruction(antiTamperData.GetMethod("GetMethod")));
                    instructions.Insert(4, OpCodes.Callvirt.ToInstruction(antiTamperData.GetMethod("GetMethodBody")));
                    instructions.Insert(5,
                        OpCodes.Callvirt.ToInstruction(antiTamperData.GetMethod("GetILAsByteArray")));
                    instructions.Insert(6, OpCodes.Ldlen.ToInstruction());
                    instructions.Insert(7, OpCodes.Conv_I4.ToInstruction());
                    instructions.Insert(8, OpCodes.Ldc_I4.ToInstruction(methodDef.Body.GetILAsByteArray().Length));
                    instructions.Insert(9, OpCodes.Ceq.ToInstruction());
                    instructions.Insert(10, OpCodes.Stloc.ToInstruction(boolean));
                    instructions.Insert(11, OpCodes.Ldloc.ToInstruction(boolean));
                    instructions.Insert(12, OpCodes.Brfalse.ToInstruction(instructions[instructions.Count - 1]));
                    released.Add(new Tuple<MethodDef, List<Instruction>>(methodDef, instructions.ToList()));
                }
            }

            moduleDefMd.Write(memoryStream);
            released.ForEach(x =>
            {
                /* Overriding size of il, because new instructions have been added */
                x.Item2[8].Operand = OpCodes.Ldc_I4;
                x.Item2[8].Operand = GetIlLength((x.Item1, memoryStream.ToArray()));
            });
        }

        private int GetIlLength((MethodDef, byte[]) data) => Assembly.Load(data.Item2).ManifestModule
            .ResolveMethod(data.Item1.MDToken.ToInt32()).GetMethodBody().GetILAsByteArray().Length;
    }

    class AntiTamperData
    {
        private List<IMethod> MethodDefs { get; }

        private readonly ModuleDefMD _moduleDefMd;

        public AntiTamperData(ModuleDefMD moduleDefMd)
        {
            _moduleDefMd = moduleDefMd;
            MethodDefs = new List<IMethod>();
        }

        public void Initialize()
        {
            MethodDefs.Add(_moduleDefMd.Import(typeof(StackTrace).GetConstructor(new Type[0])));
            MethodDefs.Add(_moduleDefMd.Import(typeof(StackTrace).GetMethod("GetFrame", new Type[] {typeof(int)})));
            MethodDefs.Add(_moduleDefMd.Import(typeof(StackFrame).GetMethod("GetMethod")));
            MethodDefs.Add(_moduleDefMd.Import(typeof(MethodBase).GetMethod("GetMethodBody")));
            MethodDefs.Add(_moduleDefMd.Import(typeof(MethodBody).GetMethod("GetILAsByteArray")));
        }

        public IMethod GetMethod(string methodName) => MethodDefs.Find(x => x.Name == methodName);
    }
}