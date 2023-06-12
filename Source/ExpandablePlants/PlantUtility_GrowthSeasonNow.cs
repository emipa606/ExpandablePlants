using HarmonyLib;
using RimWorld;
using Verse;

namespace ExpandablePlants;

[HarmonyPatch(typeof(PlantUtility), "GrowthSeasonNow")]
public static class PlantUtility_GrowthSeasonNow
{
    public static bool Prefix(IntVec3 c, Map map, ref bool __result, bool forSowing = false)
    {
        if (!IsJobSearching.isJobSearchingNow)
        {
            return true;
        }

        if (WorkGiver_Grower.wantedPlantDef == null)
        {
            WorkGiver_Grower.wantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
        }

        if (WorkGiver_Grower.wantedPlantDef == null)
        {
            return true;
        }

        var plantProps = WorkGiver_Grower.wantedPlantDef.GetCompProperties<CompProperties_Plant>();
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