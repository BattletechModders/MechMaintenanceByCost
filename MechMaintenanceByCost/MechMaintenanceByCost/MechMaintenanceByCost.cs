using Harmony;
using System.Reflection;

namespace MechMaintenanceByCost
{
    public class MechMaintenanceByCost
    {
        internal static string ModDirectory;

        public static void Init(string directory, string settingsJSON) {
            var harmony = HarmonyInstance.Create("de.morphyum.MechMaintenanceByCost");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModDirectory = directory;
        }
    } 
}
