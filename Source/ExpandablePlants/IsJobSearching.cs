using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace ExpandablePlants;

[HarmonyPatch]
public static class IsJobSearching
{
    public static bool IsJobSearchingNow;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(WorkGiver_GrowerSow), nameof(WorkGiver_GrowerSow.JobOnCell));
        yield return AccessTools.Method(typeof(Building_PlantGrower), nameof(Building_PlantGrower.GetInspectString));
        yield return AccessTools.Method(typeof(Zone_Growing), nameof(Zone_Growing.GetInspectString));
    }

    public static void Prefix()
    {
        IsJobSearchingNow = true;
    }

    public static void Postfix()
    {
        IsJobSearchingNow = false;
    }
}