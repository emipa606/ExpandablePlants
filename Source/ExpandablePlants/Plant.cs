using System;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExpandablePlants;

internal class Plant : RimWorld.Plant
{
    private const float maxTempSpeedFactor = 1f;

    private static readonly float LeafSpawnRadius = 0.4f;
    private static readonly float LeafSpawnYMin = 0.3f;
    private static readonly float LeafSpawnYMax = 1f;

    private string cachedLabelMouseover;

    private CompPlant compPropertiesPlantExpandable;

    // Temperature ranges.
    public virtual float MinLeaflessTemperature => CompPlant.MinLeaflessTemperature; // -10f
    public new virtual float MaxLeaflessTemperature => CompPlant.MaxLeaflessTemperature; // -2f
    public new virtual float MinGrowthTemperature => CompPlant.MinGrowthTemperature; // 0f
    public new virtual float MinOptimalGrowthTemperature => CompPlant.MinOptimalGrowthTemperature; // 10f
    public new virtual float MaxOptimalGrowthTemperature => CompPlant.MaxOptimalGrowthTemperature; // 42f
    public new virtual float MaxGrowthTemperature => CompPlant.MaxGrowthTemperature; // 58f

    public virtual float MinDieOfHeatTemperature => CompPlant.MinDieOfHeatTemperature;
    public virtual float MaxDieOfHeatTemperature => CompPlant.MaxDieOfHeatTemperature;

    public virtual bool CanDieOfHeat => CompPlant.CanDieOfHeat; // false

    // Resting behaviour.
    public virtual float RestBegins => CompPlant.RestBegins; // 0.8f
    public virtual float RestEnds => CompPlant.RestEnds; // 0.25f

    public CompPlant CompPlant
    {
        get
        {
            if (compPropertiesPlantExpandable == null)
            {
                compPropertiesPlantExpandable = GetComp<CompPlant>();
            }

            return compPropertiesPlantExpandable;
        }
    }

    protected virtual float DieOfHeatTemperatureThresh
    {
        get
        {
            var minDieOfHeatTemperature = MinDieOfHeatTemperature;
            var dieTempRange = MaxDieOfHeatTemperature - minDieOfHeatTemperature;
            return (Math.Abs(this.HashOffset()) * 0.01f % dieTempRange) + minDieOfHeatTemperature;
        }
    }

    protected new virtual float GrowthPerTick
    {
        get
        {
            if (LifeStage != PlantLifeStage.Growing || Resting)
            {
                return 0f;
            }

            var baseGrowthPerTick = 1f / (60000f * def.plant.growDays);
            return baseGrowthPerTick * GrowthRate;
        }
    }

    public override float GrowthRate
    {
        get
        {
            if (Blighted)
            {
                return 0f;
            }

            return GrowthRateFactor_Fertility * GrowthRateFactor_Temperature * GrowthRateFactor_Light;
        }
    }

    public new virtual float GrowthRateFactor_Temperature
    {
        get
        {
            if (!GenTemperature.TryGetTemperatureForCell(Position, Map, out var temperature))
            {
                return maxTempSpeedFactor;
            }

            if (temperature < MinOptimalGrowthTemperature)
            {
                return Mathf.InverseLerp(MinGrowthTemperature, MinOptimalGrowthTemperature, temperature);
            }

            return temperature > MaxOptimalGrowthTemperature
                ? Mathf.InverseLerp(MaxGrowthTemperature, MaxOptimalGrowthTemperature, temperature)
                : maxTempSpeedFactor;
        }
    }

    public override string LabelMouseover
    {
        get
        {
            if (cachedLabelMouseover != null)
            {
                return cachedLabelMouseover;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(def.LabelCap);
            stringBuilder.Append(" (" + "PercentGrowth".Translate(GrowthPercentString));
            if (Dying)
            {
                stringBuilder.Append(", " + "DyingLower".Translate());
            }

            stringBuilder.Append(")");
            cachedLabelMouseover = stringBuilder.ToString();

            return cachedLabelMouseover;
        }
    }

    public new virtual bool HarvestableSoon
    {
        get
        {
            if (HarvestableNow)
            {
                return true;
            }

            if (!def.plant.Harvestable)
            {
                return false;
            }

            var growthRemaining = Mathf.Max(1f - Growth, 0f);
            var daysRemaining = growthRemaining * def.plant.growDays;
            var growthRemainingAtHarvestable = Mathf.Max(1f - def.plant.harvestMinGrowth, 0f);
            var daysRemainingAtHarvestable = growthRemainingAtHarvestable * def.plant.growDays;
            return (daysRemaining <= 10f || daysRemainingAtHarvestable <= 1f) && GrowthRateFactor_Fertility > 0f &&
                   GrowthRateFactor_Temperature > 0f;
        }
    }

    protected override float LeaflessTemperatureThresh
    {
        get
        {
            var leaflessTempRange = MaxLeaflessTemperature - MinLeaflessTemperature;
            return (Math.Abs(this.HashOffset()) * 0.01f % leaflessTempRange) + MinLeaflessTemperature;
        }
    }

    protected override bool Resting
    {
        get
        {
            if (RestBegins == RestEnds)
            {
                return false;
            }

            var time = GenLocalDate.DayPercent(this);
            if (RestBegins > RestEnds)
            {
                return time < RestEnds || time > RestBegins;
            }

            return time > RestBegins && time < RestEnds;
        }
    }

    protected virtual void CheckTemperatureDieOfHeat()
    {
        if (!CanDieOfHeat)
        {
            return;
        }

        if (AmbientTemperature < DieOfHeatTemperatureThresh)
        {
            return;
        }

        // Send message for crops. No message for wild plants.
        if (IsCrop)
        {
            if (MessagesRepeatAvoider.MessageShowAllowed($"MessagePlantDiedOfHeat-{def.defName}", 240f))
            {
                string messageString =
                    "MessagePlantDiedOfHeat".Translate(GetCustomLabelNoCount(false)).CapitalizeFirst();
                Messages.Message(messageString, new TargetInfo(Position, Map), MessageTypeDefOf.NegativeEvent);
            }
        }

        TakeDamage(new DamageInfo(DamageDefOf.Rotting, 99999f));
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        if (LifeStage == PlantLifeStage.Growing)
        {
            stringBuilder.AppendLine("PercentGrowth".Translate(GrowthPercentString));
            stringBuilder.AppendLine("GrowthRate".Translate() + ": " + GrowthRate.ToStringPercent());
            if (!Blighted)
            {
                if (Resting)
                {
                    stringBuilder.AppendLine("PlantResting".Translate());
                }

                if (!HasEnoughLightToGrow)
                {
                    stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " +
                                             def.plant.growMinGlow.ToStringPercent());
                }

                var growthRateFactor_Temperature = GrowthRateFactor_Temperature;
                if (growthRateFactor_Temperature < 0.99f)
                {
                    if (growthRateFactor_Temperature < 0.01f)
                    {
                        stringBuilder.AppendLine("OutOfIdealTemperatureRangeNotGrowing".Translate());
                    }
                    else
                    {
                        stringBuilder.AppendLine(
                            "OutOfIdealTemperatureRange".Translate(Mathf.RoundToInt(growthRateFactor_Temperature * 100f)
                                .ToString()));
                    }
                }
            }
        }
        else if (LifeStage == PlantLifeStage.Mature)
        {
            stringBuilder.AppendLine(HarvestableNow ? "ReadyToHarvest".Translate() : "Mature".Translate());
        }

        if (DyingBecauseExposedToLight)
        {
            stringBuilder.AppendLine("DyingBecauseExposedToLight".Translate());
        }

        if (Blighted)
        {
            stringBuilder.AppendLine("Blighted".Translate() + " (" + Blight.Severity.ToStringPercent() + ")");
        }

        return stringBuilder.ToString().TrimEndNewlines();
    }

    public override void PostMapInit()
    {
        base.PostMapInit();
        CheckTemperatureDieOfHeat();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (Current.ProgramState == ProgramState.Playing && !respawningAfterLoad)
        {
            CheckTemperatureDieOfHeat();
        }
    }

    public override void TickLong()
    {
        CheckTemperatureDieOfHeat();
        if (Destroyed)
        {
            return;
        }

        CheckMakeLeafless();
        if (Destroyed)
        {
            return;
        }

        if (GrowthRateFactor_Temperature > 0f)
        {
            // Apply growth. Check for redraw at maturity and every 10% growth.
            var oldGrowthInt = growthInt;
            var wasMature = LifeStage == PlantLifeStage.Mature;

            growthInt += GrowthPerTick * 2000f;
            if (growthInt > 1f)
            {
                growthInt = 1f;
            }

            if ((!wasMature && LifeStage == PlantLifeStage.Mature ||
                 (int)(oldGrowthInt * 10f) != (int)(growthInt * 10f)) && CurrentlyCultivated())
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            }
        }

        if (!HasEnoughLightToGrow)
        {
            unlitTicks += 2000;
        }
        else
        {
            unlitTicks = 0;
        }

        ageInt += 2000;

        if (Dying)
        {
            var map = Map;
            var isCrop = IsCrop;
            var harvestableNow = HarvestableNow;
            var dyingBecauseExposedToLight = DyingBecauseExposedToLight;
            var damageTaken = Mathf.CeilToInt(CurrentDyingDamagePerTick * 2000f);
            TakeDamage(new DamageInfo(DamageDefOf.Rotting, damageTaken));
            if (Destroyed)
            {
                if (!isCrop || !def.plant.Harvestable || !MessagesRepeatAvoider.MessageShowAllowed(
                        $"MessagePlantDiedOfRot-{def.defName}", 240f))
                {
                    return;
                }

                string deathMessage;
                if (harvestableNow)
                {
                    deathMessage = "MessagePlantDiedOfRot_LeftUnharvested";
                }
                else if (dyingBecauseExposedToLight)
                {
                    deathMessage = "MessagePlantDiedOfRot_ExposedToLight";
                }
                else
                {
                    deathMessage = "MessagePlantDiedOfRot";
                }

                Messages.Message(
                    deathMessage.Translate(GetCustomLabelNoCount(false)).CapitalizeFirst(),
                    new TargetInfo(Position, map),
                    MessageTypeDefOf.NegativeEvent
                );

                return;
            }
        }

        ClearCachedLabelMouseover();
        if (!def.plant.dropLeaves)
        {
            return;
        }

        if (MoteMaker.MakeStaticMote(Vector3.zero, Map, ThingDefOf.Mote_Leaf) is not MoteLeaf moteLeaf)
        {
            return;
        }

        var num3 = def.plant.visualSizeRange.LerpThroughRange(growthInt);
        var treeHeight = def.graphicData.drawSize.x * num3;
        var b = Rand.InsideUnitCircleVec3 * LeafSpawnRadius;
        moteLeaf.Initialize(
            Position.ToVector3Shifted() + (Vector3.up * Rand.Range(LeafSpawnYMin, LeafSpawnYMax)) + b +
            (Vector3.forward * def.graphicData.shadowData.offset.z), Rand.Value * 2000.TicksToSeconds(),
            b.z > 0f, treeHeight);
    }

    public void ClearCachedLabelMouseover()
    {
        cachedLabelMouseover = null;
    }
}