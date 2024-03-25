using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ExpandablePlants;

[HarmonyPatch(typeof(PlantUtility), "GrowthSeasonNow")]
public static class PlantUtility_GrowthSeasonNow
{
    private static readonly FieldInfo wantedPlantDefField =
        AccessTools.Field(typeof(WorkGiver_Grower), "wantedPlantDef");

    public static bool Prefix(IntVec3 c, Map map, ref bool __result, bool forSowing = false)
    {
        if (!IsJobSearching.isJobSearchingNow)
        {
            return true;
        }

        if (wantedPlantDefField.GetValue(null) == null)
        {
            wantedPlantDefField.SetValue(null, WorkGiver_Grower.CalculateWantedPlantDef(c, map));
        }

        var wantedPlantDef = wantedPlantDefField.GetValue(null);

        if (wantedPlantDef is not ThingDef plantDef)
        {
            return true;
        }

        var plantProps = plantDef.GetCompProperties<CompProperties_Plant>();
        if (plantProps == null)
        {
            return true;
        }

        if (!GenTemperature.TryGetTemperatureForCell(c, map, out var temperature))
        {
            return true;
        }

        __result = temperature >= plantProps.minGrowthTemperature && temperature <= plantProps.maxGrowthTemperature;
        return false;
    }
}