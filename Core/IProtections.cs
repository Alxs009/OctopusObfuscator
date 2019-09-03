using dnlib.DotNet;

namespace OctopusObfuscator.Core
{
    public interface IProtections
    {
        string Name { get; }
        string Description { get; }
        void Run(ModuleDefMD moduleDefMd);
    }
}