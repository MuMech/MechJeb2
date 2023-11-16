using System.Collections.Generic;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public static class GLUtils
    {
        private static Material _materialInternal;

        private static Material _material =>
            _materialInternal ?? (_materialInternal = new Material(Shader.Find("Legacy Shaders/Particles/Additive")));

        public static void DrawMapViewGroundMarker(CelestialBody body, double latitude, double longitude, Color c, double rotation = 0,
            double radius = 0) =>
            DrawGroundMarker(body, latitude, longitude, c, true, rotation, radius);

        public static void DrawGroundMarker(CelestialBody body, double latitude, double longitude, Color c, bool map, double rotation = 0,
            double radius = 0)
        {
            Vector3d up = body.GetSurfaceNVector(latitude, longitude);
            double height = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(longitude, Vector3d.down) *
                                                                QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right);
            if (height < body.Radius) { height = body.Radius; }

            Vector3d center = body.position + height * up;

            Vector3d camPos = map
                ? ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position)
                : (Vector3d)FlightCamera.fetch.mainCamera.transform.position;

            if (IsOccluded(center, body, camPos)) return;

            Vector3d north = Vector3d.Exclude(up, body.transform.up).normalized;

            if (radius <= 0) { radius = map ? body.Radius / 15 : 5; }

            if (!map)
            {
                Vector3 centerPoint = FlightCamera.fetch.mainCamera.WorldToViewportPoint(center);
                if (centerPoint.z < 0)
                    return;
            }

            GLTriangle(
                center,
                center + radius * (QuaternionD.AngleAxis(rotation - 10, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation + 10, up) * north)
                , c, map);

            GLTriangle(
                center,
                center + radius * (QuaternionD.AngleAxis(rotation + 110, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation + 130, up) * north)
                , c, map);

            GLTriangle(
                center,
                center + radius * (QuaternionD.AngleAxis(rotation - 110, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation - 130, up) * north)
                , c, map);
        }

        private static void GLTriangle(Vector3d worldVertices1, Vector3d worldVertices2, Vector3d worldVertices3, Color c, bool map)
        {
            GL.PushMatrix();
            _material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            GL.Color(c);
            GLVertex(worldVertices1, map);
            GLVertex(worldVertices2, map);
            GLVertex(worldVertices3, map);
            GL.End();
            GL.PopMatrix();
        }

        private static void GLVertex(Vector3d worldPosition, bool map = false)
        {
            Vector3 screenPoint =
                map
                    ? PlanetariumCamera.Camera.WorldToViewportPoint(ScaledSpace.LocalToScaledSpace(worldPosition))
                    : FlightCamera.fetch.mainCamera.WorldToViewportPoint(worldPosition);
            GL.Vertex3(screenPoint.x, screenPoint.y, 0);
        }

        private static void GLPixelLine(Vector3d worldPosition1, Vector3d worldPosition2, bool map)
        {
            Vector3 screenPoint1, screenPoint2;
            if (map)
            {
                screenPoint1 = PlanetariumCamera.Camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(worldPosition1));
                screenPoint2 = PlanetariumCamera.Camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(worldPosition2));
            }
            else
            {
                screenPoint1 = FlightCamera.fetch.mainCamera.WorldToScreenPoint(worldPosition1);
                screenPoint2 = FlightCamera.fetch.mainCamera.WorldToScreenPoint(worldPosition2);
            }

            if (screenPoint1.z > 0 && screenPoint2.z > 0)
            {
                GL.Vertex3(screenPoint1.x, screenPoint1.y, 0);
                GL.Vertex3(screenPoint2.x, screenPoint2.y, 0);
            }
        }

        //Tests if byBody occludes worldPosition, from the perspective of the planetarium camera
        // https://cesiumjs.org/2013/04/25/Horizon-culling/
        private static bool IsOccluded(Vector3d worldPosition, CelestialBody byBody, Vector3d camPos)
        {
            Vector3d vc = (byBody.position - camPos) / (byBody.Radius - 100);
            Vector3d vt = (worldPosition - camPos) / (byBody.Radius - 100);

            double vtVc = Vector3d.Dot(vt, vc);

            // In front of the horizon plane
            if (vtVc < vc.sqrMagnitude - 1) return false;

            return vtVc * vtVc / vt.sqrMagnitude > vc.sqrMagnitude - 1;
        }

        //If dashed = false, draws 0-1-2-3-4-5...
        //If dashed = true, draws 0-1 2-3 4-5...
        public static void DrawPath(CelestialBody mainBody, List<Vector3d> points, Color c, bool map, bool dashed = false)
        {
            GL.PushMatrix();
            _material.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);
            GL.Color(c);

            Vector3d camPos = map
                ? ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position)
                : (Vector3d)FlightCamera.fetch.mainCamera.transform.position;

            int step = dashed ? 2 : 1;
            for (int i = 0; i < points.Count - 1; i += step)
            {
                if (!IsOccluded(points[i], mainBody, camPos) && !IsOccluded(points[i + 1], mainBody, camPos))
                {
                    GLPixelLine(points[i], points[i + 1], map);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void DrawBoundingBox(CelestialBody mainBody, Vessel vessel, MechJebModuleDockingAutopilot.Box3d box, Color c)
        {
            //Vector3d origin = vessel.GetWorldPos3D() - vessel.GetTransform().rotation * box.center ;
            //Vector3d origin = vessel.GetTransform().TransformPoint(box.center);
            Transform transform;
            Vector3d origin = (transform = vessel.transform).TransformPoint(box.center);

            Quaternion rotation = transform.rotation;

            Vector3d a1 = origin + rotation * new Vector3d(+box.size.x, +box.size.y, +box.size.z);
            Vector3d a2 = origin + rotation * new Vector3d(+box.size.x, -box.size.y, +box.size.z);
            Vector3d a3 = origin + rotation * new Vector3d(-box.size.x, -box.size.y, +box.size.z);
            Vector3d a4 = origin + rotation * new Vector3d(-box.size.x, +box.size.y, +box.size.z);

            Vector3d b1 = origin + rotation * new Vector3d(+box.size.x, +box.size.y, -box.size.z);
            Vector3d b2 = origin + rotation * new Vector3d(+box.size.x, -box.size.y, -box.size.z);
            Vector3d b3 = origin + rotation * new Vector3d(-box.size.x, -box.size.y, -box.size.z);
            Vector3d b4 = origin + rotation * new Vector3d(-box.size.x, +box.size.y, -box.size.z);

            GL.PushMatrix();
            _material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            GL.Color(c);

            GLVertex(a1);
            GLVertex(a2);

            GLVertex(a2);
            GLVertex(a3);

            GLVertex(a3);
            GLVertex(a4);

            GLVertex(a4);
            GLVertex(a1);

            GLVertex(b1);
            GLVertex(b2);

            GLVertex(b2);
            GLVertex(b3);

            GLVertex(b3);
            GLVertex(b4);

            GLVertex(b4);
            GLVertex(b1);

            GLVertex(a1);
            GLVertex(b1);

            GLVertex(a2);
            GLVertex(b2);

            GLVertex(a3);
            GLVertex(b3);

            GLVertex(a4);
            GLVertex(b4);

            GL.End();
            GL.PopMatrix();
        }

        private static readonly List<Vector3d> _points = new List<Vector3d>();

        public static void DrawOrbit(Orbit o, Color c)
        {
            _points.Clear();

            if (o.eccentricity < 1)
            {
                //elliptical orbits:
                for (int trueAnomaly = 0; trueAnomaly < 360; trueAnomaly += 1)
                {
                    _points.Add(o.WorldPositionAtUT(o.TimeOfTrueAnomaly(Deg2Rad(trueAnomaly), 0)));
                }

                _points.Add(_points[0]); //close the loop
            }
            else
            {
                //hyperbolic orbits:
                for (int meanAnomaly = -1000; meanAnomaly <= 1000; meanAnomaly += 5)
                {
                    _points.Add(o.WorldPositionAtUT(o.UTAtMeanAnomaly(meanAnomaly * UtilMath.Deg2Rad, 0)));
                }
            }

            DrawPath(o.referenceBody, _points, c, true);
        }
    }
}
