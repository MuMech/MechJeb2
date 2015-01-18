using System.Text;
using UnityEngine;

namespace MuMech
{

    class MechJebModuleDebugArrows : ComputerModule
    {

        [Persistent(pass = (int)Pass.Global)]
        public bool displayAtCoM;

        [Persistent(pass = (int)Pass.Global)]
        public bool podSrfVelocityArrowActive;
        public DebugArrow podSrfVelocityArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool comSrfVelocityArrowActive;
        public DebugArrow comSrfVelocityArrow;
        
        [Persistent(pass = (int)Pass.Global)]
        public bool podObtVelocityArrowActive;
        public DebugArrow podObtVelocityArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool comObtVelocityArrowActive;
        public DebugArrow comObtVelocityArrow;

        
        [Persistent(pass = (int)Pass.Global)]
        public bool forwardArrowActive;
        public DebugArrow forwardArrow;

        // Not used since I did not write the code for that one yet.
        //[Persistent(pass = (int)Pass.Global)]
        public bool avgForwardArrowActive;
        public DebugArrow avgForwardArrow;

        [Persistent(pass = (int)Pass.Global)]
        public bool requestedAttitudeArrowActive;
        public DebugArrow requestedAttitudeArrow;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble arrowsLength = new EditableDouble(4);

        public MechJebModuleDebugArrows(MechJebCore core) : base(core)
        {
            enabled = true;
        }

        // TODO : I should probably use an array and an enum to lower code dup ...
        public override void OnUpdate()
        {
            if (podSrfVelocityArrow == null)
            {
                podSrfVelocityArrow = new DebugArrow(Color.yellow);
                comSrfVelocityArrow = new DebugArrow(Color.green);

                podObtVelocityArrow = new DebugArrow(Color.red);
                comObtVelocityArrow = new DebugArrow(XKCDColors.Orange);

                forwardArrow = new DebugArrow(XKCDColors.NavyBlue);
                avgForwardArrow = new DebugArrow(Color.blue);

                requestedAttitudeArrow = new DebugArrow(Color.gray);
            }

            podSrfVelocityArrow.State(podSrfVelocityArrowActive);
            if (podSrfVelocityArrowActive)
            {
                podSrfVelocityArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vessel.srf_velocity);
                podSrfVelocityArrow.SetLength((float)arrowsLength.val);
            }

            comSrfVelocityArrow.State(comSrfVelocityArrowActive && MechJebModuleAttitudeController.useCoMVelocity);
            if (comSrfVelocityArrowActive)
            {
                comSrfVelocityArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vesselState.surfaceVelocity);
                comSrfVelocityArrow.SetLength((float)arrowsLength.val);
            }


            podObtVelocityArrow.State(podObtVelocityArrowActive);
            if (podObtVelocityArrowActive)
            {
                podObtVelocityArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vessel.obt_velocity);
                podObtVelocityArrow.SetLength((float)arrowsLength.val);
            }

            comObtVelocityArrow.State(comObtVelocityArrowActive && MechJebModuleAttitudeController.useCoMVelocity);
            if (comObtVelocityArrowActive)
            {
                comObtVelocityArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vesselState.orbitalVelocity);
                comObtVelocityArrow.SetLength((float)arrowsLength.val);
            }

            forwardArrow.State(forwardArrowActive);
            if (forwardArrowActive)
            {
                forwardArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vessel.GetTransform().up);
                forwardArrow.SetLength((float)arrowsLength.val);
            }

            avgForwardArrow.State(avgForwardArrowActive);
            if (avgForwardArrowActive)
            {
                avgForwardArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, vesselState.forward);
                avgForwardArrow.SetLength((float)arrowsLength.val);
            }
            

            requestedAttitudeArrow.State(requestedAttitudeArrowActive && core.attitude.enabled);
            if (requestedAttitudeArrowActive && core.attitude.enabled)
            {
                requestedAttitudeArrow.Set(displayAtCoM ? vesselState.CoM : (Vector3d)vessel.GetReferenceTransformPart().transform.position, core.attitude.RequestedAttitude);
                requestedAttitudeArrow.SetLength((float)arrowsLength.val);
            }
        }
    }

    class DebugArrow
    {
        private GameObject gameObject;
        private GameObject haft;
        private GameObject cone;

        private static Shader diffuseAmbient;

        private const float coneLength = 0.5f;
        private float length;

        static  DebugArrow()
        {
            diffuseAmbient = new Material(Encoding.ASCII.GetString(Properties.Resources.shader2)).shader;
        }

        // TODO : change the shader to allow see thru (ignore the z buffer)
        public DebugArrow(Color color)
        {
            gameObject = new GameObject("DebugArrow");

            haft = CreateCone(1f, 0.05f, 0.05f, 0f, 20);
            haft.transform.parent = gameObject.transform;
            haft.transform.localRotation = Quaternion.Euler(90, 0, 0);

            cone = CreateCone(coneLength, 0.15f, 0f, 0f, 20);
            cone.transform.parent = gameObject.transform;
            cone.transform.localRotation = Quaternion.Euler(90, 0, 0);

            SetLength(4);

            MeshRenderer haftMeshRenderer = haft.AddComponent<MeshRenderer>();
            MeshRenderer coneMeshRenderer = cone.AddComponent<MeshRenderer>();

            haftMeshRenderer.material.shader = diffuseAmbient;
            haftMeshRenderer.material.color = color;
            haftMeshRenderer.castShadows = false;
            haftMeshRenderer.receiveShadows = false;

            coneMeshRenderer.material.shader = diffuseAmbient;
            coneMeshRenderer.material.color =  color;
            coneMeshRenderer.castShadows = false;
            coneMeshRenderer.receiveShadows = false;
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
            mesh.Optimize();


            return cone;
        }


    }
}