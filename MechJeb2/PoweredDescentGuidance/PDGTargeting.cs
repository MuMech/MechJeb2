using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop
    {
        // =========================================================================
        // Targeting
        // =========================================================================

        /// <summary>
        /// Attempts to set the "land anywhere" target by using <paramref name="downrange"/>
        /// (from a coast solve) as the landing point. Queries terrain, adds clearance,
        /// and locks the resulting lat/lon.
        /// </summary>
        private bool SetLandAnywhereTargetFromCoastSolution(
            double downrange, Vector3d xHat, Vector3d yHat, CelestialBody body)
        {
            if (LandAtTarget) return false;

            if (!IsFinite(downrange) || downrange <= 0.0)
            {
                Status = $"Land Anywhere: invalid coast x={downrange:F3}";
                return false;
            }
            if (body == null || xHat.magnitude < 1e-6 || yHat.magnitude < 1e-6)
            {
                Status = "Land Anywhere: invalid frame.";
                return false;
            }

            xHat = xHat.normalized;
            yHat = yHat.normalized;

            // Coarse lat/lon guess from predicted downrange.
            Vector3d relGuess   = downrange * xHat + body.Radius * yHat;
            body.GetLatLonAlt(body.position + relGuess, out double guessLat, out double guessLon, out _);
            if (!IsFinite(guessLat) || !IsFinite(guessLon))
            {
                Status = "Land Anywhere: invalid guessed lat/lon.";
                return false;
            }

            // Refine using terrain height at the guessed location.
            double guessTerrainAlt    = GetTerrainAltitude(body, guessLat, guessLon);
            double guessTargetRadius  = body.Radius + guessTerrainAlt + TargetClearance;
            Vector3d relTargetGuess   = downrange * xHat + guessTargetRadius * yHat;
            body.GetLatLonAlt(body.position + relTargetGuess, out double finalLat, out double finalLon, out _);
            if (!IsFinite(finalLat) || !IsFinite(finalLon))
            {
                Status = "Land Anywhere: invalid final lat/lon.";
                return false;
            }

            // Re-query terrain at the refined location and build the world position.
            double finalTerrainAlt = GetTerrainAltitude(body, finalLat, finalLon);
            _worldTargetPos = body.GetWorldSurfacePosition(finalLat, finalLon, finalTerrainAlt + TargetClearance);

            _landAnywhereLat       = finalLat;
            _landAnywhereLon       = finalLon;
            _landAnywhereTargetSet = true;

            Core.Target.SetPositionTarget(body, finalLat, finalLon);

            Status = $"Land Anywhere target set. x={downrange:F0}m " +
                     $"terrain={finalTerrainAlt:F1}m clearance={TargetClearance:F1}m " +
                     $"lat={finalLat:F4} lon={finalLon:F4}";
            return true;
        }

        /// <summary>
        /// Nudges the current target by <paramref name="downrangeMeters"/> (positive = away from vessel)
        /// and <paramref name="crossrangeMeters"/> (positive = right when facing downrange).
        /// Has no effect if no target is currently locked.
        /// </summary>
        public void NudgeTargetMeters(double downrangeMeters, double crossrangeMeters)
        {
            CelestialBody cb = Vessel.mainBody;
            if (!TryGetEffectiveTarget(out _, out _, out Vector3d targetPosBody, out _)) return;

            Vector3d targetUp = targetPosBody.normalized;
            if (targetUp.sqrMagnitude < 1e-10) return;

            Vector3d vesselBCI = Vessel.GetWorldPos3D() - cb.position;
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

            if (LandAtTarget)
                Core.Target.SetPositionTarget(cb, newLat, newLon);
        }

        /// <summary>
        /// Resolves the active landing target to a world position and frame.
        /// Returns false if no valid target is available yet.
        /// </summary>
        private bool TryGetEffectiveTarget(
            out double lat, out double lon,
            out Vector3d targetPosBody, out Vector3d targetWorld)
        {
            CelestialBody cb = MainBody;
            lat = lon = 0.0;
            targetPosBody = targetWorld = Vector3d.zero;
            if (cb == null) return false;

            if (LandAtTarget)
            {
                lat = Core.Target.targetLatitude;
                lon = Core.Target.targetLongitude;
                if (!IsFinite(lat) || !IsFinite(lon)) return false;
                double terrAlt = GetTerrainAltitude(cb, lat, lon);
                targetWorld    = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody  = targetWorld - cb.position;
                return true;
            }

            if (_landAnywhereTargetSet && IsFinite(_landAnywhereLat) && IsFinite(_landAnywhereLon))
            {
                lat = _landAnywhereLat;
                lon = _landAnywhereLon;
                double terrAlt  = GetTerrainAltitude(cb, lat, lon);
                targetWorld     = cb.GetWorldSurfacePosition(lat, lon, terrAlt + TargetClearance);
                targetPosBody   = targetWorld - cb.position;
                _worldTargetPos = targetWorld;
                return true;
            }

            return false; // target not yet established
        }

        /// <summary>Returns the current target lat/lon, or false if none is set.</summary>
        public bool TryGetTargetLatLon(out double lat, out double lon)
            => TryGetEffectiveTarget(out lat, out lon, out _, out _);

        // =========================================================================
        // Solver frame helpers
        // =========================================================================

        /// <summary>
        /// Builds an orthonormal frame with x̂ pointing toward the target's horizontal
        /// component and ŷ pointing radially outward.
        /// </summary>
        private static bool SolverFrame(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;

            Vector3d d      = targetPos - pos;
            Vector3d dHoriz = d - Vector3d.Dot(d, yHat) * yHat;
            Vector3d hVel   = Vector3d.Exclude(yHat, vel);

            xHat = dHoriz.magnitude > 0.1
                ? dHoriz.normalized
                : hVel.magnitude > 1e-6 ? hVel.normalized : Vector3d.right;

            zHat = Vector3d.Cross(yHat, xHat);
            if (zHat.magnitude < 1e-6) zHat = Vector3d.forward;
            zHat = zHat.normalized;
            xHat = Vector3d.Cross(zHat, yHat).normalized;

            return xHat.magnitude > 1e-6 && yHat.magnitude > 1e-6 && zHat.magnitude > 1e-6;
        }

        /// <summary>
        /// Builds an orthonormal frame aligned with the horizontal velocity vector.
        /// Returns false (and falls back to a fixed frame) if velocity is too small.
        /// </summary>
        private static bool VelocityAlignedFrame(
            Vector3d pos, Vector3d vel,
            out Vector3d xHat, out Vector3d yHat, out Vector3d zHat)
        {
            yHat = pos.magnitude > 1e-6 ? pos.normalized : Vector3d.up;

            Vector3d hVel = Vector3d.Exclude(yHat, vel);
            if (hVel.magnitude < 1e-3)
            {
                xHat = Vector3d.right;
                zHat = Vector3d.forward;
                return false;
            }

            xHat = hVel.normalized;
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

        // =========================================================================
        // Terrain query
        // =========================================================================

        /// <summary>
        /// Returns the terrain altitude at the given lat/lon, preferring PQS if available.
        /// </summary>
        private static double GetTerrainAltitude(CelestialBody body, double lat, double lon)
        {
            if (body.pqsController != null)
            {
                Vector3d radial = body.GetRelSurfaceNVector(lat, lon);
                return body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
            }
            return body.TerrainAltitude(lat, lon);
        }
    }

    
}