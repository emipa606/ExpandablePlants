using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ExpandablePlants;

[HarmonyPatch(typeof(PlantProperties), "SpecialDisplayStats")]
public static class PlantProperties_SpecialDisplayStats
{
    public static string OldMinGrowthTemperatureEntryLabel => "MinGrowthTemperature".Translate().CapitalizeFirst();

    public static string OldMaxGrowthTemperatureEntryLabel => "MaxGrowthTemperature".Translate().CapitalizeFirst();
    // Keep all StatDrawEntries except those with the growth temperature labels.

    private static void Postfix(ref IEnumerable<StatDrawEntry> __result)
    {
        __result = __result.Where(entry => !entry.LabelCap.Equals(OldMinGrowthTemperatureEntryLabel) &&
                                           !entry.LabelCap.Equals(OldMaxGrowthTemperatureEntryLabel));
    }
}