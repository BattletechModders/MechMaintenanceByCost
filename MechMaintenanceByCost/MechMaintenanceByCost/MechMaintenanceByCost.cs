using Harmony;
using System.Reflection;

namespace MechMaintenanceByCost
{
    public class MechMaintenanceByCost
    {
        public static void Init() {
            var harmony = HarmonyInstance.Create("de.morphyum.MechMaintenanceByCost");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }


    }
}
