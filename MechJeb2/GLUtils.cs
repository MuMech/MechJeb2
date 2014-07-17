using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class GLUtils
    {
        static Material _material;
        static Material material
        {
            get
            {
                if (_material == null) _material = new Material(Shader.Find("Particles/Additive"));
                return _material;
            }
        }

        public static void DrawMapViewGroundMarker(CelestialBody body, double latitude, double longitude, Color c, double rotation = 0, double radius = 0)
        {
            Vector3d up = body.GetSurfaceNVector(latitude, longitude);
            var height = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right);
            if (height < body.Radius) { height = body.Radius; }
            Vector3d center = body.position + height * up;

            if (IsOccluded(center, body)) return;

            Vector3d north = Vector3d.Exclude(up, body.transform.up).normalized;

            if (radius <= 0) { radius = body.Radius / 15; }

            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(rotation - 10, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation + 10, up) * north)
            }, c);

            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(rotation + 110, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation + 130, up) * north)
            }, c);

            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(rotation - 110, up) * north),
                center + radius * (QuaternionD.AngleAxis(rotation - 130, up) * north)
            }, c);
        }

        public static void GLTriangleMap(Vector3d[] worldVertices, Color c)
        {
            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            GL.Color(c);
            GLVertexMap(worldVertices[0]);
            GLVertexMap(worldVertices[1]);
            GLVertexMap(worldVertices[2]);
            GL.End();
            GL.PopMatrix();
        }

        public static void GLVertexMap(Vector3d worldPosition)
        {
            Vector3 screenPoint = PlanetariumCamera.Camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(worldPosition));
            GL.Vertex3(screenPoint.x / Camera.main.pixelWidth, screenPoint.y / Camera.main.pixelHeight, 0);
        }

        //Tests if byBody occludes worldPosition, from the perspective of the planetarium camera
        public static bool IsOccluded(Vector3d worldPosition, CelestialBody byBody)
        {
            if (Vector3d.Distance(worldPosition, byBody.position) < byBody.Radius - 100) return true;

            Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);

            if (Vector3d.Angle(camPos - worldPosition, byBody.position - worldPosition) > 90) return false;

            double bodyDistance = Vector3d.Distance(camPos, byBody.position);
            double separationAngle = Vector3d.Angle(worldPosition - camPos, byBody.position - camPos);
            double altitude = bodyDistance * Math.Sin(Math.PI / 180 * separationAngle);
            return (altitude < byBody.Radius);
        }

        //If dashed = false, draws 0-1-2-3-4-5...
        //If dashed = true, draws 0-1 2-3 4-5...
        public static void DrawPath(CelestialBody mainBody, List<Vector3d> points, Color c, bool dashed = false)
        {
            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            GL.Color(c);

            int step = (dashed ? 2 : 1);
            for (int i = 0; i < points.Count() - 1; i += step)
            {
                if (!IsOccluded(points[i], mainBody) && !IsOccluded(points[i + 1], mainBody))
                {
                    GLVertexMap(points[i]);
                    GLVertexMap(points[i + 1]);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void DrawBoundingBox(CelestialBody mainBody, Vessel vessel, MechJebModuleDockingAutopilot.Box3d box, Color c )
        {
            //Vector3d origin = vessel.GetWorldPos3D() - vessel.GetTransform().rotation * box.center ;
            //Vector3d origin = vessel.GetTransform().TransformPoint(box.center);
            Vector3d origin = vessel.transform.TransformPoint(box.center);

            Vector3d A1 = origin + vessel.transform.rotation * new Vector3d(+box.size.x, +box.size.y, +box.size.z);
            Vector3d A2 = origin + vessel.transform.rotation * new Vector3d(+box.size.x, -box.size.y, +box.size.z);
            Vector3d A3 = origin + vessel.transform.rotation * new Vector3d(-box.size.x, -box.size.y, +box.size.z);
            Vector3d A4 = origin + vessel.transform.rotation * new Vector3d(-box.size.x, +box.size.y, +box.size.z);

            Vector3d B1 = origin + vessel.transform.rotation * new Vector3d(+box.size.x, +box.size.y, -box.size.z);
            Vector3d B2 = origin + vessel.transform.rotation * new Vector3d(+box.size.x, -box.size.y, -box.size.z);
            Vector3d B3 = origin + vessel.transform.rotation * new Vector3d(-box.size.x, -box.size.y, -box.size.z);
            Vector3d B4 = origin + vessel.transform.rotation * new Vector3d(-box.size.x, +box.size.y, -box.size.z);

            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            GL.Color(c);

            GLVertexMap(A1);
            GLVertexMap(A2);
            
            GLVertexMap(A2);
            GLVertexMap(A3);

            GLVertexMap(A3);
            GLVertexMap(A4);

            GLVertexMap(A4);
            GLVertexMap(A1);

            GLVertexMap(B1);
            GLVertexMap(B2);
            
            GLVertexMap(B2);
            GLVertexMap(B3);

            GLVertexMap(B3);
            GLVertexMap(B4);

            GLVertexMap(B4);
            GLVertexMap(B1);

            GLVertexMap(A1);
            GLVertexMap(B1);

            GLVertexMap(A2);
            GLVertexMap(B2);

            GLVertexMap(A3);
            GLVertexMap(B3);

            GLVertexMap(A4);
            GLVertexMap(B4);

            GL.End();
            GL.PopMatrix();
        }

        public static void DrawOrbit(Orbit o, Color c)
        {
            List<Vector3d> points = new List<Vector3d>();
            if (o.eccentricity < 1)
            {
                //elliptical orbits:
                for (int trueAnomaly = 0; trueAnomaly < 360; trueAnomaly += 1)
                {
                    points.Add(o.SwappedAbsolutePositionAtUT(o.TimeOfTrueAnomaly(trueAnomaly, 0)));
                }
                points.Add(points[0]); //close the loop
            }
            else
            {
                //hyperbolic orbits:
                for (int meanAnomaly = -1000; meanAnomaly <= 1000; meanAnomaly += 5)
                {
                    points.Add(o.SwappedAbsolutePositionAtUT(o.UTAtMeanAnomaly(meanAnomaly * Math.PI / 180, 0)));
                }
            }

            DrawPath(o.referenceBody, points, c, false);
        }
    }
}
