using System;
using System.Reflection;
using BattleTech;
using Harmony;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using UnityEngine;
using Localize;

namespace MechMaintenanceByCost
{
    public class MechMaintenanceByCost
    {
        public const string ModName = "MechMaintenanceByCost";
        public const string ModId = "de.morphyum.MechMaintenanceByCost";

        internal static ModSettings settings;
        internal static string ModDirectory;
        public static void Init(string directory, string modSettings)
        {
            ModDirectory = directory;
            try
            {
                settings = JsonConvert.DeserializeObject<ModSettings>(modSettings);
            }
            catch (Exception e)
            {
                Helper.Logger.LogError(e);
                settings = new ModSettings();
            }

            var harmony = HarmonyInstance.Create(ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
        public static class SGCaptainsQuartersStatusScreen_RefreshData
        {
            private static readonly MethodInfo methodAddLineItem = AccessTools.Method(typeof(SGCaptainsQuartersStatusScreen), "AddListLineItem");
            public static bool Prefix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange, SimGameState ___simState,
              SGDifficultyIndicatorWidget ___ExpenditureLevelIndicatorWidget, LocalizableText ___ExpenditureLevelField, LocalizableText ___SectionOneExpenseLevel,
              LocalizableText ___SectionTwoExpenseLevel, SGFinancialForecastWidget ___FinanceWidget, LocalizableText ___MoraleValueField, SGMoraleBar ___MoralBar,
              Transform ___SectionOneExpensesList, LocalizableText ___SectionOneExpensesField, LocalizableText ___SectionTwoExpensesField,
              Transform ___SectionTwoExpensesList, LocalizableText ___EndOfQuarterFunds, LocalizableText ___QuarterOperatingExpenses,
              LocalizableText ___CurrentFunds, List<LocalizableText> ___ExpenditureLvlBtnMoraleFields, List<LocalizableText> ___ExpenditureLvlBtnCostFields)
            {
                if (__instance == null || ___simState == null)
                {
                    return true;
                }
                float expenditureCostModifier = ___simState.GetExpenditureCostModifier(expenditureLevel);
                Traverse methodSetField = Traverse.Create(__instance)
                  .Method("SetField", new Type[] { typeof(LocalizableText), typeof(string) });
                int expLevel = (int)Traverse.Create(__instance)
                  .Method("GetExpendetureLevelIndexNormalized", new object[] { expenditureLevel }).GetValue();
                ___ExpenditureLevelIndicatorWidget.SetDifficulty(expLevel * 2);
                methodSetField.GetValue(new object[] { ___ExpenditureLevelField, string.Format("{0}", (object)expenditureLevel) });
                methodSetField.GetValue(new object[] { ___SectionOneExpenseLevel, string.Format("{0}", (object)expenditureLevel) });
                methodSetField.GetValue(new object[] { ___SectionTwoExpenseLevel, string.Format("{0}", (object)expenditureLevel) });
                ___FinanceWidget.RefreshData(expenditureLevel);
                int num1 = ___simState.ExpenditureMoraleValue[expenditureLevel];
                methodSetField.GetValue(new object[] { ___MoraleValueField, string.Format("{0}{1}", num1 > 0 ? (object)"+" : (object)"", (object)num1) });
                if (showMoraleChange)
                {
                    int morale = ___simState.Morale;
                    ___MoralBar.ShowMoraleChange(morale, morale + num1);
                }
                else
                    ___MoralBar.ShowCurrentMorale();
                Traverse.Create(__instance).Method("ClearListLineItems", new object[] { ___SectionOneExpensesList }).GetValue();
                List<KeyValuePair<string, int>> keyValuePairList = new List<KeyValuePair<string, int>>();
                int ongoingUpgradeCosts = 0;
                string key = ___simState.CurDropship == DropshipType.Leopard ? Strings.T("Bank Loan Interest Payment") : Strings.T("Argo Operating Costs");
                int num2 = Mathf.RoundToInt(expenditureCostModifier * (float)___simState.GetShipBaseMaintenanceCost());
                keyValuePairList.Add(new KeyValuePair<string, int>(key, num2));
                foreach (ShipModuleUpgrade shipUpgrade in ___simState.ShipUpgrades)
                {
                    if (___simState.CurDropship == DropshipType.Argo && Mathf.CeilToInt((float)shipUpgrade.AdditionalCost * ___simState.Constants.CareerMode.ArgoMaintenanceMultiplier) > 0)
                    {
                        string name = shipUpgrade.Description.Name;
                        int num3 = Mathf.RoundToInt(expenditureCostModifier * (float)Mathf.CeilToInt((float)shipUpgrade.AdditionalCost * ___simState.Constants.CareerMode.ArgoMaintenanceMultiplier));
                        keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
                    }
                }
                foreach (MechDef mechDef in ___simState.ActiveMechs.Values)
                {
                    string name = mechDef.Name;
                    int num3 = Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                    if (settings.CostByTons)
                    {
                        num3 = Mathf.RoundToInt(expenditureCostModifier * (float)mechDef.Chassis.Tonnage * settings.cbillsPerTon);
                        if (settings.TonsAdditive)
                            num3 += Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                    }                    
                    keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
                }
                keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>)((a, b) => b.Value.CompareTo(a.Value)));
                keyValuePairList.ForEach((Action<KeyValuePair<string, int>>)(entry =>
                {
                    ongoingUpgradeCosts += entry.Value;
                    methodAddLineItem.Invoke(__instance, new object[] { ___SectionOneExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value) });
                }));
                methodSetField.GetValue(new object[] { ___SectionOneExpensesField, SimGameState.GetCBillString(ongoingUpgradeCosts) });
                keyValuePairList.Clear();
                Traverse.Create(__instance).Method("ClearListLineItems", new object[] { ___SectionTwoExpensesList }).GetValue();
                int ongoingMechWariorCosts = 0;
                foreach (Pilot pilot in ___simState.PilotRoster)
                {
                    string displayName = pilot.pilotDef.Description.DisplayName;
                    int num3 = Mathf.CeilToInt(expenditureCostModifier * (float)___simState.GetMechWarriorValue(pilot.pilotDef));
                    keyValuePairList.Add(new KeyValuePair<string, int>(displayName, num3));
                }
                keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>)((a, b) => b.Value.CompareTo(a.Value)));
                keyValuePairList.ForEach((Action<KeyValuePair<string, int>>)(entry =>
                {
                    ongoingMechWariorCosts += entry.Value;
                    methodAddLineItem.Invoke(__instance, new object[] { ___SectionTwoExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value) });
                }));
                methodSetField.GetValue(new object[] { ___SectionTwoExpensesField, SimGameState.GetCBillString(ongoingMechWariorCosts) });
                methodSetField.GetValue(new object[] { ___EndOfQuarterFunds, SimGameState.GetCBillString(___simState.Funds + ___simState.GetExpenditures(false)) });
                methodSetField.GetValue(new object[] { ___QuarterOperatingExpenses, SimGameState.GetCBillString(___simState.GetExpenditures(false)) });
                methodSetField.GetValue(new object[] { ___CurrentFunds, SimGameState.GetCBillString(___simState.Funds) });
                int index = 0;
                foreach (KeyValuePair<EconomyScale, int> keyValuePair in ___simState.ExpenditureMoraleValue)
                {
                    ___ExpenditureLvlBtnMoraleFields[index].SetText(string.Format("{0}", (object)keyValuePair.Value), (object[])Array.Empty<object>());
                    ___ExpenditureLvlBtnCostFields[index].SetText(SimGameState.GetCBillString(___simState.GetExpenditures(keyValuePair.Key, false)), (object[])Array.Empty<object>());
                    ++index;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
        [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
        public static class SimGameState_GetExpenditures
        {
            public static bool Prefix(SimGameState __instance, EconomyScale expenditureLevel, bool proRate, int ___ProRateRefund, ref int __result)
            {
                int baseMaintenanceCost = __instance.GetShipBaseMaintenanceCost();
                float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
                for (int index = 0; index < __instance.ShipUpgrades.Count; ++index)
                    baseMaintenanceCost += Mathf.CeilToInt((float)__instance.ShipUpgrades[index].AdditionalCost * __instance.Constants.CareerMode.ArgoMaintenanceMultiplier);
                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    if (settings.CostByTons)
                    {
                        baseMaintenanceCost += Mathf.RoundToInt((float)mechDef.Chassis.Tonnage * settings.cbillsPerTon * expenditureCostModifier);
                        if (settings.TonsAdditive)
                            baseMaintenanceCost += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                    }
                    else
                        baseMaintenanceCost += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                }
                for (int index = 0; index < __instance.PilotRoster.Count; ++index)
                    baseMaintenanceCost += __instance.GetMechWarriorValue(__instance.PilotRoster[index].pilotDef);
                __result = Mathf.CeilToInt((float)(baseMaintenanceCost - (proRate ? ___ProRateRefund : 0)) * expenditureCostModifier);
                return false;
            }
        }

        public class Helper
        {
            public class Logger
            {
                static readonly string filePath = $"{MechMaintenanceByCost.ModDirectory}/Log.txt";
                public static void LogError(Exception ex)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                           "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                        writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                    }
                }

                public static void LogLine(String line)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                        writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                    }
                }
            }
            public static float CalculateCBillValue(MechDef mech)
            {
                float num = 10000f;
                float currentCBillValue = (float)mech.Chassis.Description.Cost;
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
                for (int i = 0; i < mech.Inventory.Length; i++)
                {
                    MechComponentRef mechComponentRef = mech.Inventory[i];
                    currentCBillValue += (float)mechComponentRef.Def.Description.Cost;
                }
                currentCBillValue = Mathf.Round(currentCBillValue / num) * num;
                return currentCBillValue;
            }
        }

        internal class ModSettings
        {
            public float PercentageOfMechCost = 0.003f;

            public bool CostByTons = false;
            public int cbillsPerTon = 500;
            public bool TonsAdditive = false;
        }
    }
}
