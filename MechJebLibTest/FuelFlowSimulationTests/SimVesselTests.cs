/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.FuelFlowSimulation;
using Xunit;

namespace MechJebLibTest.FuelFlowSimulationTests
{
    public class SimVesselTests
    {
        // the simulation stages the vessel down past stage 0, and the vessel managers put the stage back before the next run
        // with the stage KSP has now
        [Fact]
        public void ResetCurrentStageRestoresTheStageAfterARun()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();
            new FuelFlowSimulation().Run(v);

            Assert.Equal(-1, v.CurrentStage);

            v.ResetCurrentStage(4);

            Assert.Equal(4, v.CurrentStage);
        }

        // the vessel was built a tick before KSP lowered its stage, or KSP has staged it since: the lower stage sticks, and
        // the next reset goes back to it rather than to the stage the vessel was built with
        [Fact]
        public void ResetCurrentStageFollowsKspStaging()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();

            v.ResetCurrentStage(3);
            Assert.Equal(3, v.CurrentStage);

            new FuelFlowSimulation().Run(v);
            v.ResetCurrentStage(3);
            Assert.Equal(3, v.CurrentStage);
        }

        // a stage higher than the saved one puts the vessel back at the saved stage: the vessel doesn't go back up a stage
        [Fact]
        public void ResetCurrentStageDoesNotRaiseTheStage()
        {
            SimVessel v = SimVesselFixtures.MainsailTinCan2Stage();

            v.ResetCurrentStage(2);
            Assert.Equal(2, v.CurrentStage);

            v.ResetCurrentStage(3);
            Assert.Equal(2, v.CurrentStage);

            v.ResetCurrentStage(4);
            Assert.Equal(2, v.CurrentStage);
        }
    }
}
