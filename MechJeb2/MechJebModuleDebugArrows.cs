using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MuMech
{

    class MechJebModuleDebugArrows : ComputerModule
    {
        [Persistent(pass = (int)Pass.Global)]
        public bool srfVelocityArrowActive;
        public DebugArrow srfVelocityArrow;
        
        [Persistent(pass = (int)Pass.Global)]
        public bool obtVelocityArrowActive;
        public DebugArrow obtVelocityArrow;
        
        [Persistent(pass = (int)Pass.Global)]
        public bool forwardArrowActive;
        public DebugArrow forwardArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool avgForwardArrowActive;
        public DebugArrow avgForwardArrow;

        public MechJebModuleDebugArrows(MechJebCore core) : base(core)
        {
            enabled = true;
        }

        public override void OnUpdate()
        {
            if (srfVelocityArrow == null)
            {
                srfVelocityArrow = new DebugArrow(Color.yellow);
                obtVelocityArrow = new DebugArrow(Color.red);
                forwardArrow = new DebugArrow(Color.green);
                avgForwardArrow = new DebugArrow(Color.blue);
            }

            srfVelocityArrow.Set(vessel.GetReferenceTransformPart().transform.position, vessel.srf_velocity);
            srfVelocityArrow.State(srfVelocityArrowActive);

            obtVelocityArrow.Set(vessel.GetReferenceTransformPart().transform.position, vessel.obt_velocity);
            obtVelocityArrow.State(obtVelocityArrowActive);

            forwardArrow.Set(vessel.GetReferenceTransformPart().transform.position, vessel.GetTransform().up);
            forwardArrow.State(forwardArrowActive);

            avgForwardArrow.Set(vessel.GetReferenceTransformPart().transform.position, vesselState.forward);
            avgForwardArrow.State(avgForwardArrowActive);
        }
    }


    class DebugArrow
    {

        private GameObject gameObject;
        private GameObject haft;
        private GameObject cone;

        public DebugArrow(Color color)
        {
            gameObject = new GameObject("DebugArrow");

            haft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            //foreach (var component in haft.GetComponents(typeof(Component)))
            //{
            //    MechJebCore.print(component.GetType() + " " + component.name);
            //}

            haft.collider.enabled = false;

            haft.transform.parent = gameObject.transform;
            haft.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
            haft.transform.localPosition = new Vector3(0f, 0f, 2f);
            haft.transform.localRotation = Quaternion.Euler(90, 0, 0);

            cone = CreateCone(0.5f, 0.15f, 0f, 20);
            cone.transform.parent = gameObject.transform;
            cone.transform.localRotation = Quaternion.Euler(90, 0, 0);
            cone.transform.localPosition = new Vector3(0f, 0f, 4f);

            MeshRenderer haftMeshRenderer = haft.GetComponent<MeshRenderer>();
            MeshRenderer coneMeshRenderer = cone.AddComponent<MeshRenderer>();

            haftMeshRenderer.material = new Material(Shader.Find("KSP/Unlit")) { color = color };
            haftMeshRenderer.castShadows = false;
            haftMeshRenderer.receiveShadows = false;
            //haftMeshRenderer.material.color = color;

            coneMeshRenderer.material = new Material(Shader.Find("KSP/Unlit")) { color = color };
            coneMeshRenderer.castShadows = false;
            coneMeshRenderer.receiveShadows = false;
            //coneMeshRenderer.material.color = color;

            //haft.SetActive(true);
            //cone.SetActive(true);
        }

        public void Set(Vector3d position, Vector3d direction)
        {
            gameObject.transform.position = position;
            gameObject.transform.rotation = Quaternion.LookRotation(direction);
        }

        public void State(bool state)
        {
            gameObject.SetActive(state);
        }

        private GameObject CreateCone(float height, float bottomRadius, float topRadius, int nbSides)
        {
            cone = new GameObject();
            MeshFilter filter = cone.AddComponent<MeshFilter>();
            Mesh mesh = filter.mesh;
            mesh.Clear();

            int nbVerticesCap = nbSides + 1;

            #region Vertices

            // bottom + top + sides
            Vector3[] vertices = new Vector3[nbVerticesCap + nbVerticesCap + nbSides * 2 + 2];
            int vert = 0;
            float _2pi = Mathf.PI * 2f;

            // Bottom cap
            vertices[vert++] = new Vector3(0f, 0f, 0f);
            while (vert <= nbSides)
            {
                float rad = (float)vert / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
                vert++;
            }

            // Top cap
            vertices[vert++] = new Vector3(0f, height, 0f);
            while (vert <= nbSides * 2 + 1)
            {
                float rad = (float)(vert - nbSides - 1) / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
                vert++;
            }

            // Sides
            int v = 0;
            while (vert <= vertices.Length - 4)
            {
                float rad = (float)v / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
                vertices[vert + 1] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
                vert += 2;
                v++;
            }
            vertices[vert] = vertices[nbSides * 2 + 2];
            vertices[vert + 1] = vertices[nbSides * 2 + 3];

            #endregion

            #region Normales

            // bottom + top + sides
            Vector3[] normales = new Vector3[vertices.Length];
            vert = 0;

            // Bottom cap
            while (vert <= nbSides)
            {
                normales[vert++] = Vector3.down;
            }

            // Top cap
            while (vert <= nbSides * 2 + 1)
            {
                normales[vert++] = Vector3.up;
            }

            // Sides
            v = 0;
            while (vert <= vertices.Length - 4)
            {
                float rad = (float)v / nbSides * _2pi;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                normales[vert] = new Vector3(cos, 0f, sin);
                normales[vert + 1] = normales[vert];

                vert += 2;
                v++;
            }
            normales[vert] = normales[nbSides * 2 + 2];
            normales[vert + 1] = normales[nbSides * 2 + 3];

            #endregion

            #region UVs

            Vector2[] uvs = new Vector2[vertices.Length];

            // Bottom cap
            int u = 0;
            uvs[u++] = new Vector2(0.5f, 0.5f);
            while (u <= nbSides)
            {
                float rad = (float)u / nbSides * _2pi;
                uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
                u++;
            }

            // Top cap
            uvs[u++] = new Vector2(0.5f, 0.5f);
            while (u <= nbSides * 2 + 1)
            {
                float rad = (float)u / nbSides * _2pi;
                uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
                u++;
            }

            // Sides
            int u_sides = 0;
            while (u <= uvs.Length - 4)
            {
                float t = (float)u_sides / nbSides;
                uvs[u] = new Vector3(t, 1f);
                uvs[u + 1] = new Vector3(t, 0f);
                u += 2;
                u_sides++;
            }
            uvs[u] = new Vector2(1f, 1f);
            uvs[u + 1] = new Vector2(1f, 0f);

            #endregion

            #region Triangles

            int nbTriangles = nbSides + nbSides + nbSides * 2;
            int[] triangles = new int[nbTriangles * 3 + 3];

            // Bottom cap
            int tri = 0;
            int i = 0;
            while (tri < nbSides - 1)
            {
                triangles[i] = 0;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = tri + 2;
                tri++;
                i += 3;
            }
            triangles[i] = 0;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = 1;
            tri++;
            i += 3;

            // Top cap
            //tri++;
            while (tri < nbSides * 2)
            {
                triangles[i] = tri + 2;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = nbVerticesCap;
                tri++;
                i += 3;
            }

            triangles[i] = nbVerticesCap + 1;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = nbVerticesCap;
            tri++;
            i += 3;
            tri++;

            // Sides
            while (tri <= nbTriangles)
            {
                triangles[i] = tri + 2;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = tri + 0;
                tri++;
                i += 3;

                triangles[i] = tri + 1;
                triangles[i + 1] = tri + 2;
                triangles[i + 2] = tri + 0;
                tri++;
                i += 3;
            }

            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            mesh.Optimize();


            return cone;
        }


    }
}