using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MechMaintenanceByCost {

   [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        static void Postfix(SGCaptainsQuartersStatusScreen __instance) {
            try {
                ReflectionHelper.InvokePrivateMethode(__instance, "ClearListLineItems",new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList") });

                Settings settings = Helper.LoadSettings();

                SimGameState simState = (SimGameState)ReflectionHelper.GetPrivateField(__instance, "simState");
                float expenditureCostModifier = simState.GetExpenditureCostModifier(simState.ExpenditureLevel);
                List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
                int ongoingUpgradeCosts = 0;
                string key = (simState.CurDropship != DropshipType.Leopard) ? "Argo Operating Costs" : "Bank Loan Interest Payment";
                int value = Mathf.RoundToInt(expenditureCostModifier * (float)simState.GetShipBaseMaintenanceCost());
                list.Add(new KeyValuePair<string, int>(key, value));
                foreach (ShipModuleUpgrade shipModuleUpgrade in simState.ShipUpgrades) {
                    if (simState.CurDropship == DropshipType.Argo && shipModuleUpgrade.AdditionalCost > 0) {
                        string name = shipModuleUpgrade.Description.Name;
                        value = Mathf.RoundToInt(expenditureCostModifier * (float)shipModuleUpgrade.AdditionalCost);
                        list.Add(new KeyValuePair<string, int>(name, value));
                    }
                }
                foreach (MechDef mechDef in simState.ActiveMechs.Values) {
                    key = mechDef.Name;
                    if (settings.CostByTons) {
                        value = Mathf.RoundToInt(expenditureCostModifier * (float)mechDef.Chassis.Tonnage * settings.cbillsPerTon);
                        if(settings.TonsAdditive) {
                            value += Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                        }
                    }
                    else {
                        value = Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                    }

                    list.Add(new KeyValuePair<string, int>(key, value));
                }
                list.Sort((KeyValuePair<string, int> a, KeyValuePair<string, int> b) => b.Value.CompareTo(a.Value));
                list.ForEach(delegate (KeyValuePair<string, int> entry)
                {
                    ongoingUpgradeCosts += entry.Value;
                    ReflectionHelper.InvokePrivateMethode(__instance, "AddListLineItem", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList"), entry.Key, SimGameState.GetCBillString(entry.Value) });
                });

                ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesField"), SimGameState.GetCBillString(ongoingUpgradeCosts) }, new Type[] { typeof(TextMeshProUGUI), typeof(string)  });
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
    public static class SimGameState_GetExpenditures {

        static void Postfix(ref SimGameState __instance, ref int __result) {
            try {
                Settings settings = Helper.LoadSettings();
                float expenditureCostModifier = __instance.GetExpenditureCostModifier(__instance.ExpenditureLevel);
                foreach (MechDef mechDef in __instance.ActiveMechs.Values) {
                    __result -= Mathf.CeilToInt(__instance.Constants.Finances.MechCostPerQuarter * expenditureCostModifier);
                    if (settings.CostByTons) {
                        __result += Mathf.RoundToInt((float)mechDef.Chassis.Tonnage * settings.cbillsPerTon * expenditureCostModifier);
                        if (settings.TonsAdditive) {
                            __result += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                        }
                    } else {
                        __result += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                    }               
                }
            }              
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}