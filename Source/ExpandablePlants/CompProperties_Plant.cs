using Verse;

namespace ExpandablePlants;

internal class CompProperties_Plant : CompProperties
{
    public bool canDieOfHeat = false;
    public float maxDieOfHeatTemperature = 0f;
    public float maxGrowthTemperature = 58f;
    public float maxLeaflessTemperature = -2f;
    public float maxOptimalGrowthTemperature = 42f;

    // Do not use these unless canDieOfHeat is true.
    public float minDieOfHeatTemperature = 0f;
    public float minGrowthTemperature = 0f;

    public float minLeaflessTemperature = -10f;
    public float minOptimalGrowthTemperature = 10f;

    public float restBegins = 0.8f;
    public float restEnds = 0.25f;

    public CompProperties_Plant()
    {
        compClass = typeof(CompPlant);
    }
}