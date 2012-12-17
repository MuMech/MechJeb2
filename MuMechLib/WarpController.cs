using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    //Should this really be a ComputerModule? It doesn't use any of the callbacks. It does use the vesselState.
    class WarpController : ComputerModule 
    {
        public WarpController(MechJebCore core) : base(core) { }

        double warpIncreaseAttemptTime = 0;


        public bool warpIncrease(ComputerModule controller, bool instant = false, double maxRate = 100000.0)
        {
//            if ((controlModule != null) && (controller != null) && (controlModule != controller)) return false;

            //need to use instantaneous altitude and not the time-averaged vesselState.altitudeASL,
            //because the game freaks out really hard if you try to violate the altitude limits
            double instantAltitudeASL = (vesselState.CoM - part.vessel.mainBody.position).magnitude - part.vessel.mainBody.Radius;

            if ((TimeWarp.WarpMode == TimeWarp.Modes.LOW) && ((instantAltitudeASL > part.vessel.mainBody.maxAtmosphereAltitude) || part.vessel.Landed))
            {
                warpIncreaseAttemptTime = vesselState.time;
                TimeWarp.SetRate(0, instant);
                return true;
            }

            //conditions to increase warp:
            //-we are not already at max warp
            //-the most recent non-instant warp change has completed
            //-the next warp rate is not greater than maxRate
            //-we are out of the atmosphere, or the next warp rate is still a physics rate, or we are landed
            //-increasing the rate is allowed by altitude limits, or we are landed
            //-we did not increase the warp within the last 2 seconds
            if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length
                && (TimeWarp.CurrentRate == 0 || TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] == TimeWarp.CurrentRate)
                && TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate
                && (instantAltitudeASL > part.vessel.mainBody.maxAtmosphereAltitude
                    || TimeWarp.WarpMode == TimeWarp.Modes.LOW
                    || TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= TimeWarp.MaxPhysicsRate
                    || part.vessel.Landed)
                && (instantAltitudeASL > TimeWarp.fetch.GetAltitudeLimit(TimeWarp.CurrentRateIndex + 1, part.vessel.mainBody)
                    || part.vessel.Landed)
                && vesselState.time - warpIncreaseAttemptTime > 2)
            {
                warpIncreaseAttemptTime = vesselState.time;
                TimeWarp.SetRate(TimeWarp.CurrentRateIndex + 1, instant);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void warpDecrease(ComputerModule controller, bool instant = false)
        {
//            if ((controlModule != null) && (controller != null) && (controlModule != controller)) return;

            if (TimeWarp.CurrentRateIndex > 0
                /*&& timeWarp.warpRates[TimeWarp.CurrentRateIndex] == TimeWarp.CurrentRate*/)
            {
                TimeWarp.SetRate(TimeWarp.CurrentRateIndex - 1, instant);
            }
        }

        public void warpMinimum(ComputerModule controller, bool instant = false)
        {
//            if ((controlModule != null) && (controller != null) && (controlModule != controller)) return;
            if (TimeWarp.CurrentRateIndex <= 0) return; //Somehow setting TimeWarp.SetRate to 0 when already at 0 causes unexpected rapid separation (Kracken)
            TimeWarp.SetRate(0, instant);
        }

        public void warpPhysics(ComputerModule controller, bool instant = false)
        {
//            if ((controlModule != null) && (controller != null) && (controlModule != controller)) return;

            if ((TimeWarp.WarpMode == TimeWarp.Modes.LOW) || (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] <= TimeWarp.MaxPhysicsRate))
            {
                return;
            }
            else
            {
                int newIndex = TimeWarp.CurrentRateIndex;
                while (newIndex > 0 && TimeWarp.fetch.warpRates[newIndex] > TimeWarp.MaxPhysicsRate) newIndex--;
                TimeWarp.SetRate(newIndex, instant);
            }
        }

        public void warpTo(ComputerModule controller, double timeLeft, double[] lookaheadTimes, double maxRate = 100000.0)
        {
//            if ((controlModule != null) && (controller != null) && (controlModule != controller)) return;

            if ((TimeWarp.WarpMode == TimeWarp.Modes.HIGH) && ((timeLeft < lookaheadTimes[TimeWarp.CurrentRateIndex])
                || (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] > maxRate)))
            {
                warpDecrease(controller, true);
            }
            else if ((TimeWarp.CurrentRateIndex < TimeWarp.fetch.warpRates.Length - 1
                && lookaheadTimes[TimeWarp.CurrentRateIndex + 1] < timeLeft
                && TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
                || (TimeWarp.WarpMode == TimeWarp.Modes.LOW))
            {
                warpIncrease(controller, false, maxRate);
            }
        }
    }
}
