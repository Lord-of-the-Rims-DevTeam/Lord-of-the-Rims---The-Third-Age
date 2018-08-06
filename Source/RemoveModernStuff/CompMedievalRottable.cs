using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheThirdAge
{
    public class CompMedievalRottable : CompRottable
    {

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            switch (base.Stage)
            {
                case RotStage.Fresh:
                    sb.AppendLine("RotStateFresh".Translate());
                    break;
                case RotStage.Rotting:
                    sb.AppendLine("RotStateRotting".Translate());
                    break;
                case RotStage.Dessicated:
                    sb.AppendLine("RotStateDessicated".Translate());
                    break;
            }
            float num = (float)this.PropsRot.TicksToRotStart - base.RotProgress;
            if (num > 0f)
            {
                float num2 = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.Map);
                List<Thing> thingList = GridsUtility.GetThingList(this.parent.PositionHeld, this.parent.Map);
                var factor = 1f;
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is Building_RottableFixer)
                    {
                        var b = thingList[i] as Building_RottableFixer;
                        var isMeat = this?.parent?.def.IsMeat ?? false;
                        if (!isMeat && b.def.defName == "LotR_PantryShelf")
                        {
                            factor = 3f;
                        }
                        if (b.def.defName == "LotR_SaltBarrel")
                        {
                            factor = 15f;
                        }                           
                        //num2 = building_Refrigerator.CurrentTemp;
                        break;
                    }
                }
                num2 = (float)Mathf.RoundToInt(num2);
                float num3 = GenTemperature.RotRateAtTemperature(num2);
                int ticksUntilRotAtCurrentTemp = (int)(base.TicksUntilRotAtCurrentTemp * factor);
                if (num3 < 0.001f)
                {
                    sb.Append("CurrentlyFrozen".Translate() + ".");
                }
                else
                {
                    if (num3 < 0.999f)
                    {
                        sb.Append("CurrentlyRefrigerated".Translate(new object[]
                        {
                            ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()
                        }) + ".");
                    }
                    else
                    {
                        sb.Append("NotRefrigerated".Translate(new object[]
                        {
                            ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()
                        }) + ".");
                    }
                }
            }
            return sb.ToString().TrimEndNewlines();
        }

        public override void CompTickRare()
        {
            
            if (this.parent.MapHeld != null && this.parent.Map != null)
            {
                HashSet<Thing> list = new HashSet<Thing>(this.parent.MapHeld.thingGrid.ThingsListAtFast(this.parent.PositionHeld));
                var isMeat = this?.parent?.def.IsMeat ?? false;
                var isSalted = this?.parent?.def?.defName?.ToLowerInvariant()?.Contains("salted") ?? false;
                var pantryShelf = list.FirstOrDefault(x => x is Building_RottableFixer && x.def.defName == "LotR_PantryShelf");
                var saltPot = list.FirstOrDefault(x => x is Building_RottableFixer && x.def.defName == "LotR_SaltBarrel");
                if (isMeat && !isSalted && saltPot != null)
                {
                    HandleMeatThatNeedsSalting(this.parent);
                    return;
                }
                
                float rotProgress = this.RotProgress;
                float num = 1f;
                float temperatureForCell = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.MapHeld);
                var b = list.FirstOrDefault(x => x is Building_RottableFixer);
                if (b != null)
                {
                    if (!isMeat && b.def.defName == "LotR_PantryShelf")
                    {
                        num = 0.3f;
                    }
                    if (b.def.defName == "LotR_SaltBarrel")
                    {
                        num = 0.0666667f;
                    }   
                }

                num *= GenTemperature.RotRateAtTemperature(temperatureForCell);
                this.RotProgress += Mathf.Round(num * 250f);
                if (this.Stage == RotStage.Rotting && this.PropsRot.rotDestroys)
                {
                    if (this.parent.Position.GetSlotGroup(this.parent.Map) != null)
                    {
                        Messages.Message("MessageRottedAwayInStorage".Translate(new object[]
                        {
                this.parent.Label
                        }).CapitalizeFirst(), MessageTypeDefOf.SilentInput);
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, OpportunityType.GoodToKnow);
                    }
                    this.parent.Destroy(DestroyMode.Vanish);
                    return;
                }
                if (Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(this.RotProgress / 60000f))
                {
                    if (this.Stage == RotStage.Rotting && this.PropsRot.rotDamagePerDay > 0f)
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, (float)GenMath.RoundRandom(this.PropsRot.rotDamagePerDay), 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
                    }
                    else if (this.Stage == RotStage.Dessicated && this.PropsRot.dessicatedDamagePerDay > 0f && this.ShouldTakeDessicateDamage())
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, (float)GenMath.RoundRandom(this.PropsRot.dessicatedDamagePerDay), 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
                    }
                }
            }
        }

        private void HandleMeatThatNeedsSalting(ThingWithComps meat)
        {
            var count = meat.stackCount;
            float curHP = meat.HitPoints;
            var curDefName = meat.def.defName;
            var curPosition = meat.PositionHeld;
            var curMap = meat.MapHeld;
            this.parent.Destroy(DestroyMode.Vanish);
            var newThing = (ThingWithComps) ThingMaker.MakeThing(ThingDef.Named(curDefName + "Salted"));
            newThing.stackCount = count;
            newThing.HitPoints = Mathf.RoundToInt(curHP / meat.GetStatValue(StatDefOf.MaxHitPoints) * newThing.MaxHitPoints); // curHP;
            GenPlace.TryPlaceThing(newThing, curPosition, curMap, ThingPlaceMode.Direct);
        }

        private bool ShouldTakeDessicateDamage()
        {
            if (this.parent.ParentHolder != null)
            {
                Thing thing = this.parent.ParentHolder as Thing;
                if (thing != null && thing.def.category == ThingCategory.Building && thing.def.building.preventDeteriorationInside)
                {
                    return false;
                }
            }
            return true;
        }

        private void StageChanged()
        {
            Corpse corpse = this.parent as Corpse;
            if (corpse != null)
            {
                corpse.RotStageChanged();
            }
        }
    }
}
