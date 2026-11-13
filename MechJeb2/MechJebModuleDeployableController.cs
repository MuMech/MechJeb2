extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;

namespace MuMech
{
    public abstract class MechJebModuleDeployableController : ComputerModule
    {
        protected MechJebModuleDeployableController(MechJebCore core) : base(core)
        {
            Priority = 200;
            Enabled = true;
        }

        protected string ButtonText;
        protected bool Extended;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool AutoDeploy;

        [UsedImplicitly, Persistent(pass = (int)Pass.LOCAL)]
        public bool PrevShouldDeploy;

        public bool PrevAutoDeploy = true;

        [UsedImplicitly]
        protected readonly List<ModuleDeployablePart> CachedPartModules = new List<ModuleDeployablePart>(16);

        [UsedImplicitly]
        protected void DiscoverDeployablePartModules()
        {
            CachedPartModules.Clear();
            foreach (Part p in Vessel.Parts)
            foreach (PartModule pm in p.Modules)
                if (pm != null && pm is ModuleDeployablePart mdp && IsModules(mdp))
                    CachedPartModules.Add(mdp);
        }

        // KSP shows neither event while the part moves, once it is broken, or when it is extended and cannot retract: this
        // chooses the parts to act on, never their state
        [UsedImplicitly]
        protected bool IsDeployable(ModuleDeployablePart sa) => sa.Events["Extend"].active || sa.Events["Retract"].active;

        // the parts an auto-deploy left folded because they were shielded (a fairing, a closed bay): extended once they are not
        private readonly List<ModuleDeployablePart> _shieldedAtDeploy = new List<ModuleDeployablePart>();

        // the list is not saved, PrevShouldDeploy is: false until an ExtendAll or a RetractAll of this session has set the list
        private bool _shieldedAtDeployKnown;

        public void ExtendAll()
        {
            _shieldedAtDeploy.Clear();
            _shieldedAtDeployKnown = true;
            foreach (ModuleDeployablePart mdp in CachedPartModules)
            {
                if (mdp is null || !IsDeployable(mdp)) continue;
                if (mdp.part.ShieldedFromAirstream)
                    _shieldedAtDeploy.Add(mdp);
                else
                    mdp.Extend();
            }
        }

        // only the parts ExtendAll skipped: a panel the player retracts by hand afterwards stays retracted
        private void ExtendUnshielded()
        {
            // a vessel loaded already deployed: the parts still folded and shielded are the ones its ExtendAll skipped, as KSP
            // extends no shielded part
            if (!_shieldedAtDeployKnown)
            {
                _shieldedAtDeployKnown = true;
                foreach (ModuleDeployablePart mdp in CachedPartModules)
                    if (!(mdp is null) && IsDeployable(mdp) && mdp.part.ShieldedFromAirstream)
                        _shieldedAtDeploy.Add(mdp);
            }

            for (int i = _shieldedAtDeploy.Count - 1; i >= 0; i--)
            {
                ModuleDeployablePart mdp = _shieldedAtDeploy[i];
                if (mdp == null || mdp.vessel != Vessel)
                    _shieldedAtDeploy.RemoveAt(i);
                else if (!mdp.part.ShieldedFromAirstream)
                {
                    _shieldedAtDeploy.RemoveAt(i);
                    if (IsDeployable(mdp)) mdp.Extend();
                }
            }
        }

        public void RetractAll()
        {
            _shieldedAtDeploy.Clear();
            _shieldedAtDeployKnown = true;
            foreach (ModuleDeployablePart mdp in CachedPartModules)
                if (!(mdp is null) && IsDeployable(mdp))
                    mdp.Retract();
        }

        /// <summary>No part that can move is extended or moving: each is retracted, or broken. A fixed panel (no animation) does not count.</summary>
        public bool AllRetracted()
        {
            foreach (ModuleDeployablePart mdp in CachedPartModules)
                if (!(mdp is null) && mdp.useAnimation && mdp.deployState != ModuleDeployablePart.DeployState.RETRACTED &&
                    mdp.deployState != ModuleDeployablePart.DeployState.BROKEN)
                    return false;
            return true;
        }

        /// <summary>RetractAll has nothing left to do: no part moves, and no extended part can still retract (KSP offers its Retract).</summary>
        private bool DoneRetracting()
        {
            foreach (ModuleDeployablePart mdp in CachedPartModules)
            {
                if (mdp is null) continue;
                switch (mdp.deployState)
                {
                    case ModuleDeployablePart.DeployState.EXTENDING:
                    case ModuleDeployablePart.DeployState.RETRACTING:
                    case ModuleDeployablePart.DeployState.EXTENDED when IsDeployable(mdp):
                        return false;
                }
            }

            return true;
        }

        /// <summary>Whether the ascent settings have the ascent autopilot manage these parts.</summary>
        protected abstract bool AutoDeployDuringAscent { get; }

        /// <summary>An ascent autopilot's per-tick hook: retracted inside the atmosphere, extended above it.</summary>
        public void DriveForAscent()
        {
            if (!AutoDeployDuringAscent)
                return;

            if (VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude())
                ExtendAll();
            else
                RetractAll();
        }

        /// <summary>An ascent autopilot's prelaunch hook: retracts these parts and returns true once they are clear for launch.</summary>
        public bool RetractForLaunch()
        {
            if (!AutoDeployDuringAscent || !MainBody.atmosphere)
                return true;

            RetractAll();
            return DoneRetracting();
        }

        private bool ShouldDeploy()
        {
            if (!MainBody.atmosphere)
                return true;

            if (!Vessel.LiftedOff())
                return false;

            if (Vessel.LandedOrSplashed)
                return false; // True adds too many complex case

            const double DT = 10;
            double minAlt; // minimum altitude between now and now+dt seconds
            double t = Planetarium.GetUniversalTime();

            double peT = Orbit.NextPeriapsisTime(t) - t;
            if (peT > 0 && peT < DT)
                minAlt = Orbit.PeA;
            else
                minAlt = Math.Sqrt(Math.Min(Orbit.getRelativePositionAtUT(t).sqrMagnitude, Orbit.getRelativePositionAtUT(t + DT).sqrMagnitude)) -
                    MainBody.Radius;

            return minAlt > MainBody.RealMaxAtmosphereAltitude();
        }

        public override void OnFixedUpdate()
        {
            // Let the ascent guidance handle the solar panels to retract them before launch
            if (AutoDeploy && !Core.Ascent.Enabled)
            {
                bool tmp = ShouldDeploy();

                switch (tmp)
                {
                    case true when !PrevShouldDeploy || AutoDeploy != PrevAutoDeploy:
                        ExtendAll();
                        break;
                    case true:
                        ExtendUnshielded();
                        break;
                    case false when PrevShouldDeploy || AutoDeploy != PrevAutoDeploy:
                        RetractAll();
                        break;
                }

                PrevShouldDeploy = tmp;
                PrevAutoDeploy = true;
            }
            else
            {
                PrevAutoDeploy = false;
            }

            bool extendedThisPass = !AllRetracted();
            if (Extended != extendedThisPass)
                ButtonText = GetButtonText(extendedThisPass ? DeployablePartState.EXTENDED : DeployablePartState.RETRACTED);

            Extended = extendedThisPass;
        }

        /// <summary>The toggle button's text ("Toggle ... (currently extended)").</summary>
        internal string CurrentButtonText => ButtonText;

        /// <summary>The toggle button's action: retract all when a part can retract, extend all otherwise, unless parts are still moving.</summary>
        internal void ToggleDeploy()
        {
            if (ExtendingOrRetracting())
                return;

            // not Extended: an extended part that cannot retract would keep the button on a RetractAll that moves nothing
            if (AnyCanRetract())
                RetractAll();
            else
                ExtendAll();
        }

        private bool AnyCanRetract()
        {
            foreach (ModuleDeployablePart mdp in CachedPartModules)
                if (mdp != null && mdp.Events["Retract"].active)
                    return true;
            return false;
        }

        protected bool ExtendingOrRetracting()
        {
            foreach (ModuleDeployablePart mdp in CachedPartModules)
                if (mdp != null && (mdp.deployState == ModuleDeployablePart.DeployState.EXTENDING ||
                                    mdp.deployState == ModuleDeployablePart.DeployState.RETRACTING))
                    return true;
            return false;
        }

        protected abstract bool IsModules(ModuleDeployablePart p);

        protected enum DeployablePartState
        {
            RETRACTED,
            EXTENDED
        }

        protected abstract string GetButtonText(DeployablePartState deployablePartState);

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            // OnFixedUpdate only sets the text when the state changes; parts that start retracted never change it,
            // which left the button empty. Start from the state Extended has now.
            ButtonText = GetButtonText(Extended ? DeployablePartState.EXTENDED : DeployablePartState.RETRACTED);
            if (HighLogic.LoadedSceneIsFlight)
                DiscoverDeployablePartModules();
        }

        public override void OnVesselWasModified(Vessel v) => DiscoverDeployablePartModules();
    }
}
