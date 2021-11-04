using BH.oM.Analytical.Results;
using BH.oM.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Development
{
    class Program
    {
        public static void Main(string[] args = null)
        {
            List<Assembly> assemblies = new List<Assembly>();
            //assemblies.AddRange(AssemblyUtils.LoadAssemblies(@"C:\Program Files\Autodesk\Revit 2018", false));
            assemblies.AddRange(AssemblyUtils.LoadAssemblies());

            List<ToolkitCRUDMethods> allToolkitCRUDMethods = DiscoverCRUDMethods(assemblies);
            Dictionary<string, string> toolkitMarkdownTables = new Dictionary<string, string>();

            string directory = @"C:\Users\alombardi\BuroHappold\Buildings and Habitats object Model - 02_Current\03_Code analysis\Auto-generated\CRUD_methods";
            System.IO.Directory.CreateDirectory(directory);

            foreach (var toolkitMethods in allToolkitCRUDMethods)
            {
                var cruMethodsPerType = CRUMethodsPerType(toolkitMethods);
                string cruMarkdownTable = CRUMarkdownTable(cruMethodsPerType);
                toolkitMarkdownTables[toolkitMethods.ToolkitName] = cruMarkdownTable;

                File.WriteAllText(Path.Combine(directory, $"{toolkitMethods.ToolkitName}.md"), cruMarkdownTable);
            }

            Console.WriteLine("Press any other key to close.");
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
        }

        public static Dictionary<Type, Tuple<List<MethodInfo>, List<MethodInfo>, List<MethodInfo>>> CRUMethodsPerType(ToolkitCRUDMethods tkms)
        {
            // Aggregate CRU methods available on each type. Return a Dictionary with key is the type and value is a tuple where first item = Create methods, etc.
            var result = new Dictionary<Type, Tuple<List<MethodInfo>, List<MethodInfo>, List<MethodInfo>>>();
            List<Type> allTypes = tkms.CreateMethods.Keys.Concat(tkms.ReadMethods.Keys).Concat(tkms.UpdateMethods.Keys).ToList();
            foreach (Type type in allTypes)
            {
                List<MethodInfo> typeCreateMethods = new List<MethodInfo>();
                List<MethodInfo> typeReadMethods = new List<MethodInfo>();
                List<MethodInfo> typeUpdateMethods = new List<MethodInfo>();

                tkms.CreateMethods.TryGetValue(type, out typeCreateMethods);
                tkms.ReadMethods.TryGetValue(type, out typeReadMethods);
                tkms.UpdateMethods.TryGetValue(type, out typeUpdateMethods);


                var tuple = new Tuple<List<MethodInfo>, List<MethodInfo>, List<MethodInfo>>(typeCreateMethods, typeReadMethods, typeUpdateMethods);
                result[type] = tuple;
            }

            return result;
        }

        public static string CRUMarkdownTable(Dictionary<Type, Tuple<List<MethodInfo>, List<MethodInfo>, List<MethodInfo>>> TypeCRUMethods)
        {
            string CRU = "| Object | Create | Read | Update |\n";
            CRU += "|-|-|-|-|\n";
            foreach (var kv in TypeCRUMethods)
            {
                CRU += $"| {kv.Key.FullName} | {MethodText(kv.Value.Item1)} | {MethodText(kv.Value.Item2)} | {MethodText(kv.Value.Item3)} |\n";
            }

            return CRU;
        }

        public static string MethodText(List<MethodInfo> methods, bool distinct = true)
        {
            if (methods == null)
                return "";

            return String.Join("<br>",methods.Select(m => BH.Engine.Reflection.Convert.ToText(m, true)).Distinct());
        }

        public class ToolkitCRUDMethods
        {
            public string ToolkitName { get; set; }
            public Dictionary<Type, List<MethodInfo>> CreateMethods { get; set; }
            public Dictionary<Type, List<MethodInfo>> ReadMethods { get; set; }
            public Dictionary<Type, List<MethodInfo>> UpdateMethods { get; set; }
            public List<MethodInfo> DeleteMethods { get; set; }
        }

        // Discover Adapter support of specific BHoMObjects.
        public static List<ToolkitCRUDMethods> DiscoverCRUDMethods(List<Assembly> assemblies)
        {
            // Our result list.
            List<ToolkitCRUDMethods> allToolkitCRUDMethods = new List<ToolkitCRUDMethods>();

            // Get all Engine and Adapter assemblies, grouped per Toolkit name.
            Dictionary<string, List<Assembly>> allToolkitAssemblies = GetToolkitAssemblies(assemblies, false);

            foreach (var kv in allToolkitAssemblies)
            {

                string toolkitName = kv.Key;
                string softwareName = kv.Key.Split(new string[] { "_Toolkit" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
 
                List<Assembly> toolkitAssemblies = kv.Value;

                ToolkitCRUDMethods toolkitCRUDMethods = new ToolkitCRUDMethods() { ToolkitName = toolkitName };

                if (toolkitName.Contains("Revit"))
                    toolkitName = toolkitName;


                // For each Toolkit assembly:
                //      If the assembly name ends for _Adapter, collect all methods into an AdapterMethods List.
                var adapterAssemblies = toolkitAssemblies.Where(a => a.GetName().Name.Contains("_Adapter"));
                var adapterTypes = adapterAssemblies.Select(a => a.TryGetTypes()).SelectMany(t => t);
                var adapterMethods = adapterTypes.SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).ToList();
                var adapterMethods_declaredOnly = adapterTypes.SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)).ToList();

                if (toolkitName.Contains("Revit"))
                    toolkitName = toolkitName;

                // (a) For each method that has name that contains a word like "Create", "Read", "Update", "Delete":
                //       Create/Update/Delete => get all methods that have a subclass of BHoMObject or IObject as their first parameter.
                //       Read => get all methods that return subclass of BHoMObject or IObject. 
                var createMethods = adapterMethods
                    .Where(m => m.Name.ToLower().Contains("create"))
                    .GroupBy(m => TypeUtils.GetInnermostType(m.GetParameters().FirstOrDefault()?.ParameterType))
                    .Where(g => IsBHoMType(g.Key) && !IsBHoMGenericBaseType(g.Key))
                    .ToDictionary(g => g.Key, g => g.Distinct().ToList());

                var readMethods = adapterMethods
                    .Where(m => m.Name.ToLower().Contains("read"))
                    .GroupBy(m => TypeUtils.GetInnermostType(m.ReturnType))
                    .Where(g => IsBHoMType(g.Key) || g.Key == typeof(void))
                    .ToDictionary(g => g.Key, g => g.Distinct().ToList());

                var updateMethods = adapterMethods
                    .Where(m => m.Name.ToLower().Contains("update"))
                    .GroupBy(m => TypeUtils.GetInnermostType(m.GetParameters().FirstOrDefault()?.ParameterType))
                    .Where(g => IsBHoMType(g.Key) && !IsBHoMGenericBaseType(g.Key))
                    .ToDictionary(g => g.Key, g => g.Distinct().ToList());

                var deleteMethods = adapterMethods_declaredOnly
                    .Where(m => m.Name.ToLower().Contains("delete") && m.Name != "IDelete").Distinct().ToList();

                // (b) Rely on the Converts methods. 
                // Ideally we would want to find all references (which CRUD method calls the Convert method). This is hard.
                // Simplification: if the method name contains ToXXX => Create; FromXXX => Read // where XXX = adapter name.
                // Limitation of this approach: this only dispatches methods to Create and Read (no update / delete). Ok for most cases. Haven't seen any Update/Delete calling a Convert.
                // Get all the XXX_Engine Convert methods
                //   for each method
                //      if the method name contains ToXXX => Create; FromXXX => Read // where XXX = adapter name 

                //var engineAssemblies = toolkitAssemblies.Where(a => a.GetName().Name.EndsWith("_Engine"));
                var convertMethods = toolkitAssemblies.SelectMany(a => a.TryGetTypes())
                    .Where(t => t.Name.Contains("Convert"))
                    .SelectMany(t => t.GetMethods());

                var convertToMethods = convertMethods.Where(m => m.Name.ToLower().Contains($"To{softwareName}".ToLower()));
                var convertToMethodsPerType = convertToMethods
                    .GroupBy(m => TypeUtils.GetInnermostType(m.GetParameters().FirstOrDefault().ParameterType))
                    .Where(g => IsBHoMType(g.Key) && !IsBHoMGenericBaseType(g.Key))
                    .ToDictionary(g => g.Key, g => g.ToList());
                createMethods.ConcatenateDictionaryValues(convertToMethodsPerType, true);

                var convertFromMethods = convertMethods.Where(m => m.Name.ToLower().Contains($"From{softwareName}".ToLower()));
                var convertFromMethodsPerType = convertToMethods
                    .GroupBy(m => TypeUtils.GetInnermostType(m.ReturnType))
                    .Where(g => IsBHoMType(g.Key) && !IsBHoMGenericBaseType(g.Key))
                    .ToDictionary(g => g.Key, g => g.ToList());
                readMethods.ConcatenateDictionaryValues(convertFromMethodsPerType, true);

                // If after (b) still there are CRUD methods but they have no correspondent object-specific method:
                // (c) rely on the CRUD method body content. Check if we can gather info from the body instructions.
                // E.g. IES: The Read method mentions in its body what objects are supported. It calls a non-compliantly named ToBHoMPanels method. 
                // Get all methods that in their name mention the words for CRUD, for which no result was previously found 
                // (e.g. if a method for Read was already found, don't do this)
                //    for each method
                //        get the instructions like MethodBodyReader.GetInstructions(methodBase) // see https://stackoverflow.com/a/5490526/3873799
                //           for each instruction
                //              for Read: if the instruction contains a reference to a method that returns a subclass of BHoMObject or a List of them, add the relative method
                //              for Create: same but the trigger is the first parameter of the method
                // TODO

                if (!createMethods.Any() && !readMethods.Any() && !updateMethods.Any() && !deleteMethods.Any())
                    continue;

                toolkitCRUDMethods.CreateMethods = createMethods;
                toolkitCRUDMethods.ReadMethods = readMethods;
                toolkitCRUDMethods.UpdateMethods = updateMethods;
                toolkitCRUDMethods.DeleteMethods = deleteMethods;

                allToolkitCRUDMethods.Add(toolkitCRUDMethods);
            }

            return allToolkitCRUDMethods;
        }

        /// <summary>
        /// Get all the assemblies belonging to all Toolkits.
        /// </summary>
        public static Dictionary<string, List<Assembly>> GetToolkitAssemblies(List<Assembly> assemblies, bool includeOm = true, bool includeEngine = true, bool includeAdapter = true)
        {
            var gna = assemblies
                .Where(a => a.GetCustomAttribute<AssemblyProductAttribute>().Product.EndsWith("_Toolkit")); // not reliable. We dont write the name consistently.


            HashSet<string> allAdapterNames = new HashSet<string>(assemblies
                .Where(a => a.GetName().Name.ToLower().Contains("adapter") && a.GetName().Name != "BHoM_Adapter")
                .Select(a => a.GetName().Name.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
                .Where(n => n != "Adapter")
                );


            // Collect all assemblies that have an AssemblyProduct name that ends with _Toolkit(e.g. "IES_Toolkit")
            Dictionary<string, List<Assembly>> allToolkitAssemblies = assemblies
               .Where(a =>
               {
                   string projName = a.GetCustomAttribute<AssemblyTitleAttribute>().Title;
                   getinfo(a);
                   return (includeOm && projName.Contains("_oM")) || (includeEngine && projName.Contains("_Engine")) || (includeAdapter && projName.Contains("_Adapter"));
               })
               .Where(a => allAdapterNames.Contains(a.GetName().Name.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()))
               .GroupBy(a => a.GetCustomAttribute<AssemblyTitleAttribute>().Title.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() + "_Toolkit")
               .ToDictionary(g => g.Key, g => g.ToList());

            return allToolkitAssemblies;
        }

        public static void getinfo(Assembly a)
        {
            if (a.GetName().Name.Contains("Revit"))
                a = a;

            string assemblyTitle = a.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            string assemblyName = a.GetName().Name;

        }

        public static bool IsBHoMGenericBaseType(Type type)
        {
            return !(type.Name != "BHoMObject" && type.Name != "IObject" && type.Name != "IBHoMObject");
        }

        public static bool IsBHoMType(Type type)
        {
            return type.FullName?.StartsWith("BH.oM") ?? false;
        }
    }
}
