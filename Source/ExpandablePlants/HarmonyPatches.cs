using System.Reflection;
using HarmonyLib;
using Verse;

namespace ExpandablePlants;

[StaticConstructorOnStartup]
internal static class HarmonyPatches
{
    static HarmonyPatches()
    {
        new Harmony("eagle0600.expandablePlants").PatchAll(Assembly.GetExecutingAssembly());
    }
}