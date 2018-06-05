using Newtonsoft.Json;
using System;
using System.IO;

namespace MechMaintenanceByCost {
    public class Helper {

        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader($"{MechMaintenanceByCost.ModDirectory}/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }
    }
}