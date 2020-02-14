using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MuMech
{

    class MechJebModuleDebugArrows : ComputerModule
    {

        [Persistent(pass = (int)Pass.Global)]
        public bool displayAtCoM;

        [Persistent(pass = (int)Pass.Global)]
        public bool seeThrough;


        [Persistent(pass = (int)Pass.Global)]
        public bool comSphereActive;
        public static DebugIcoSphere comSphere;


        [Persistent(pass = (int)Pass.Global)]
        public bool colSphereActive;
        public static DebugIcoSphere colSphere;

        [Persistent(pass = (int)Pass.Global)]
        public bool cotSphereActive;
        public static DebugIcoSphere cotSphere;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble comSphereRadius = new EditableDouble(0.09);

        [Persistent(pass = (int)Pass.Global)]
        public bool srfVelocityArrowActive;
        public static DebugArrow srfVelocityArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool obtVelocityArrowActive;
        public static DebugArrow obtVelocityArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool dotArrowActive;
        public static DebugArrow dotArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool forwardArrowActive;
        public static DebugArrow forwardArrow;

        // Not used since I did not write the code for that one yet.
        //[Persistent(pass = (int)Pass.Global)]
        //public bool avgForwardArrowActive;
        //public static DebugArrow avgForwardArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool requestedAttitudeArrowActive;
        public static DebugArrow requestedAttitudeArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool debugArrowActive;
        public static DebugArrow debugArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool debugArrow2Active;
        public static DebugArrow debugArrow2;


        public static Vector3d debugVector = Vector3d.one;
        public static Vector3d debugVector2 = Vector3d.one;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble arrowsLength = new EditableDouble(4);

        public MechJebModuleDebugArrows(MechJebCore core) : base(core)
        {
            enabled = true;
        }

        public override void OnDestroy()
        {
            // Dirty cleanup. I m spending too much time on this already
            if (comSphere == null)
                return;

            comSphere.Destroy();
            comSphere = null;

            colSphere.Destroy();
            colSphere = null;
            cotSphere.Destroy();
            cotSphere = null;

            srfVelocityArrow.Destroy();
            srfVelocityArrow = null;

            obtVelocityArrow.Destroy();
            obtVelocityArrow = null;

            dotArrow.Destroy();
            dotArrow = null;

            forwardArrow.Destroy();
            forwardArrow = null;
            //avgForwardArrow.Destroy();
            //avgForwardArrow = null;

            requestedAttitudeArrow.Destroy();
            requestedAttitudeArrow = null;

            debugArrow.Destroy();
            debugArrow = null;

            debugArrow2.Destroy();
            debugArrow2 = null;
        }

        // TODO : I should probably use an array and an enum to lower code dup ...
        public override void OnUpdate()
        {
            if (vessel != FlightGlobals.ActiveVessel)
                return;

            if (comSphere == null)
            {
                comSphere = new DebugIcoSphere(XKCDColors.BloodRed, true);

                colSphere = new DebugIcoSphere(XKCDColors.Teal, true);
                cotSphere = new DebugIcoSphere(XKCDColors.PurplePink, true);

                srfVelocityArrow = new DebugArrow(Color.green);
                obtVelocityArrow = new DebugArrow(Color.red);

                dotArrow        = new DebugArrow(XKCDColors.PurplePink);

                forwardArrow = new DebugArrow(XKCDColors.ElectricBlue);
                //avgForwardArrow = new DebugArrow(Color.blue);

                requestedAttitudeArrow = new DebugArrow(Color.gray);

                debugArrow = new DebugArrow(XKCDColors.Fuchsia);
                debugArrow2 = new DebugArrow(XKCDColors.LightBlue);
            }


            var frameVel = (vesselState.orbitalVelocity - Krakensbane.GetFrameVelocity() - vessel.orbit.GetRotFrameVel(vessel.orbit.referenceBody).xzy) * Time.fixedDeltaTime;
            Vector3d instantCoM = vesselState.CoM + frameVel;

            Vector3 arrowPos = displayAtCoM
                ? instantCoM
                : (Vector3d)vessel.ReferenceTransform.position;

            comSphere.State(comSphereActive && core.ShowGui);
            if (comSphereActive)
            {
                comSphere.Set(instantCoM);
                comSphere.SetRadius((float)comSphereRadius.val);
            }

            colSphere.State(colSphereActive && vesselState.CoLScalar > 0 && core.ShowGui);
            if (colSphereActive)
            {
                colSphere.Set(vesselState.CoL + frameVel);
                colSphere.SetRadius((float)comSphereRadius.val);
            }

            cotSphere.State(cotSphereActive && vesselState.CoTScalar > 0 && core.ShowGui);
            if (cotSphereActive)
            {
                cotSphere.Set(vesselState.CoT + frameVel);
                cotSphere.SetRadius((float)comSphereRadius.val);
            }

            srfVelocityArrow.State(srfVelocityArrowActive && core.ShowGui);
            if (srfVelocityArrowActive)
            {
                srfVelocityArrow.Set(arrowPos, vessel.srf_velocity);
                srfVelocityArrow.SetLength((float)arrowsLength.val);
                srfVelocityArrow.SeeThrough(seeThrough);
            }

            obtVelocityArrow.State(obtVelocityArrowActive && core.ShowGui);
            if (obtVelocityArrowActive)
            {
                obtVelocityArrow.Set(arrowPos, vessel.obt_velocity);
                obtVelocityArrow.SetLength((float)arrowsLength.val);
                obtVelocityArrow.SeeThrough(seeThrough);
            }

            dotArrow.State(dotArrowActive && vesselState.thrustCurrent > 0 && core.ShowGui);
            if (dotArrowActive)
            {
                dotArrow.Set(vesselState.CoT + frameVel, vesselState.DoT);
                dotArrow.SetLength((float)Math.Log10(vesselState.thrustCurrent + 1));
                dotArrow.SeeThrough(seeThrough);
            }

            forwardArrow.State(forwardArrowActive && core.ShowGui);
            if (forwardArrowActive)
            {
                forwardArrow.Set(arrowPos, vessel.GetTransform().up);
                forwardArrow.SetLength((float)arrowsLength.val);
                forwardArrow.SeeThrough(seeThrough);
            }

            /*
            avgForwardArrow.State(avgForwardArrowActive && core.ShowGui);
            if (avgForwardArrowActive)
            {
                avgForwardArrow.Set(arrowPos, vesselState.forward);
                avgForwardArrow.SetLength((float)arrowsLength.val);
                avgForwardArrow.SeeThrough(seeThrough);
            }
            */

            requestedAttitudeArrow.State(requestedAttitudeArrowActive && core.attitude.enabled && core.ShowGui);
            if (requestedAttitudeArrowActive && core.attitude.enabled)
            {
                requestedAttitudeArrow.Set(arrowPos, core.attitude.RequestedAttitude);
                requestedAttitudeArrow.SetLength((float)arrowsLength.val);
                requestedAttitudeArrow.SeeThrough(seeThrough);
            }

            debugArrow.State(debugArrowActive && core.ShowGui);
            if (debugArrowActive)
            {
                debugArrow.Set(vessel.ReferenceTransform.position, debugVector);
                debugArrow.SetLength((float)debugVector.magnitude);
                debugArrow.SeeThrough(seeThrough);
            }

            debugArrow2.State(debugArrow2Active && core.ShowGui);
            if (debugArrow2Active)
            {

                //debugArrow2.Set(vessel.ReferenceTransform.position, debugVector2);
                //
                //debugArrow2.SetLength((float)debugVector2.magnitude);
                //debugArrow2.SeeThrough(seeThrough);

                var vector3d =  vesselState.CoL - instantCoM + frameVel;
                debugArrow2.Set(instantCoM, vector3d);

                debugArrow2.SetLength((float)vector3d.magnitude);
                debugArrow2.SeeThrough(seeThrough);



            }
        }
    }

    class DebugArrow
    {
        private readonly GameObject gameObject;
        private readonly GameObject haft;
        private GameObject cone;

        private const float coneLength = 0.5f;
        private float length;
        private bool seeThrough = false;
        private readonly MeshRenderer _haftMeshRenderer;
        private readonly MeshRenderer _coneMeshRenderer;


        public DebugArrow(Color color, bool seeThrough = false)
        {
            gameObject = new GameObject("DebugArrow");
            gameObject.layer = 15; // Change layer. Not reentry effect that way (TODO :  try 22)

            haft = CreateCone(1f, 0.05f, 0.05f, 0f, 20);
            haft.transform.parent = gameObject.transform;
            haft.transform.localRotation = Quaternion.Euler(90, 0, 0);
            haft.layer = 15;

            cone = CreateCone(coneLength, 0.15f, 0f, 0f, 20);
            cone.transform.parent = gameObject.transform;
            cone.transform.localRotation = Quaternion.Euler(90, 0, 0);
            cone.layer = 15;

            SetLength(4);

            _haftMeshRenderer = haft.AddComponent<MeshRenderer>();
            _coneMeshRenderer = cone.AddComponent<MeshRenderer>();

            _haftMeshRenderer.material.color = color;
            _haftMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _haftMeshRenderer.receiveShadows = false;

            _coneMeshRenderer.material.color =  color;
            _coneMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _coneMeshRenderer.receiveShadows = false;

            SeeThrough(seeThrough);
        }

        public void Destroy()
        {
            if (gameObject != null)
                Object.Destroy(gameObject);
        }

        public void SeeThrough(bool state)
        {
            if (seeThrough != state)
            {
                seeThrough = state;
                _coneMeshRenderer.material.shader = state ? MechJebBundlesManager.diffuseAmbientIgnoreZ : MechJebBundlesManager.diffuseAmbient;
                _haftMeshRenderer.material.shader = state ? MechJebBundlesManager.diffuseAmbientIgnoreZ : MechJebBundlesManager.diffuseAmbient;
            }
        }


        public void SetLength(float length)
        {
            if (this.length == length)
                return;
            float conePos = length - coneLength;
            if (conePos > 0)
            {
                this.length = length;
                haft.transform.localScale = new Vector3(1f, conePos, 1f);
                cone.transform.localPosition = new Vector3(0f, 0f, conePos);
                cone.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                this.length = length;
                haft.transform.localScale = new Vector3(1f, 0, 1f);
                cone.transform.localPosition = new Vector3(0f, 0f, 0);
                cone.transform.localScale = new Vector3(length / coneLength, length / coneLength, length / coneLength);
            }
        }

        public void Set(Vector3d position, Vector3d direction)
        {
            if (direction.sqrMagnitude < 0.001)
            {
                State(false);
                return;
            }
            Set(position, Quaternion.LookRotation(direction));
        }

        public void Set(Vector3d position, Quaternion direction)
        {
            gameObject.transform.position = position;
            gameObject.transform.rotation = direction;
        }

        public void State(bool state)
        {
            if (state != gameObject.activeSelf)
                gameObject.SetActive(state);
        }

        // From http://wiki.unity3d.com/index.php/ProceduralPrimitives
        // TODO : merge with the cylinder code  on that page and create the full arrow instead of using 2 mesh.
        private GameObject CreateCone(float height, float bottomRadius, float topRadius, float offset, int nbSides)
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
            vertices[vert++] = new Vector3(0f, offset, 0f);
            while (vert <= nbSides)
            {
                float rad = (float)vert / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * bottomRadius, offset, Mathf.Sin(rad) * bottomRadius);
                vert++;
            }

            // Top cap
            vertices[vert++] = new Vector3(0f, offset + height, 0f);
            while (vert <= nbSides * 2 + 1)
            {
                float rad = (float)(vert - nbSides - 1) / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, offset + height, Mathf.Sin(rad) * topRadius);
                vert++;
            }

            // Sides
            int v = 0;
            while (vert <= vertices.Length - 4)
            {
                float rad = (float)v / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, offset + height, Mathf.Sin(rad) * topRadius);
                vertices[vert + 1] = new Vector3(Mathf.Cos(rad) * bottomRadius, offset, Mathf.Sin(rad) * bottomRadius);
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

            return cone;
        }


    }

    class DebugIcoSphere
    {
        private readonly GameObject gameObject;

        private readonly MeshRenderer _meshRenderer;

        private float radius;
        private bool seeThrough = false;

        public DebugIcoSphere(Color color, bool seeThrough = false)
        {
            gameObject = CreateIcoSphere(1);
            gameObject.layer = 15; // Change layer. Not reentry effect that way (TODO :  try 22)

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            _meshRenderer.material.color = color;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            SetRadius(0.09f);
            SeeThrough(seeThrough);
        }

        public void Destroy()
        {
            Object.Destroy(gameObject);
        }

        public void Set(Vector3d position)
        {
            gameObject.transform.position = position;
        }

        public void State(bool state)
        {
            if (state != gameObject.activeSelf)
                gameObject.SetActive(state);
        }

        public void SetRadius(float radius)
        {
            if (this.radius == radius || radius <= 0)
                return;
            this.radius = radius;
            gameObject.transform.localScale = new Vector3(radius, radius, radius);
        }

        public void SeeThrough(bool state)
        {
            if (seeThrough != state)
            {
                seeThrough = state;
                _meshRenderer.material.shader = state ? MechJebBundlesManager.diffuseAmbientIgnoreZ : MechJebBundlesManager.diffuseAmbient;
            }
        }

        private static GameObject CreateIcoSphere(float radius, int recursionLevel = 3)
        {
            GameObject gameObject = new GameObject("DebugIcoSphere");
            gameObject.layer = 15; // Change layer. Not reentry effect that way (TODO :  try 22)

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            Mesh mesh = filter.mesh;
            mesh.Clear();

            List<Vector3> vertList = new List<Vector3>();
            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();

            // create 12 vertices of a icosahedron
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            vertList.Add(new Vector3(-1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(-1f, -t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, -t, 0f).normalized * radius);

            vertList.Add(new Vector3(0f, -1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, -1f, -t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, -t).normalized * radius);

            vertList.Add(new Vector3(t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(t, 0f, 1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, 1f).normalized * radius);


            // create 20 triangles of the icosahedron
            List<TriangleIndices> faces = new List<TriangleIndices>();

            // 5 faces around point 0
            faces.Add(new TriangleIndices(0, 11, 5));
            faces.Add(new TriangleIndices(0, 5, 1));
            faces.Add(new TriangleIndices(0, 1, 7));
            faces.Add(new TriangleIndices(0, 7, 10));
            faces.Add(new TriangleIndices(0, 10, 11));

            // 5 adjacent faces
            faces.Add(new TriangleIndices(1, 5, 9));
            faces.Add(new TriangleIndices(5, 11, 4));
            faces.Add(new TriangleIndices(11, 10, 2));
            faces.Add(new TriangleIndices(10, 7, 6));
            faces.Add(new TriangleIndices(7, 1, 8));

            // 5 faces around point 3
            faces.Add(new TriangleIndices(3, 9, 4));
            faces.Add(new TriangleIndices(3, 4, 2));
            faces.Add(new TriangleIndices(3, 2, 6));
            faces.Add(new TriangleIndices(3, 6, 8));
            faces.Add(new TriangleIndices(3, 8, 9));

            // 5 adjacent faces
            faces.Add(new TriangleIndices(4, 9, 5));
            faces.Add(new TriangleIndices(2, 4, 11));
            faces.Add(new TriangleIndices(6, 2, 10));
            faces.Add(new TriangleIndices(8, 6, 7));
            faces.Add(new TriangleIndices(9, 8, 1));


            // refine triangles
            for (int i = 0; i < recursionLevel; i++)
            {
                List<TriangleIndices> faces2 = new List<TriangleIndices>();
                for (int j = 0; j < faces.Count; j++)
                {
                    var tri = faces[j];
                    // replace triangle by 4 triangles
                    int a = getMiddlePoint(tri.v1, tri.v2, ref vertList, ref middlePointIndexCache, radius);
                    int b = getMiddlePoint(tri.v2, tri.v3, ref vertList, ref middlePointIndexCache, radius);
                    int c = getMiddlePoint(tri.v3, tri.v1, ref vertList, ref middlePointIndexCache, radius);

                    faces2.Add(new TriangleIndices(tri.v1, a, c));
                    faces2.Add(new TriangleIndices(tri.v2, b, a));
                    faces2.Add(new TriangleIndices(tri.v3, c, b));
                    faces2.Add(new TriangleIndices(a, b, c));
                }
                faces = faces2;
            }

            mesh.vertices = vertList.ToArray();

            List<int> triList = new List<int>();
            for (int i = 0; i < faces.Count; i++)
            {
                triList.Add(faces[i].v1);
                triList.Add(faces[i].v2);
                triList.Add(faces[i].v3);
            }


            // The UV and normals are wrong, but it works for my needs.
            mesh.triangles = triList.ToArray();
            mesh.uv = new Vector2[vertList.Count];

            Vector3[] normales = new Vector3[vertList.Count];
            for (int i = 0; i < normales.Length; i++)
                normales[i] = vertList[i].normalized;


            mesh.normals = normales;

            mesh.RecalculateBounds();

            return gameObject;
        }

        private struct TriangleIndices
        {
            public int v1;
            public int v2;
            public int v3;

            public TriangleIndices(int v1, int v2, int v3)
            {
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
            }
        }

        // return index of point in the middle of p1 and p2
        private static int getMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius)
        {
            // first check if we have it already
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (cache.TryGetValue(key, out ret))
            {
                return ret;
            }

            // not in cache, calculate it
            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = new Vector3
                (
                 (point1.x + point2.x) / 2f,
                 (point1.y + point2.y) / 2f,
                 (point1.z + point2.z) / 2f
                );

            // add vertex makes sure point is on unit sphere
            int i = vertices.Count;
            vertices.Add(middle.normalized * radius);

            // store it, return index
            cache.Add(key, i);

            return i;
        }
    }


}
