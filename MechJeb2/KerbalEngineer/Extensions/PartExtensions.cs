// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

namespace KerbalEngineer.Extensions
{
    using System;
    using System.Collections.Generic;
    using CompoundParts;

    public static class PartExtensions
    {
        //private static Part cachePart;
        //private static PartModule cachePartModule;
        //private static PartResource cachePartResource;

        /// <summary>
        ///     Gets whether the part contains a specific resource.
        /// </summary>
        public static bool ContainsResource(this Part part, int resourceId)
        {
            return part.Resources.Contains(resourceId);
        }

        /// <summary>
        ///     Gets whether the part contains resources.
        /// </summary>
        public static bool ContainsResources(this Part part)
        {
            for (int i = 0; i < part.Resources.list.Count; ++i)
            {
                if (part.Resources.list[i].amount > 0.0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Gets whether the part has fuel.
        /// </summary>
        public static bool EngineHasFuel(this Part part)
        {
            PartModule cachePartModule = GetModule<ModuleEngines>(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEngines).getFlameoutState;
            }

            cachePartModule = GetModuleMultiModeEngine(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEnginesFX).getFlameoutState;
            }

            return false;
        }

        /// <summary>
        ///     Gets the cost of the part excluding resources.
        /// </summary>
        public static double GetCostDry(this Part part)
        {
            return part.partInfo.cost - GetResourceCostMax(part) + part.GetModuleCosts(0.0f);
        }

        /// <summary>
        ///     Gets the cost of the part including maximum resources.
        /// </summary>
        public static double GetCostMax(this Part part)
        {
            return part.partInfo.cost + part.GetModuleCosts(0.0f);
        }

        /// <summary>
        ///     Gets the cost of the part modules
        ///     Same as stock but without mem allocation
        /// </summary>
        public static double GetModuleCostsNoAlloc(this Part part, float defaultCost)
        {
            float cost = 0f;
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                if (pm is IPartCostModifier)
                    cost += (pm as IPartCostModifier).GetModuleCost(defaultCost);
            }
            return cost;
        }

        /// <summary>
        ///     Gets the cost of the part including resources.
        /// </summary>
        public static double GetCostWet(this Part part)
        {
            return part.partInfo.cost - GetResourceCostInverted(part) + part.GetModuleCostsNoAlloc(0.0f); // part.GetModuleCosts allocate 44B per call. 
        }

        /// <summary>
        ///     Gets the dry mass of the part.
        /// </summary>
        public static double GetDryMass(this Part part)
        {
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass : 0d;
        }

        /// <summary>
        ///     Gets the maximum thrust of the part if it's an engine.
        /// </summary>
        public static double GetMaxThrust(this Part part)
        {
            PartModule cachePartModule = GetModule<ModuleEngines>(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEngines).maxThrust;
            }

            cachePartModule = GetModuleMultiModeEngine(part) ?? GetModule<ModuleEnginesFX>(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEnginesFX).maxThrust;
            }

            return 0.0;
        }

        /// <summary>
        ///     Gets the first typed PartModule in the part's module list.
        /// </summary>
        public static T GetModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                if (pm is T)
                    return (T)pm;
            }
            return null;
        }

        /// <summary>
        ///     Gets a typed PartModule.
        /// </summary>
        public static T GetModule<T>(this Part part, string className) where T : PartModule
        {
            return part.Modules[className] as T;
        }

        /// <summary>
        ///     Gets a typed PartModule.
        /// </summary>
        public static T GetModule<T>(this Part part, int classId) where T : PartModule
        {
            return part.Modules[classId] as T;
        }

        /// <summary>
        ///     Gets a ModuleAlternator typed PartModule.
        /// </summary>
        public static ModuleAlternator GetModuleAlternator(this Part part)
        {
            return GetModule<ModuleAlternator>(part);
        }

        /// <summary>
        ///     Gets a ModuleDeployableSolarPanel typed PartModule.
        /// </summary>
        public static ModuleDeployableSolarPanel GetModuleDeployableSolarPanel(this Part part)
        {
            return GetModule<ModuleDeployableSolarPanel>(part);
        }

        /// <summary>
        ///     Gets a ModuleEngines typed PartModule.
        /// </summary>
        public static ModuleEngines GetModuleEngines(this Part part)
        {
            return GetModule<ModuleEngines>(part);
        }

        public static ModuleEnginesFX GetModuleEnginesFx(this Part part)
        {
            return GetModule<ModuleEnginesFX>(part);
        }

        /// <summary>
        ///     Gets a ModuleGenerator typed PartModule.
        /// </summary>
        public static ModuleGenerator GetModuleGenerator(this Part part)
        {
            return GetModule<ModuleGenerator>(part);
        }

        /// <summary>
        ///     Gets a ModuleGimbal typed PartModule.
        /// </summary>
        public static ModuleGimbal GetModuleGimbal(this Part part)
        {
            return GetModule<ModuleGimbal>(part);
        }

        /// <summary>
        ///     Gets the current selected ModuleEnginesFX.
        /// </summary>
        public static ModuleEnginesFX GetModuleMultiModeEngine(this Part part)
        {
            ModuleEnginesFX moduleEngineFx;
            string mode = GetModule<MultiModeEngine>(part).mode;
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                moduleEngineFx = part.Modules[i] as ModuleEnginesFX;
                if (moduleEngineFx != null && moduleEngineFx.engineID == mode)
                {
                    return moduleEngineFx;
                }
            }
            return null;
        }

        /// <summary>
        ///     Gets a ModuleParachute typed PartModule.
        /// </summary>
        public static ModuleParachute GetModuleParachute(this Part part)
        {
            return GetModule<ModuleParachute>(part);
        }

        public static ModuleRCS GetModuleRcs(this Part part)
        {
            return GetModule<ModuleRCS>(part);
        }

        /// <summary>
        ///     Gets a typed list of PartModules.
        /// </summary>
        public static List<T> GetModules<T>(this Part part) where T : PartModule
        {
            List<T> list = new List<T>();
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                T module = part.Modules[i] as T;
                if (module != null)
                {
                    list.Add(module);
                }
            }
            return list;
        }

        public static ProtoModuleDecoupler GetProtoModuleDecoupler(this Part part)
        {
            PartModule cachePartModule = GetModule<ModuleDecouple>(part);
            if (cachePartModule == null)
            {
                cachePartModule = GetModule<ModuleAnchoredDecoupler>(part);
            }
            if (cachePartModule != null)
            {
                return new ProtoModuleDecoupler(cachePartModule);
            }

            return null;
        }

        /// <summary>
        ///     Gets a generic proto engine for the current engine module attached to the part.
        /// </summary>
        public static ProtoModuleEngine GetProtoModuleEngine(this Part part)
        {
            PartModule cachePartModule = GetModule<ModuleEngines>(part);
            if (cachePartModule != null)
            {
                return new ProtoModuleEngine(cachePartModule);
            }

            cachePartModule = GetModuleMultiModeEngine(part) ?? GetModule<ModuleEnginesFX>(part);
            if (cachePartModule != null)
            {
                return new ProtoModuleEngine(cachePartModule);
            }

            return null;
        }

        /// <summary>
        ///     Gets the cost of the part's contained resources.
        /// </summary>
        public static double GetResourceCost(this Part part)
        {
            double cost = 0.0;
            for (int i = 0; i < part.Resources.list.Count; ++i)
            {
                PartResource cachePartResource = part.Resources.list[i];
                cost = cost + (cachePartResource.amount * cachePartResource.info.unitCost);
            }
            return cost;
        }

        /// <summary>
        ///     Gets the cost of the part's contained resources, inverted.
        /// </summary>
        public static double GetResourceCostInverted(this Part part)
        {
            double sum = 0;
            for (int i = 0; i < part.Resources.list.Count; i++)
            {
                PartResource r = part.Resources.list[i];
                sum += (r.maxAmount - r.amount) * r.info.unitCost;
            }
            return sum;
        }

        /// <summary>
        ///     Gets the cost of the part's maximum contained resources.
        /// </summary>
        public static double GetResourceCostMax(this Part part)
        {
            double cost = 0.0;
            for (int i = 0; i < part.Resources.list.Count; ++i)
            {
                PartResource cachePartResource = part.Resources.list[i];
                cost = cost + (cachePartResource.maxAmount * cachePartResource.info.unitCost);
            }
            return cost;
        }

        /// <summary>
        ///     Gets the current specific impulse for the engine.
        /// </summary>
        public static double GetSpecificImpulse(this Part part, float atmosphere)
        {
            PartModule cachePartModule = GetModule<ModuleEngines>(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEngines).atmosphereCurve.Evaluate(atmosphere);
            }

            cachePartModule = GetModuleMultiModeEngine(part) ?? GetModule<ModuleEnginesFX>(part);
            if (cachePartModule != null)
            {
                return (cachePartModule as ModuleEnginesFX).atmosphereCurve.Evaluate(atmosphere);
            }

            return 0.0;
        }

        /// <summary>
        ///     Gets the total mass of the part including resources.
        /// </summary>
        public static double GetWetMass(this Part part)
        {
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass + part.GetResourceMass() : part.GetResourceMass();
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule.
        /// </summary>
        public static bool HasModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (part.Modules[i] is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule conforming to the supplied predicate.
        /// </summary>
        public static bool HasModule<T>(this Part part, Func<T, bool> predicate) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                if (pm is T && predicate(pm as T))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule.
        /// </summary>
        public static bool HasModule(this Part part, string className)
        {
            return part.Modules.Contains(className);
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule.
        /// </summary>
        public static bool HasModule(this Part part, int moduleId)
        {
            return part.Modules.Contains(moduleId);
        }

        /// <summary>
        ///     Gets whether the part has a one shot animation.
        /// </summary>
        public static bool HasOneShotAnimation(this Part part)
        {
            PartModule cachePartModule = GetModule<ModuleAnimateGeneric>(part);
            return cachePartModule != null && (cachePartModule as ModuleAnimateGeneric).isOneShot;
        }

        /// <summary>
        ///     Gets whether the part is a command module.
        /// </summary>
        public static bool IsCommandModule(this Part part)
        {
            return HasModule<ModuleCommand>(part);
        }

        /// <summary>
        ///     Gets whether the part is decoupled in a specified stage.
        /// </summary>
        public static bool IsDecoupledInStage(this Part part, int stage)
        {
            if ((IsDecoupler(part) || IsLaunchClamp(part)) && part.inverseStage == stage)
            {
                return true;
            }
            if (part.parent == null)
            {
                return false;
            }
            return IsDecoupledInStage(part.parent, stage);
        }

        /// <summary>
        ///     Gets whether the part is a decoupler.
        /// </summary>
        public static bool IsDecoupler(this Part part)
        {
            return HasModule<ModuleDecouple>(part) || HasModule<ModuleAnchoredDecoupler>(part);
        }

        /// <summary>
        ///     Gets whether the part is an active engine.
        /// </summary>
        public static bool IsEngine(this Part part)
        {
            return HasModule<ModuleEngines>(part) || HasModule<ModuleEnginesFX>(part);
        }

        /// <summary>
        ///     Gets whether the part is a fuel line.
        /// </summary>
        public static bool IsFuelLine(this Part part)
        {
            return HasModule<CModuleFuelLine>(part);
        }

        /// <summary>
        ///     Gets whether the part is a generator.
        /// </summary>
        public static bool IsGenerator(this Part part)
        {
            return HasModule<ModuleGenerator>(part);
        }

        /// <summary>
        ///     Gets whether the part is a launch clamp.
        /// </summary>
        public static bool IsLaunchClamp(this Part part)
        {
            return HasModule<LaunchClamp>(part);
        }

        /// <summary>
        ///     Gets whether the part is a parachute.
        /// </summary>
        public static bool IsParachute(this Part part)
        {
            return HasModule<ModuleParachute>(part);
        }

        /// <summary>
        ///     Gets whether the part is considered a primary part on the vessel.
        /// </summary>
        public static bool IsPrimary(this Part part, List<Part> partsList, PartModule module)
        {
            for (int i = 0; i < partsList.Count; i++)
            {
                var vesselPart = partsList[i];
                if (!vesselPart.HasModule(module.ClassID))
                {
                    continue;
                }
                if (vesselPart == part)
                {
                    return true;
                }
                break;
            }

            return false;
        }

        public static bool IsRcsModule(this Part part)
        {
            return HasModule<ModuleRCS>(part);
        }

        /// <summary>
        ///     Gets whether the part is a sepratron.
        /// </summary>
        public static bool IsSepratron(this Part part)
        {
            return IsSolidRocket(part) && part.ActivatesEvenIfDisconnected && IsDecoupledInStage(part, part.inverseStage);
        }

        /// <summary>
        ///     Gets whether the part is a deployable solar panel.
        /// </summary>
        public static bool IsSolarPanel(this Part part)
        {
            return HasModule<ModuleDeployableSolarPanel>(part);
        }

        /// <summary>
        ///     Gets whether the part is a solid rocket motor.
        /// </summary>
        public static bool IsSolidRocket(this Part part)
        {
            return (part.HasModule<ModuleEngines>() && part.GetModuleEngines().throttleLocked) || (part.HasModule<ModuleEnginesFX>() && part.GetModuleEnginesFx().throttleLocked);
        }

        public class ProtoModuleDecoupler
        {
            private readonly PartModule module;

            public ProtoModuleDecoupler(PartModule module)
            {
                this.module = module;

                if (this.module is ModuleDecouple)
                {
                    SetModuleDecouple();
                }
                else if (this.module is ModuleAnchoredDecoupler)
                {
                    SetModuleAnchoredDecoupler();
                }
            }

            public double EjectionForce { get; private set; }
            public bool IsOmniDecoupler { get; private set; }

            private void SetModuleAnchoredDecoupler()
            {
                ModuleAnchoredDecoupler decoupler = module as ModuleAnchoredDecoupler;
                if (decoupler == null)
                {
                    return;
                }

                EjectionForce = decoupler.ejectionForce;
            }

            private void SetModuleDecouple()
            {
                ModuleDecouple decoupler = module as ModuleDecouple;
                if (decoupler == null)
                {
                    return;
                }

                EjectionForce = decoupler.ejectionForce;
                IsOmniDecoupler = decoupler.isOmniDecoupler;
            }
        }

        public class ProtoModuleEngine
        {
            private readonly PartModule module;

            public ProtoModuleEngine(PartModule module)
            {
                this.module = module;

                if (module is ModuleEngines)
                {
                    SetModuleEngines();
                }
            }

            public double MaximumThrust { get; private set; }
            public double MinimumThrust { get; private set; }
            public List<Propellant> Propellants { get; private set; }

            public float GetSpecificImpulse(float atmosphere)
            {
                if (module is ModuleEngines)
                {
                    return (module as ModuleEngines).atmosphereCurve.Evaluate(atmosphere);
                }
                return 0.0f;
            }

            private void SetModuleEngines()
            {
                ModuleEngines engine = module as ModuleEngines;
                if (engine == null)
                {
                    return;
                }

                MaximumThrust = engine.maxThrust * (engine.thrustPercentage * 0.01);
                MinimumThrust = engine.minThrust;
                Propellants = engine.propellants;
            }

            private void SetModuleEnginesFx()
            {
                ModuleEnginesFX engine = module as ModuleEnginesFX;
                if (engine == null)
                {
                    return;
                }

                MaximumThrust = engine.maxThrust * (engine.thrustPercentage * 0.01);
                MinimumThrust = engine.minThrust;
                Propellants = engine.propellants;
            }
        }
    }
}