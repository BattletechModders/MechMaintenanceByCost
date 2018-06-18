using BattleTech;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace MechMaintenanceByCost {
    public class Helper {

        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader("mods/MechMaintenanceByCost/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static float CalculateCBillValue(MechDef mech) {
            float currentCBillValue = 0f;
            float num = 10000f;
                currentCBillValue = (float)mech.Chassis.Description.Cost;
                float num2 = 0f;
                num2 += mech.Head.CurrentArmor;
                num2 += mech.CenterTorso.CurrentArmor;
                num2 += mech.CenterTorso.CurrentRearArmor;
                num2 += mech.LeftTorso.CurrentArmor;
                num2 += mech.LeftTorso.CurrentRearArmor;
            num2 += mech.RightTorso.CurrentArmor;
            num2 += mech.RightTorso.CurrentRearArmor;
            num2 += mech.LeftArm.CurrentArmor;
            num2 += mech.RightArm.CurrentArmor;
            num2 += mech.LeftLeg.CurrentArmor;
            num2 += mech.RightLeg.CurrentArmor;
            num2 *= UnityGameInstance.BattleTechGame.MechStatisticsConstants.CBILLS_PER_ARMOR_POINT;
                currentCBillValue += num2;
                for (int i = 0; i < mech.Inventory.Length; i++) {
                    MechComponentRef mechComponentRef = mech.Inventory[i];
                    currentCBillValue += (float)mechComponentRef.Def.Description.Cost;
                }
                currentCBillValue = Mathf.Round(currentCBillValue / num) * num;
            return currentCBillValue;
        }
    }
}