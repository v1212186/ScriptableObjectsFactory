using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Compilation;
using UnityEngine;

namespace ScriptableObjectsFactory.Editor
{
    public static class AssemblyUtils 
    {

        public static IEnumerable<UnityEditor.Compilation.Assembly> GetPlayerAssemblies()
        {
            IEnumerable<UnityEditor.Compilation.Assembly> playerAssemblies =
                CompilationPipeline.GetAssemblies(AssembliesType.Player);
            playerAssemblies = FilterPlayerAssemblies(playerAssemblies);
            return playerAssemblies;
        }

        public static IEnumerable<UnityEditor.Compilation.Assembly> FilterPlayerAssemblies(
            IEnumerable<UnityEditor.Compilation.Assembly> _playerAssemblies)
        {
            IList<UnityEditor.Compilation.Assembly> playerAssembliesWithScriptableObjects =
                new List<UnityEditor.Compilation.Assembly>();
            foreach (UnityEditor.Compilation.Assembly playerAssembly in _playerAssemblies)
            {
                if (CheckIfAssemblyContainsScriptableObjects(playerAssembly))
                {
                    playerAssembliesWithScriptableObjects.Add(playerAssembly);
                }
            }

            return playerAssembliesWithScriptableObjects;
        }

        public static bool CheckIfAssemblyContainsScriptableObjects(UnityEditor.Compilation.Assembly _assembly)
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
