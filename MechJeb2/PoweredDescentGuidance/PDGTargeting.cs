using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class CTGIGLandingStep
    {
        private bool SetLandAnywhereTargetFromCoastSolution(
            double downrange,
            Vector3d xHat,
            Vector3d yHat,
            CelestialBody body)
        {
            if (LandAtTarget)
                return false;

            if (!IsFinite(downrange) || downrange <= 0.0)
            {
                Status = $"Land Anywhere: invalid coast solution x={downrange:F3}";
                return false;
            }

            if (body == null || xHat.magnitude < 1e-6 || yHat.magnitude < 1e-6)
            {
                Status = "Land Anywhere: invalid frame.";
                return false;
            }

            xHat = xHat.normalized;
            yHat = yHat.normalized;

            // 1. Use predicted downrange to get an initial surface lat/lon guess.
            Vector3d relGuess = downrange * xHat + body.Radius * yHat;
            Vector3d worldGuess = body.position + relGuess;

            body.GetLatLonAlt(worldGuess, out double guessLat, out double guessLon, out _);

            if (!IsFinite(guessLat) || !IsFinite(guessLon))
            {
                Status = "Land Anywhere: invalid guessed lat/lon.";
                return false;
            }

            // 2. Query terrain at the guessed lat/lon.
            double guessTerrainAlt = GetTerrainAltitude(body, guessLat, guessLon);

            // 3. Use terrain-adjusted radius only to refine the final lat/lon.
            double guessTargetRadius = body.Radius + guessTerrainAlt + TargetClearance;
            Vector3d relTargetGuess = downrange * xHat + guessTargetRadius * yHat;
            Vector3d worldTargetGuess = body.position + relTargetGuess;

            body.GetLatLonAlt(worldTargetGuess, out double finalLat, out double finalLon, out _);

            if (!IsFinite(finalLat) || !IsFinite(finalLon))
            {
                Status = "Land Anywhere: invalid final lat/lon.";
                return false;
            }

            // 4. Re-query terrain at the final lat/lon.
            double finalTerrainAlt = GetTerrainAltitude(body, finalLat, finalLon);

            // 5. Build the actual held target from lat/lon + terrain + clearance.
            // This avoids relying on the solver-frame vector to preserve altitude.
            _worldTargetPos = body.GetWorldSurfacePosition(
                finalLat,
                finalLon,
                finalTerrainAlt + TargetClearance
            );

            // 6. Store once. From now on TryGetEffectiveTarget() holds this lat/lon.
            _landAnywhereLat = finalLat;
            _landAnywhereLon = finalLon;
            _landAnywhereTargetSet = true;

            Core.Target.SetPositionTarget(body, finalLat, finalLon);

            Status =
                $"Land Anywhere target set. x={downrange:F0}m " +
                $"terrain={finalTerrainAlt:F1}m clearance={TargetClearance:F1}m " +
                $"lat={finalLat:F4} lon={finalLon:F4}";

            return true;
        }

        private double GetTerrainAltitude(CelestialBody body, double lat, double lon)
        {
            if (body.pqsController != null)
            {
                Vector3d radial = body.GetRelSurfaceNVector(lat, lon);
                return body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
            }

            return body.TerrainAltitude(lat, lon);
        }
        
        public void NudgeTargetMeters(double downrangeMeters, double crossrangeMeters)
        {
            Vessel vessel    = Vessel;
            CelestialBody cb = vessel.mainBody;
            if (!TryGetEffectiveTarget(out _, out _, out Vector3d targetPosBody, out _)) return;

            Vector3d targetUp = targetPosBody.normalized;
            if (targetUp.sqrMagnitude < 1e-10) return;

            // Fresh LOS from vessel to target, projected onto target tangent plane
            Vector3d vesselBCI = vessel.GetWorldPos3D() - cb.position;
            Vector3d los       = targetPosBody - vesselBCI;
            Vector3d downDir   = Vector3d.Exclude(targetUp, los);
            if (downDir.sqrMagnitude < 1e-10) downDir = Vector3d.Exclude(targetUp, _dbgFHat);
            if (downDir.sqrMagnitude < 1e-10) return;
            downDir.Normalize();

            Vector3d crossDir = Vector3d.Cross(targetUp, downDir);
            if (crossDir.sqrMagnitude < 1e-10) return;
            crossDir.Normalize();

            Vector3d shifted = targetPosBody + downrangeMeters * downDir + crossrangeMeters * crossDir;
            shifted = shifted.normalized * targetPosBody.magnitude;

            cb.GetLatLonAlt(cb.position + shifted, out double newLat, out double newLon, out _);
            double terrAlt = cb.TerrainAltitude(newLat, newLon);
            _worldTargetPos = cb.GetWorldSurfacePosition(newLat, newLon, terrAlt + TargetClearance);

            // Also update MechJeb target so the map marker moves
            if (LandAtTarget)
                Core.Target.SetPositionTarget(cb, newLat, newLon);
        }

        private bool TryGetEffectiveTarget(
            out double lat,
            out double lon,
            out Vector3d targetPosBody,
            out Vector3d targetWorld)
        {
            CelestialBody cb = MainBody;
            lat = lon = 0.0;
            targetPosBody = targetWorld = Vector3d.zero;

            if (cb == null)
                return false;

            

            // Targeted mode: use MechJeb's selected target.
            if (LandAtTarget)
            {
                lat = Core.Target.targetLatitude;
                lon = Core.Target.targetLongitude;

                if (!IsFinite(lat) || !IsFinite(lon))
                    return false;

                double terrAlt = GetTerrainAltitude(cb, lat, lon);
                targetWorld = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody = targetWorld - cb.position;
                return true;
            }

            // Land Anywhere mode: once selected, hold the lat/lon fixed.
            if (_landAnywhereTargetSet &&
                IsFinite(_landAnywhereLat) &&
                IsFinite(_landAnywhereLon))
            {
                lat = _landAnywhereLat;
                lon = _landAnywhereLon;

                double terrAlt = GetTerrainAltitude(cb, lat, lon);
                targetWorld = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody = targetWorld - cb.position;

                _worldTargetPos = targetWorld;
                return true;
            }

            // Land Anywhere has not been solved/locked yet.
            return false;
        }

        private bool SolverFrame(Vector3d pos, Vector3d vel, Vector3d targetPos, out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;
            Vector3d d = targetPos - pos;
            Vector3d d_horiz  = d - Vector3d.Dot(d, yHat) * yHat;
            Vector3d horizVel = Vector3d.Exclude(yHat, vel);
            xHat = d_horiz.magnitude > 0.1
                ? d_horiz.normalized
                : (horizVel.magnitude > 1e-6 ? horizVel.normalized : Vector3d.right);
            zHat = Vector3d.Cross(yHat, xHat);
            if (zHat.magnitude < 1e-6) zHat = Vector3d.forward;
            zHat = zHat.normalized;
            xHat = Vector3d.Cross(zHat, yHat).normalized;
            return xHat.magnitude > 1e-6 && yHat.magnitude > 1e-6 && zHat.magnitude > 1e-6;
        }

        private bool VelocityAlignedFrame(Vector3d pos, Vector3d vel, out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;

            Vector3d horizVel = Vector3d.Exclude(yHat, vel);
            if (horizVel.magnitude < 1e-3)
            {
                xHat = Vector3d.right;
                zHat = Vector3d.forward;
                return false;
            }

            xHat = horizVel.normalized;
            zHat = Vector3d.Cross(yHat, xHat);

            if (zHat.magnitude < 1e-6)
            {
                zHat = Vector3d.forward;
                return false;
            }

            zHat = zHat.normalized;
            xHat = Vector3d.Cross(zHat, yHat).normalized;

            return xHat.magnitude > 1e-6 && yHat.magnitude > 1e-6 && zHat.magnitude > 1e-6;
        }

        public bool TryGetTargetLatLon(out double lat, out double lon)
        {
            if (!TryGetEffectiveTarget(out lat, out lon, out _, out _)) return false;
            return true;
        }
    }

    
}