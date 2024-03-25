using Verse;

namespace ExpandablePlants;

internal class CompProperties_Plant : CompProperties
{
    public readonly bool canDieOfHeat = false;
    public readonly float maxDieOfHeatTemperature = 0f;
    public readonly float maxGrowthTemperature = 58f;
    public readonly float maxLeaflessTemperature = -2f;
    public readonly float maxOptimalGrowthTemperature = 42f;

    // Do not use these unless canDieOfHeat is true.
    public readonly float minDieOfHeatTemperature = 0f;
    public readonly float minGrowthTemperature = 0f;

    public readonly float minLeaflessTemperature = -10f;
    public readonly float minOptimalGrowthTemperature = 10f;

    public readonly float restBegins = 0.8f;
    public readonly float restEnds = 0.25f;

    public CompProperties_Plant()
    {
        compClass = typeof(CompPlant);
    }
}