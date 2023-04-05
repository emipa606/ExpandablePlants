using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ExpandablePlants;

[StaticConstructorOnStartup]
internal static class HarmonyPatches
{
    static HarmonyPatches()
    {
        var harmony = new Harmony("eagle0600.expandablePlants");
        harmony.Patch(AccessTools.Method(typeof(PlantProperties), "SpecialDisplayStats"),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(RemoveConstantGrowthTemperatureDisplay)));
        harmony.Patch(AccessTools.Method(typeof(ThingDef), "SpecialDisplayStats"),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AddTrueGrowthTemperatureDisplay)));
    }

    public static string OldMinGrowthTemperatureEntryLabel => "MinGrowthTemperature".Translate().CapitalizeFirst();
    public static string OldMaxGrowthTemperatureEntryLabel => "MaxGrowthTemperature".Translate().CapitalizeFirst();

    // Keep all StatDrawEntries except those with the growth temperature labels.
    private static void RemoveConstantGrowthTemperatureDisplay(ref IEnumerable<StatDrawEntry> __result)
    {
        __result = __result.Where(entry => !entry.LabelCap.Equals(OldMinGrowthTemperatureEntryLabel) &&
                                           !entry.LabelCap.Equals(OldMaxGrowthTemperatureEntryLabel));
    }

    // Add new growth temperature labels based on the true growth temperature of the plant.
    private static void AddTrueGrowthTemperatureDisplay(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
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
            minGrowthTemperature = RimWorld.Plant.MinGrowthTemperature;
            maxGrowthTemperature = RimWorld.Plant.MaxGrowthTemperature;
        }
        else
        {
            // ExpandablePlants plants get their growth temperatures from the component properties.
            minGrowthTemperature = compProperties_Plant.minGrowthTemperature;
            maxGrowthTemperature = compProperties_Plant.maxGrowthTemperature;
        }

        __result = __result.Concat(new[]
        {
            new StatDrawEntry(StatCategoryDefOf.Basics, "MinGrowthTemperature".Translate(),
                minGrowthTemperature.ToStringTemperature(),
                "Stat_Thing_Plant_MinGrowthTemperature_Desc".Translate(), 4152),
            new StatDrawEntry(StatCategoryDefOf.Basics, "MaxGrowthTemperature".Translate(),
                maxGrowthTemperature.ToStringTemperature(),
                "Stat_Thing_Plant_MaxGrowthTemperature_Desc".Translate(), 4153)
        });
    }
}