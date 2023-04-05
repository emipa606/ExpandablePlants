using RimWorld;
using Verse;
using Verse.AI;

namespace ExpandablePlants;

internal class WorkGiver_GrowerSow : WorkGiver_Grower
{
    protected static string CantSowCavePlantBecauseOfLightTrans;
    protected static string CantSowCavePlantBecauseUnroofedTrans;

    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public static void ResetStaticData()
    {
        CantSowCavePlantBecauseOfLightTrans = "CantSowCavePlantBecauseOfLight".Translate();
        CantSowCavePlantBecauseUnroofedTrans = "CantSowCavePlantBecauseUnroofed".Translate();
    }

    protected override bool ExtraRequirements(IPlantToGrowSettable settable, Pawn pawn)
    {
        if (!settable.CanAcceptSowNow())
        {
            return false;
        }

        if (settable is Zone_Growing { allowSow: false })
        {
            return false;
        }

        wantedPlantDef = settable.GetPlantDefToGrow();
        return wantedPlantDef != null;
    }

    public override Job JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
    {
        var map = pawn.Map;

        // Check sowing allowed.
        if (c.IsForbidden(pawn))
        {
            return null;
        }

        if (wantedPlantDef == null)
        {
            wantedPlantDef = CalculateWantedPlantDef(c, map);
            if (wantedPlantDef == null)
            {
                return null;
            }
        }

        // Check temperature.
        var plantProps = wantedPlantDef.GetCompProperties<CompProperties_Plant>();
        if (plantProps == null)
        {
            if (!PlantUtility.GrowthSeasonNow(c, map, true))
            {
                return null;
            }
        }
        else
        {
            if (GenTemperature.TryGetTemperatureForCell(c, map, out var temperature))
            {
                if (temperature < plantProps.minGrowthTemperature || temperature > plantProps.maxGrowthTemperature)
                {
                    return null;
                }
            }
        }

        // Check tile entities present. Disallow growing if same plant or non-growable building planned.
        var thingList = c.GetThingList(map);
        var zone_Growing = c.GetZone(map) as Zone_Growing;
        var ownBlueprintOrFramePresent = false;
        foreach (var thing in thingList)
        {
            if (thing.def == wantedPlantDef)
            {
                return null;
            }

            if (thing is Blueprint or Frame && thing.Faction == pawn.Faction)
            {
                ownBlueprintOrFramePresent = true;
            }
        }

        if (ownBlueprintOrFramePresent)
        {
            Thing edifice = c.GetEdifice(map);
            if (edifice == null || edifice.def.fertility < 0f)
            {
                return null;
            }
        }

        // Check roof and light for cave plants.
        if (wantedPlantDef.plant.cavePlant)
        {
            if (!c.Roofed(map))
            {
                JobFailReason.Is(CantSowCavePlantBecauseUnroofedTrans);
                return null;
            }

            if (map.glowGrid.GameGlowAt(c, true) > 0f)
            {
                JobFailReason.Is(CantSowCavePlantBecauseOfLightTrans);
                return null;
            }
        }

        // Don't grow trees indoors.
        if (wantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
        {
            return null;
        }

        // Cut existing plants.
        var existingPlant = c.GetPlant(map);
        if (existingPlant != null && existingPlant.def.plant.blockAdjacentSow)
        {
            if (!pawn.CanReserve(existingPlant, 1, -1, null, forced) || existingPlant.IsForbidden(pawn))
            {
                return null;
            }

            if (zone_Growing is { allowCut: false })
            {
                return null;
            }

            return !PlantUtility.PawnWillingToCutPlant_Job(existingPlant, pawn)
                ? null
                : JobMaker.MakeJob(JobDefOf.CutPlant, existingPlant);
        }

        // Check adjacent blocker.
        var adjacentSowBlocker = PlantUtility.AdjacentSowBlocker(wantedPlantDef, c, map);
        if (adjacentSowBlocker != null)
        {
            if (adjacentSowBlocker is not Plant adjacentPlant ||
                !pawn.CanReserveAndReach(adjacentPlant, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced) ||
                adjacentPlant.IsForbidden(pawn))
            {
                return null;
            }

            var plantToGrowSettable = adjacentPlant.Position.GetPlantToGrowSettable(adjacentPlant.Map);
            if (plantToGrowSettable != null && plantToGrowSettable.GetPlantDefToGrow() == adjacentPlant.def)
            {
                return null;
            }

            var adjacentPlantZone = adjacentPlant.Position.GetZone(map) as Zone_Growing;
            if (zone_Growing is { allowCut: false } ||
                adjacentPlantZone is { allowCut: false })
            {
                return null;
            }

            return !PlantUtility.PawnWillingToCutPlant_Job(adjacentPlant, pawn)
                ? null
                : JobMaker.MakeJob(JobDefOf.CutPlant, adjacentPlant);
        }

        // Check skill level.
        if (wantedPlantDef.plant.sowMinSkill > 0 && pawn.skills != null &&
            pawn.skills.GetSkill(SkillDefOf.Plants).Level < wantedPlantDef.plant.sowMinSkill)
        {
            JobFailReason.Is("UnderAllowedSkill".Translate(wantedPlantDef.plant.sowMinSkill), def.label);
            return null;
        }

        // Check things at site that could block planting. Remove if possible.
        var j = 0;
        while (j < thingList.Count)
        {
            var thing = thingList[j];
            if (thing.def.BlocksPlanting())
            {
                if (!pawn.CanReserve(thing, 1, -1, null, forced))
                {
                    return null;
                }

                if (thing.def.category != ThingCategory.Plant)
                {
                    return thing.def.EverHaulable ? HaulAIUtility.HaulAsideJobFor(pawn, thing) : null;
                }

                if (thing.IsForbidden(pawn) || zone_Growing is { allowCut: false })
                {
                    return null;
                }

                return !PlantUtility.PawnWillingToCutPlant_Job(thing, pawn)
                    ? null
                    : JobMaker.MakeJob(JobDefOf.CutPlant, thing);
            }

            j++;
        }

        // Check if location still blocked or fertility insufficient.
        if (!wantedPlantDef.CanNowPlantAt(c, map) || !pawn.CanReserve(c, 1, -1, null, forced))
        {
            return null;
        }

        // Return sowing job.
        var job = JobMaker.MakeJob(JobDefOf.Sow, c);
        job.plantDefToSow = wantedPlantDef;
        return job;
    }
}