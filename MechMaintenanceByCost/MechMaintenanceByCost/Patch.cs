using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



namespace MechMaintenanceByCost {

   [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        static void Postfix(SGCaptainsQuartersStatusScreen __instance)
        {
            try {
                Traverse.Create(__instance).Method("ClearListLineItems", new Type[] { typeof(Transform) }).GetValue(new object[] { Traverse.Create(__instance).Field("SectionOneExpensesList").GetValue() });

                Settings settings = Helper.LoadSettings();

                SimGameState simState = Traverse.Create(__instance).Field("simState").GetValue<SimGameState>();
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
                    Traverse.Create(__instance).Method("AddListLineItem", new Type[] { typeof(Transform), typeof(string), typeof(string) }).GetValue(new object[] { Traverse.Create(__instance).Field("SectionOneExpensesList").GetValue(), entry.Key, SimGameState.GetCBillString(entry.Value) });
                });

                Traverse.Create(__instance).Method("SetField", new Type[] { typeof(LocalizableText), typeof(string) }).GetValue(new object[] { Traverse.Create(__instance).Field("SectionOneExpensesField").GetValue<LocalizableText>(), SimGameState.GetCBillString(ongoingUpgradeCosts) } );
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
    public static class SimGameState_GetExpenditures
    {

        static void Postfix(ref SimGameState __instance, ref int __result, ref bool proRate)
        {
            try
            {
                Settings settings = Helper.LoadSettings();
                float expenditureCostModifier = __instance.GetExpenditureCostModifier(__instance.ExpenditureLevel);
                __result = 0;

                FinancesConstantsDef finances = __instance.Constants.Finances;
                __result = __instance.GetShipBaseMaintenanceCost();
                for (int i = 0; i < __instance.ShipUpgrades.Count; i++)
                {
                    __result += Mathf.RoundToInt(expenditureCostModifier * __instance.ShipUpgrades[i].AdditionalCost);
                }

                for (int i = 0; i < __instance.PilotRoster.Count; i++)
                {
                    __result += Mathf.RoundToInt(expenditureCostModifier * __instance.GetMechWarriorValue(__instance.PilotRoster[i].pilotDef));
                }

                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (settings.CostByTons)
                    {
                        __result += Mathf.RoundToInt(expenditureCostModifier * (float)mechDef.Chassis.Tonnage * settings.cbillsPerTon);
                        if (settings.TonsAdditive)
                        {
                            __result += Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                        }
                    }
                    else
                    {
                        __result += Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                    }
                }


                __result -= Mathf.RoundToInt(expenditureCostModifier * ((!proRate) ? 0 : Traverse.Create(__instance).Field("ProRateRefund").GetValue<int>()));
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}