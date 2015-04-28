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

#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace KerbalEngineer.Extensions
{
    using CompoundParts;

    public static class PartExtensions
    {
        #region Methods: public

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
            return part.Resources.list.Count(p => p.amount > 0d) > 0;
        }

        /// <summary>
        ///     Gets whether the part has fuel.
        /// </summary>
        public static bool EngineHasFuel(this Part part)
        {
            if (part.HasModule<ModuleEngines>())
            {
                return part.GetModuleEngines().getFlameoutState;
            }
            if (part.HasModule<MultiModeEngine>())
            {
                return part.GetModuleMultiModeEngine().getFlameoutState;
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
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass + part.GetPhysicslessChildMass() : 0d;
        }

        /// <summary>
        ///     Gets the maximum thrust of the part if it's an engine.
        /// </summary>
        public static double GetMaxThrust(this Part part)
        {
            if (part.HasModule<ModuleEngines>())
            {
                return part.GetModuleEngines().maxThrust;
            }
            if (part.HasModule<MultiModeEngine>())
            {
                return part.GetModuleMultiModeEngine().maxThrust;
            }
            if (part.HasModule<ModuleEnginesFX>())
            {
                return part.GetModuleEnginesFx().maxThrust;
            }

            return 0d;
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
            return (T)Convert.ChangeType(part.Modules[className], typeof(T));
        }

        /// <summary>
        ///     Gets a typed PartModule.
        /// </summary>
        public static T GetModule<T>(this Part part, int classId) where T : PartModule
        {
            return (T)Convert.ChangeType(part.Modules[classId], typeof(T));
        }

        /// <summary>
        ///     Gets a ModuleAlternator typed PartModule.
        /// </summary>
        public static ModuleAlternator GetModuleAlternator(this Part part)
        {
            return part.GetModule<ModuleAlternator>();
        }

        /// <summary>
        ///     Gets a ModuleDeployableSolarPanel typed PartModule.
        /// </summary>
        public static ModuleDeployableSolarPanel GetModuleDeployableSolarPanel(this Part part)
        {
            return part.GetModule<ModuleDeployableSolarPanel>();
        }

        /// <summary>
        ///     Gets a ModuleEngines typed PartModule.
        /// </summary>
        public static ModuleEngines GetModuleEngines(this Part part)
        {
            return part.GetModule<ModuleEngines>();
        }

        public static ModuleEnginesFX GetModuleEnginesFx(this Part part)
        {
            return part.GetModule<ModuleEnginesFX>();
        }

        /// <summary>
        ///     Gets a ModuleGenerator typed PartModule.
        /// </summary>
        public static ModuleGenerator GetModuleGenerator(this Part part)
        {
            return part.GetModule<ModuleGenerator>();
        }

        /// <summary>
        ///     Gets a ModuleGimbal typed PartModule.
        /// </summary>
        public static ModuleGimbal GetModuleGimbal(this Part part)
        {
            return part.GetModule<ModuleGimbal>();
        }

        /// <summary>
        ///     Gets the current selected ModuleEnginesFX.
        /// </summary>
        public static ModuleEnginesFX GetModuleMultiModeEngine(this Part part)
        {
            var mode = part.GetModule<MultiModeEngine>().mode;
            return part.Modules.OfType<ModuleEnginesFX>().FirstOrDefault(engine => engine.engineID == mode);
        }

        /// <summary>
        ///     Gets a ModuleParachute typed PartModule.
        /// </summary>
        public static ModuleParachute GetModuleParachute(this Part part)
        {
            return part.GetModule<ModuleParachute>();
        }

        public static ModuleRCS GetModuleRcs(this Part part)
        {
            return part.GetModule<ModuleRCS>();
        }

        /// <summary>
        ///     Gets a typed list of PartModules.
        /// </summary>
        public static List<T> GetModules<T>(this Part part) where T : PartModule
        {
            return part.Modules.OfType<T>().ToList();
        }

        public static ProtoModuleDecoupler GetProtoModuleDecoupler(this Part part)
        {
            if (HasModule<ModuleDecouple>(part))
            {
                return new ProtoModuleDecoupler(GetModule<ModuleDecouple>(part));
            }
            if (HasModule<ModuleAnchoredDecoupler>(part))
            {
                return new ProtoModuleDecoupler(GetModule<ModuleAnchoredDecoupler>(part));
            }
            return null;
        }

        /// <summary>
        ///     Gets a generic proto engine for the current engine module attached to the part.
        /// </summary>
        public static ProtoModuleEngine GetProtoModuleEngine(this Part part)
        {
            if (HasModule<ModuleEngines>(part))
            {
                return new ProtoModuleEngine(GetModule<ModuleEngines>(part));
            }
            if (HasModule<MultiModeEngine>(part))
            {
                return new ProtoModuleEngine(GetModuleMultiModeEngine(part));
            }
            if (HasModule<ModuleEnginesFX>(part))
            {
                return new ProtoModuleEngine(GetModule<ModuleEnginesFX>(part));
            }
            return null;
        }

        /// <summary>
        ///     Gets the cost of the part's contained resources.
        /// </summary>
        public static double GetResourceCost(this Part part)
        {
            return part.Resources.list.Sum(r => r.amount * r.info.unitCost);
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
            return part.Resources.list.Sum(r => r.maxAmount * r.info.unitCost);
        }

        /// <summary>
        ///     Gets the current specific impulse for the engine.
        /// </summary>
        public static double GetSpecificImpulse(this Part part, float atmosphere)
        {
            if (part.HasModule<ModuleEngines>())
            {
                return part.GetModuleEngines().atmosphereCurve.Evaluate(atmosphere);
            }
            if (part.HasModule<MultiModeEngine>())
            {
                return part.GetModuleMultiModeEngine().atmosphereCurve.Evaluate(atmosphere);
            }
            if (part.HasModule<ModuleEnginesFX>())
            {
                return part.GetModuleEnginesFx().atmosphereCurve.Evaluate(atmosphere);
            }

            return 0d;
        }

        /// <summary>
        ///     Gets the total mass of the part including resources.
        /// </summary>
        public static double GetWetMass(this Part part)
        {
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass + part.GetPhysicslessChildMass() + part.GetResourceMass() : part.GetResourceMass();
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
            return part.HasModule<ModuleAnimateGeneric>() && part.GetModule<ModuleAnimateGeneric>().isOneShot;
        }

        /// <summary>
        ///     Gets whether the part is a command module.
        /// </summary>
        public static bool IsCommandModule(this Part part)
        {
            return part.HasModule<ModuleCommand>();
        }

        /// <summary>
        ///     Gets whether the part is decoupled in a specified stage.
        /// </summary>
        public static bool IsDecoupledInStage(this Part part, int stage)
        {
            if ((part.IsDecoupler() || part.IsLaunchClamp()) && part.inverseStage == stage)
            {
                return true;
            }
            if (part.parent == null)
            {
                return false;
            }
            return part.parent.IsDecoupledInStage(stage);
        }

        /// <summary>
        ///     Gets whether the part is a decoupler.
        /// </summary>
        public static bool IsDecoupler(this Part part)
        {
            return part.HasModule<ModuleDecouple>() || part.HasModule<ModuleAnchoredDecoupler>();
        }

        /// <summary>
        ///     Gets whether the part is an active engine.
        /// </summary>
        public static bool IsEngine(this Part part)
        {
            return part.HasModule<ModuleEngines>() || part.HasModule<ModuleEnginesFX>();
        }

        /// <summary>
        ///     Gets whether the part is a fuel line.
        /// </summary>
        public static bool IsFuelLine(this Part part)
        {
            return (HasModule<CModuleFuelLine>(part));
        }

        /// <summary>
        ///     Gets whether the part is a generator.
        /// </summary>
        public static bool IsGenerator(this Part part)
        {
            return part.HasModule<ModuleGenerator>();
        }

        /// <summary>
        ///     Gets whether the part is a launch clamp.
        /// </summary>
        public static bool IsLaunchClamp(this Part part)
        {
            return part.HasModule<LaunchClamp>();
        }

        /// <summary>
        ///     Gets whether the part is a parachute.
        /// </summary>
        public static bool IsParachute(this Part part)
        {
            return part.HasModule<ModuleParachute>();
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
            return part.HasModule<ModuleRCS>();
        }

        /// <summary>
        ///     Gets whether the part is a sepratron.
        /// </summary>
        public static bool IsSepratron(this Part part)
        {
            return (part.IsSolidRocket() && part.ActivatesEvenIfDisconnected && part.IsDecoupledInStage(part.inverseStage));
        }

        /// <summary>
        ///     Gets whether the part is a deployable solar panel.
        /// </summary>
        public static bool IsSolarPanel(this Part part)
        {
            return part.HasModule<ModuleDeployableSolarPanel>();
        }

        /// <summary>
        ///     Gets whether the part is a solid rocket motor.
        /// </summary>
        public static bool IsSolidRocket(this Part part)
        {
            return (part.HasModule<ModuleEngines>() && part.GetModuleEngines().throttleLocked) || (part.HasModule<ModuleEnginesFX>() && part.GetModuleEnginesFx().throttleLocked);
        }

        #endregion

        #region Nested Type: ProtoModuleDecoupler

        public class ProtoModuleDecoupler
        {
            #region Fields

            private readonly PartModule module;

            #endregion

            #region Constructors

            public ProtoModuleDecoupler(PartModule module)
            {
                this.module = module;

                if (this.module is ModuleDecouple)
                {
                    this.SetModuleDecouple();
                }
                else if (this.module is ModuleAnchoredDecoupler)
                {
                    this.SetModuleAnchoredDecoupler();
                }
            }

            #endregion

            #region Properties

            public double EjectionForce { get; private set; }
            public bool IsOmniDecoupler { get; private set; }

            #endregion

            #region Methods: private

            private void SetModuleAnchoredDecoupler()
            {
                var decoupler = this.module as ModuleAnchoredDecoupler;
                if (decoupler == null)
                {
                    return;
                }

                this.EjectionForce = decoupler.ejectionForce;
            }

            private void SetModuleDecouple()
            {
                var decoupler = this.module as ModuleDecouple;
                if (decoupler == null)
                {
                    return;
                }

                this.EjectionForce = decoupler.ejectionForce;
                this.IsOmniDecoupler = decoupler.isOmniDecoupler;
            }

            #endregion
        }

        #endregion

        #region Nested Type: ProtoModuleEngine

        public class ProtoModuleEngine
        {
            #region Fields

            private readonly PartModule module;

            #endregion

            #region Constructors

            public ProtoModuleEngine(PartModule module)
            {
                this.module = module;

                if (module is ModuleEngines)
                {
                    this.SetModuleEngines();
                }
                else if (module is ModuleEnginesFX)
                {
                    this.SetModuleEnginesFx();
                }
            }

            #endregion

            #region Properties

            public double MaximumThrust { get; private set; }
            public double MinimumThrust { get; private set; }
            public List<Propellant> Propellants { get; private set; }

            #endregion

            #region Methods: public

            public float GetSpecificImpulse(float atmosphere)
            {
                if (this.module is ModuleEngines)
                {
                    return (this.module as ModuleEngines).atmosphereCurve.Evaluate(atmosphere);
                }
                if (this.module is ModuleEnginesFX)
                {
                    return (this.module as ModuleEnginesFX).atmosphereCurve.Evaluate(atmosphere);
                }
                return 0.0f;
            }

            #endregion

            #region Methods: private

            private void SetModuleEngines()
            {
                var engine = this.module as ModuleEngines;
                if (engine == null)
                {
                    return;
                }

                this.MaximumThrust = engine.maxThrust * (engine.thrustPercentage * 0.01);
                this.MinimumThrust = engine.minThrust;
                this.Propellants = engine.propellants;
            }

            private void SetModuleEnginesFx()
            {
                var engine = this.module as ModuleEnginesFX;
                if (engine == null)
                {
                    return;
                }

                this.MaximumThrust = engine.maxThrust * (engine.thrustPercentage * 0.01);
                this.MinimumThrust = engine.minThrust;
                this.Propellants = engine.propellants;
            }

            #endregion
        }

        #endregion
    }
}