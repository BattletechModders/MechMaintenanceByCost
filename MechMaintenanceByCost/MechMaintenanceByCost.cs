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
            private static void FilterListItems(Transform container, SimGameState simState, List<string> mechNames, List<KeyValuePair<string, int>> keyValuePairList)
            {
                List<GameObject> list = new List<GameObject>();
                System.Collections.IEnumerator enumerator = container.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    GameObject gameObject = transform.gameObject;
                    list.Add(gameObject);
                }

                foreach (GameObject gameObject in list)
                {
                    SGKeyValueView component = gameObject.GetComponent<SGKeyValueView>();
                    string key = Traverse.Create(component).Field("Key").GetValue<LocalizableText>().OriginalText;
                    string valueString = Traverse.Create(component).Field("Value").GetValue<LocalizableText>().OriginalText;
                    valueString = valueString.Replace("¢", "").Replace(",", "");
                    int value = int.Parse(valueString);
                    if (!mechNames.Contains(key))
                        keyValuePairList.Add(new KeyValuePair<string, int>(key, value));
                    
                    simState.DataManager.PoolGameObject("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element", gameObject);
                }

            }
            static void Postfix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, SimGameState ___simState,
                Transform ___SectionOneExpensesList, LocalizableText ___SectionOneExpensesField)
            {
                try
                {
                    List<KeyValuePair<string, int>> keyValuePairList = new List<KeyValuePair<string, int>>();
                    float expenditureCostModifier = ___simState.GetExpenditureCostModifier(expenditureLevel);
                    string sectionOneExpenses = ___SectionOneExpensesField.OriginalText;
                    sectionOneExpenses = sectionOneExpenses.Replace("¢", "").Replace(",", "");
                    int ongoingUpgradeCosts = int.Parse(sectionOneExpenses);

                    List<string> mechNames = new List<string>();
                    foreach (MechDef mechDef in ___simState.ActiveMechs.Values)
                    {
                        string key = mechDef.Name;
                        mechNames.Add(key);
                        int value = Mathf.RoundToInt(expenditureCostModifier * (float)___simState.Constants.Finances.MechCostPerQuarter);
                        ongoingUpgradeCosts -= value;
                        if (settings.CostByTons)
                        {
                            value = Mathf.RoundToInt(expenditureCostModifier * (float)mechDef.Chassis.Tonnage * settings.cbillsPerTon);
                            if (settings.TonsAdditive)
                                value += Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);
                        }
                        else
                            value = Mathf.RoundToInt(expenditureCostModifier * Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost);

                        ongoingUpgradeCosts += value;
                        keyValuePairList.Add(new KeyValuePair<string, int>(key, value));
                    }
                    FilterListItems(___SectionOneExpensesList, ___simState, mechNames, keyValuePairList);
                    keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>)((a, b) => b.Value.CompareTo(a.Value)));
                    keyValuePairList.ForEach((Action<KeyValuePair<string, int>>)(entry =>
                    {
                        Traverse.Create(__instance).Method("AddListLineItem", new Type[] { typeof(Transform), typeof(string), typeof(string) }).GetValue(
                            new object[] { ___SectionOneExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value) });
                    }));                  
                    Traverse.Create(__instance).Method("SetField", new Type[] { typeof(LocalizableText), typeof(string) }).GetValue(
                        new object[] { ___SectionOneExpensesField, SimGameState.GetCBillString(ongoingUpgradeCosts) });
                }
                catch (Exception e)
                {
                    Helper.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
        [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
        public static class SimGameState_GetExpenditures
        {
            public static void Postfix(SimGameState __instance, EconomyScale expenditureLevel, ref int __result)
            {

                FinancesConstantsDef finances = __instance.Constants.Finances;
                float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
                int baseMaintenanceCost = __result;

                foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                {
                    baseMaintenanceCost -= finances.MechCostPerQuarter;
                    if (settings.CostByTons)
                    {
                        baseMaintenanceCost += Mathf.RoundToInt((float)mechDef.Chassis.Tonnage * settings.cbillsPerTon * expenditureCostModifier);
                        if (settings.TonsAdditive)
                            baseMaintenanceCost += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                    }
                    else
                        baseMaintenanceCost += Mathf.RoundToInt(Helper.CalculateCBillValue(mechDef) * settings.PercentageOfMechCost * expenditureCostModifier);
                }
                __result = baseMaintenanceCost;
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
                        writer.WriteLine(Environment.NewLine + "-------------------------------------------------------------" + Environment.NewLine);
                    }
                }

                public static void LogLine(String line)
                {
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                        writer.WriteLine(Environment.NewLine + "--------------------------------------------------------------" + Environment.NewLine);
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
