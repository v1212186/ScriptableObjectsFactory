using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Compilation;
using UnityEngine;

namespace ScriptableObjectsFactory.Editor
{
    public static class AssemblyUtils 
    {

        public static IEnumerable<UnityEditor.Compilation.Assembly> GetAssembliesByType(AssembliesType _assembliesType)
        {
            IEnumerable<UnityEditor.Compilation.Assembly> assemblies =
                CompilationPipeline.GetAssemblies(_assembliesType);
            assemblies = FilterAssembliesWithoutScriptableObjects(assemblies);
            return assemblies;
        }

        private static IEnumerable<UnityEditor.Compilation.Assembly> FilterAssembliesWithoutScriptableObjects(
            IEnumerable<UnityEditor.Compilation.Assembly> _assemblies)
        {
            IList<UnityEditor.Compilation.Assembly> assembliesWithScriptableObjects =
                new List<UnityEditor.Compilation.Assembly>();
            foreach (UnityEditor.Compilation.Assembly playerAssembly in _assemblies)
            {
                if (CheckIfAssemblyContainsScriptableObjects(playerAssembly))
                {
                    assembliesWithScriptableObjects.Add(playerAssembly);
                }
            }

            return assembliesWithScriptableObjects;
        }

        private static bool CheckIfAssemblyContainsScriptableObjects(UnityEditor.Compilation.Assembly _assembly)
        {
            System.Reflection.Assembly assembly = GetAssembly(_assembly.name);
            if ((from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(ScriptableObject))
                select t).Any())
            {
                return true;
            }

            return false;
        }

        public static System.Reflection.Assembly GetAssembly(string _assemblyName)
        {
            return System.Reflection.Assembly.Load(new AssemblyName(_assemblyName));
        }

        public static IEnumerable<string> GetAssemblyNames(IEnumerable<UnityEditor.Compilation.Assembly> _assemblies)
        {
            return _assemblies.Select(_assembly => _assembly.name);
        }
    }
}
