using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;

namespace OctopusObfuscator.Core
{
    public class Engine
    {
        private ModuleDefMD _moduleDefMd;

        private readonly List<IProtections> _protectionses;

        private readonly Stopwatch _stopwatch;

        public Engine(List<IProtections> protectionses)
        {
            _protectionses = protectionses;
            _stopwatch = new Stopwatch();
        }

        public void Initialize()
        {
            Logger.Push("Input assembly: ", Logger.TypeLine.Default);
            var assembly = Console.ReadLine();
            _moduleDefMd = LoadAssembly(assembly);

            _stopwatch.Start();

            Logger.Push("Resolving dependency...");
            ResolveDependency(_moduleDefMd);
        }

        public void Obfuscate()
        {
            var i = 0;
            _protectionses.ForEach(x => { Logger.Push($"{++i}) {x.Name}: {x.Description}"); });

            Logger.Push("Select options: ", Logger.TypeLine.Default);
            var prefers = Console.ReadLine()?.ToCharArray().Select(x => int.Parse(x.ToString()) - 1).ToList();
            if (prefers != null)
                foreach (var options in prefers)
                    _protectionses[options].Run(_moduleDefMd);

            void Watermark()
            {
                Logger.Push("Watermarking...");
                var attribute = new TypeDefUser("", "OctopusObfuscator",
                    _moduleDefMd.ImportAsTypeSig(typeof(Attribute)).ToTypeDefOrRef());
                _moduleDefMd.Types.Add(attribute);

                var body = new MethodDefUser(
                    "_" + Guid.NewGuid().ToString("n").ToUpper().Substring(2, 5),
                    MethodSig.CreateStatic(_moduleDefMd.ImportAsTypeSig(typeof(void))),
                    MethodAttributes.Static | MethodAttributes.Public);
                attribute.Methods.Add(body);
                _moduleDefMd.CustomAttributes.Add(new CustomAttribute(body));
            }

            Watermark();

            SaveAssembly(_moduleDefMd);
            _stopwatch.Stop();
            Logger.Push($"Obfuscation task finished. Time elapsed: {_stopwatch.Elapsed}");
        }

        /// <summary>
        /// Load assembly from path
        /// </summary>
        /// <param name="path">Path to .NET executable file</param>
        /// <returns>Return <see cref="ModuleDefMD"/></returns>
        private ModuleDefMD LoadAssembly(string path) => !string.IsNullOrEmpty(path) ? ModuleDefMD.Load(path) : null;

        /// <summary>
        /// Saving assembly with prefix '_protected'
        /// </summary>
        /// <param name="moduleDefMd">Current <see cref="ModuleDefMD"/></param>
        private void SaveAssembly(ModuleDefMD moduleDefMd) =>
            moduleDefMd.Write(Path.Combine(
                Path.GetDirectoryName(moduleDefMd.Location) ?? throw new InvalidOperationException(),
                Path.GetFileNameWithoutExtension(moduleDefMd.Location) + "_protected" +
                Path.GetExtension(moduleDefMd.Location)));

        private void ResolveDependency(ModuleDefMD moduleDefMd)
        {
            var resolver = new AssemblyResolver();
            try
            {
                foreach (var assemblyRef in moduleDefMd.GetAssemblyRefs().Where(x => x != null))
                {
                    var resolved = resolver.Resolve(assemblyRef.FullName, moduleDefMd); // Resolve assemblyDef
                    if (moduleDefMd.Context.AssemblyResolver.AddToCache(resolved))
                        Logger.Push($"Resolved dependency: {resolved.Name}");
                }
            }
            catch (AssemblyResolveException)
            {
                Logger.Push("Failed resolve dependency. Make sure dependency is near with executable file");
            }
            catch (Exception ex)
            {
                Logger.Push($"Unknown exception message: {ex.Message}");
            }
        }
    }
}