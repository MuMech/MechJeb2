using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class GLUtils
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

        public static void DrawMapViewGroundMarker(CelestialBody body, double latitude, double longitude, Color c)
        {            
            Vector3d up = body.GetSurfaceNVector(latitude, longitude);
            Vector3d center = body.position + body.Radius * up;

            if (Vector3d.Dot(up, ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position) - center) < 0) return; //target occluded by planet

            Vector3d north = Vector3d.Exclude(up, body.transform.up).normalized;

            double radius = body.Radius / 15;

            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(-10, up) * north),
                center + radius * (QuaternionD.AngleAxis(+10, up) * north)
            }, Color.red);

            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(110, up) * north),
                center + radius * (QuaternionD.AngleAxis(130, up) * north)
            }, Color.red);


            GLTriangleMap(new Vector3d[]{
                center,
                center + radius * (QuaternionD.AngleAxis(230, up) * north),
                center + radius * (QuaternionD.AngleAxis(250, up) * north)
            }, Color.red);
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
    }
}
