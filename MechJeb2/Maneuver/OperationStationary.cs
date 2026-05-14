extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationStationaryOrbit : Operation
    {
        [Persistent] public double targetLongitude = 0;
        [Persistent] public double targetLatitude = 0;

        public override string GetName() => Localizer.Format("#MechJeb_stationary_title"); //stationary orbit

        private void MoveByMeter(ref EditableAngle angle, double distance, double alt, Orbit o)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + o.referenceBody.Radius);
            angle += angularDelta;
        }

        public override void DoParametersGUI(Orbit o, double UT, MechJebModuleTargetController targetController)
        {
            double asl = o.referenceBody.TerrainAltitude(targetController.targetLatitude, targetController.targetLongitude);

            GUILayout.BeginVertical();

            if (!targetController.PositionTargetExists)
            {
                targetController.SetPositionTarget(o.referenceBody, targetLatitude, targetLongitude);
            }

            GUILayout.Label(Localizer.Format("#MechJeb_stationary_label1")); //Target longitude:

            GUILayout.BeginHorizontal();
            targetController.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);

            if (GUILayout.Button("◄"))
            {
                MoveByMeter(ref targetController.targetLongitude, -10, asl, o);
            }

            GUILayout.Label("10m");
            if (GUILayout.Button("►"))
            {
                MoveByMeter(ref targetController.targetLongitude, 10, asl, o);
            }
            GUILayout.EndHorizontal();

            if (targetController.targetBody != null)
            {
                GUILayout.Label(targetController.targetBody.GetExperimentBiomeSafe(targetController.targetLatitude, targetController.targetLongitude));
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) targetController.PickPositionTargetOnMap(); //Pick target on map

            targetLatitude = 0;
            targetLongitude = targetController.targetLongitude;

            GUILayout.EndVertical();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double ut, MechJebModuleTargetController target)
        {
            double syncSMA = Math.Pow(o.referenceBody.gravParameter * Math.Pow(o.referenceBody.rotationPeriod / (2.0 * Math.PI), 2.0), 1.0 / 3.0);

            if (syncSMA > o.referenceBody.sphereOfInfluence)
            {
                throw new OperationException(Localizer.Format("#MechJeb_stationary_Exception1", o.referenceBody.displayName.LocalizeRemoveGender()));
            }

            double currentBodyRotationRad = (o.referenceBody.rotationAngle + (360.0 * (ut / o.referenceBody.rotationPeriod))) * Math.PI / 180.0;

            double targMNA = (targetLongitude * Math.PI / 180.0) + currentBodyRotationRad;
            targMNA = (targMNA % (2 * Math.PI) + 2 * Math.PI) % (2 * Math.PI);
            double syncAlt = syncSMA - o.referenceBody.Radius;
            Vector3d targetWorldPos = o.referenceBody.GetWorldSurfacePosition(0, targetLongitude, syncAlt);
            Vector3d radiusVector = targetWorldPos - o.referenceBody.position;
            double velMag = Math.Sqrt(o.referenceBody.gravParameter / syncSMA);
            Vector3d velDir = Vector3d.Cross(o.referenceBody.angularVelocity, radiusVector).normalized;
            Vector3d targetVelocity = velDir * velMag;

            Orbit targOrbit = new Orbit();
            targOrbit.UpdateFromStateVectors(radiusVector, targetVelocity, o.referenceBody, ut);
            targOrbit.eccentricity = 0;
            targOrbit.inclination = 0;
            targOrbit.Init();
            (Vector3d dV1, double ut1, Vector3d dV2, double ut2) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targOrbit, ut, 0, false, false, true, true);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV1, ut1),
                new ManeuverParameters(dV2, ut2)
            };
        }
    }
}
