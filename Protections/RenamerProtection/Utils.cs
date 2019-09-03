using System.Collections.Generic;
using System.Linq;
using OctopusObfuscator.Helper;

namespace OctopusObfuscator.Protections.RenamerProtection
{
    public class Utils : Renamer
    {
        private readonly CryptoRandom _cryptoRandom;
        private List<string> _types;
        private List<string> _methods;
        private List<string> _fields;
        private List<string> _properties;

        public Utils()
        {
            _cryptoRandom = new CryptoRandom();
            _types = new List<string>();
            _methods = new List<string>();
            _fields = new List<string>();
            _properties = new List<string>();
        }

        /// <summary>
        /// Filling of all lists of the names of the mscorlib
        /// </summary>
        public void Initialize()
        {
            var module = typeof(void).Module; // Get module mscorlib
            foreach (var types in module.GetTypes())
            {
                // Adding names to list

                _types.Add(types.Name);
                types.GetMethods().ToList().ForEach(x => _methods.Add(x.Name));
                types.GetFields().ToList().ForEach(x => _fields.Add(x.Name));
                types.GetProperties().ToList().ForEach(x => _properties.Add(x.Name));
            }

            _types = _types.Distinct().ToList();
            _methods = _methods.Distinct().ToList();
            _fields = _fields.Distinct().ToList();
            _properties = _properties.Distinct().ToList();
        }

        /// <summary>
        /// Getting name from mscorlib
        /// https://docs.microsoft.com/ru-ru/dotnet/csharp/whats-new/csharp-8#more-patterns-in-more-places
        /// </summary>
        /// <param name="typeData">Type for rename</param>
        /// <returns></returns>
        public string GetName(TypeData typeData) =>
            typeData switch
            {
                TypeData.Type => _types[_cryptoRandom.Next(0, _types.Count)],
                TypeData.Method => _methods[_cryptoRandom.Next(0, _methods.Count)],
                TypeData.Field => _fields[_cryptoRandom.Next(0, _fields.Count)],
                TypeData.Property => _properties[_cryptoRandom.Next(0, _properties.Count)]
            };

        public enum TypeData
        {
            Type,
            Method,
            Field,
            Property
        }
    }
}