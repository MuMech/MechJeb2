extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSBalancerWindow : DisplayModule
    {
        public MechJebModuleRCSBalancer balancer;

        public override void OnStart(PartModule.StartState state)
        {
            balancer = Core.GetComputerModule<MechJebModuleRCSBalancer>();

            if (balancer.smartTranslation)
            {
                balancer.Users.Add(this);
            }

            base.OnStart(state);
        }

        // shared by the IMGUI window and the new UI: the smart translation checkbox changed
        internal void OnSmartTranslationToggled()
        {
            balancer.ResetThrusterForces();

            if (balancer.smartTranslation)
            {
                balancer.Users.Add(this);
            }
            else
            {
                balancer.Users.Remove(this);
            }
        }

        // shared by the IMGUI window and the new UI: runs every frame the window is drawn
        internal void MaintainBalancerUser()
        {
            if (balancer.smartTranslation)
            {
                balancer.Users.Add(this);
            }
            else
            {
                balancer.Users.Remove(this);
            }
        }

        public override string GetName() => Localizer.Format("#MechJeb_RCSBalancer_title"); //"RCS Balancer"

        public override string IconName() => "RCS Balancer";

        public MechJebModuleRCSBalancerWindow(MechJebCore core) : base(core) { }
    }
}
