using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ExpandablePlants;

[HarmonyPatch(typeof(ThingDef), "SpecialDisplayStats")]
public static class ThingDef_SpecialDisplayStats
{
    // Add new growth temperature labels based on the true growth temperature of the plant.
    private static void Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
    {
        // Skip non-plants.
        if (__instance.plant == null)
        {
            return;
        }

        float minGrowthTemperature;
        float maxGrowthTemperature;

        // Determine whether this is an ExpandablePlants plant or a regular plant.
        var compProperties_Plant = __instance.GetCompProperties<CompProperties_Plant>();
        if (compProperties_Plant == null)
        {
            // Regular plants use RimWorld's constant growth temperatures.
            minGrowthTemperature = RimWorld.Plant.DefaultMinGrowthTemperature;
            maxGrowthTemperature = RimWorld.Plant.DefaultMaxGrowthTemperature;
        }
        else
        {
            // ExpandablePlants plants get their growth temperatures from the component properties.
            minGrowthTemperature = compProperties_Plant.minGrowthTemperature;
            maxGrowthTemperature = compProperties_Plant.maxGrowthTemperature;
        }

        __result = __result.Concat([
            new StatDrawEntry(StatCategoryDefOf.Basics, "MinGrowthTemperature".Translate(),
                minGrowthTemperature.ToStringTemperature(),
                "Stat_Thing_Plant_MinGrowthTemperature_Desc".Translate(), 4152),
            new StatDrawEntry(StatCategoryDefOf.Basics, "MaxGrowthTemperature".Translate(),
                maxGrowthTemperature.ToStringTemperature(),
                "Stat_Thing_Plant_MaxGrowthTemperature_Desc".Translate(), 4153)
        ]);
    }
}