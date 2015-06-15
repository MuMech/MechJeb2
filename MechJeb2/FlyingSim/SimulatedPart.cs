using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class SimulatedPart
    {

        protected DragCubeList cubes;

        public double totalMass = 0;
        public bool shieldedFromAirstream;
        public bool noDrag;
        public bool hasLiftModule;
        private float bodyLiftMultiplier;

        private float areaDrag;
        private Vector3 liftForce;

        //private float DragCubeMultiplier;
        //private float DragMultiplier;

        //private PhysicsGlobals.LiftingSurfaceCurve liftCurves;
        //private FloatCurve liftCurve;
        //private FloatCurve liftMachCurve;

        private ReentrySimulation.SimCurves simCurves;

        // Remove after test
        //public Part oPart;


        private Quaternion vesselToPart;

        static public SimulatedPart New(Part p, ReentrySimulation.SimCurves simCurve)
        {
            SimulatedPart part = new SimulatedPart();
            part.Set(p, simCurve);
            return part;
        }

        protected virtual void Set(Part p, ReentrySimulation.SimCurves _simCurves)
        {
            Rigidbody rigidbody = p.rb;

            totalMass = rigidbody == null ? 0 : rigidbody.mass; // TODO : check if we need to use this or the one without the childMass
            shieldedFromAirstream = p.ShieldedFromAirstream;

            noDrag = rigidbody == null && !PhysicsGlobals.ApplyDragToNonPhysicsParts;
            hasLiftModule = p.hasLiftModule;
            bodyLiftMultiplier = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier;

            simCurves = _simCurves;

            cubes = new DragCubeList();
            CopyDragCubesList(p.DragCubes, cubes);

            // Rotation to convert the vessel space vesselVelocity to the part space vesselVelocity
            vesselToPart = Quaternion.LookRotation(p.vessel.GetTransform().InverseTransformDirection(p.transform.forward), p.vessel.GetTransform().InverseTransformDirection(p.transform.up)).Inverse();

            //DragCubeMultiplier = PhysicsGlobals.DragCubeMultiplier;
            //DragMultiplier = PhysicsGlobals.DragMultiplier;


            //if (p.dragModel != Part.DragModel.CUBE)
            //    MechJebCore.print(p.name + " " + p.dragModel);

            //oPart = p;

        }

        public virtual Vector3 Drag(Vector3 vesselVelocity, float dynamicPressurekPa,  float mach)
        {
            if (shieldedFromAirstream || noDrag)
                return Vector3.zero;

            Vector3 dragVectorDirLocal = -(vesselToPart * vesselVelocity).normalized;

            // Use our thread safe version of SetDrag
            SetDrag(-dragVectorDirLocal, mach);

#warning do some of this math once per frame
            Vector3 drag = -vesselVelocity.normalized * areaDrag * dynamicPressurekPa * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;


            //bool delta = false;
            //string msg = oPart.name;
            //if (vesselVelocity.sqrMagnitude > 1 && dynamicPressurekPa - oPart.dynamicPressurekPa > oPart.dynamicPressurekPa * 0.1)
            //{
            //    msg += " dynamicPressurekPa " + dynamicPressurekPa.ToString("f4") + " vs " + oPart.dynamicPressurekPa.ToString("f4");
            //    delta = true;
            //}
            //
            ////if (vesselVelocity.sqrMagnitude > 1 && cubes.AreaDrag - oPart.DragCubes.AreaDrag > oPart.DragCubes.AreaDrag * 0.1)
            //if (vesselVelocity.sqrMagnitude > 1)
            //{
            //    msg += "\n AreaDrag " + cubes.AreaDrag.ToString("f4") + " vs " + oPart.DragCubes.AreaDrag.ToString("f4");
            //    //msg += "\n mach "     + mach.ToString("f4")           + " vs " + oPart.machNumber.ToString("f4");
            //
            //    msg += "\n dragDir " + MuUtils.PrettyPrint(dragDir)             + " vs " + MuUtils.PrettyPrint(oPart.dragVectorDirLocal)    + " " + Vector3.Angle(dragDir, oPart.dragVectorDirLocal).ToString("F3") + "°";
            //    //msg += "\n dragVel " + MuUtils.PrettyPrint(vesselVelocity.normalized) + " vs " + MuUtils.PrettyPrint(oPart.dragVector.normalized) + " " + Vector3.Angle(vesselVelocity.normalized, oPart.dragVector).ToString("F3") + "°";
            //
            //    msg += "\n Real° " + MuUtils.PrettyPrint(oPart.dragVectorDirLocal) + " " + Vector3.Angle(oPart.dragVectorDirLocal, Vector3.down).ToString("F3") + "°";
            //    msg += "\n Sim°  " + MuUtils.PrettyPrint(dragDir)                  + " " + Vector3.Angle(dragDir, Vector3.down).ToString("F3") + "°";
            //
            //    msg += "\n toUp " + MuUtils.PrettyPrint(vesselToPart * Vector3.up) + Vector3.Angle(vesselToPart * Vector3.up, Vector3.up).ToString("F3") + "°";
            //
            //
            //    Vector3 quatUp = vesselToPart * Vector3.up;
            //    Vector3 shipUp = oPart.vessel.transform.InverseTransformDirection(oPart.transform.up);
            //
            //    msg += "\n Ups " + MuUtils.PrettyPrint(quatUp) + " vs " + MuUtils.PrettyPrint(shipUp) + " " + Vector3.Angle(quatUp, shipUp).ToString("F3") + "°";
            //
            //
            //
            //    //msg += "\n AreaOccluded ";
            //    //for (int i = 0; i < 6; i++)
            //    //{
            //    //    msg += cubes.AreaOccluded[i].ToString("F3") + "/" + oPart.DragCubes.AreaOccluded[i].ToString("F3") + " ";
            //    //}
            //    //msg += "\n WeightedDrag ";
            //    //for (int i = 0; i < 6; i++)
            //    //{
            //    //    msg += cubes.WeightedDrag[i].ToString("F3") + "/" + oPart.DragCubes.WeightedDrag[i].ToString("F3") + " ";
            //    //}
            //
            //    msg += "\n vesselToPart " + MuUtils.PrettyPrint(vesselToPart.eulerAngles);
            //    delta = true;
            //}
            //
            //if (delta)
            //    MechJebCore.print(msg);

            return drag;
        }

        public virtual Vector3 Lift(Vector3 vesselVelocity, float dynamicPressurekPa, float mach)
        {
            if (shieldedFromAirstream || hasLiftModule)
                return Vector3.zero;

#warning obviously move out of here and evaluate once per mach value


            float bodyLiftScalar = bodyLiftMultiplier * dynamicPressurekPa * simCurves.LiftMachCurve.Evaluate(mach);

            // direction of the lift in a vessel centric reference
            Vector3 liftV = vesselToPart.Inverse() * liftForce * bodyLiftScalar;

            Vector3 liftVector = Vector3.ProjectOnPlane(liftV, -vesselVelocity);


            // cubes.LiftForce OK


            //if (vesselVelocity.sqrMagnitude > 1 && oPart.DragCubes.LiftForce.sqrMagnitude > 0.001)
            //{
            //    string msg = oPart.name;
            //
            //    Vector3 bodyL = oPart.transform.rotation * (oPart.bodyLiftScalar * oPart.DragCubes.LiftForce);
            //    Vector3 bodyLift = Vector3.ProjectOnPlane(bodyL, -oPart.dragVectorDir);
            //
            //    msg += "\n liftDir " + MuUtils.PrettyPrint(liftVector) + " vs " + MuUtils.PrettyPrint(bodyLift) + " " + Vector3.Angle(liftVector, bodyLift).ToString("F3") + "°";
            //
            //    Vector3 localBodyL = oPart.vessel.transform.InverseTransformDirection(bodyL);
            //    msg += "\n liftV " + MuUtils.PrettyPrint(liftV) + " vs " + MuUtils.PrettyPrint(localBodyL) + " " + Vector3.Angle(liftV, localBodyL).ToString("F3") + "°";
            //
            //    msg += "\n liftForce " + MuUtils.PrettyPrint(cubes.LiftForce) + " vs " + MuUtils.PrettyPrint(oPart.DragCubes.LiftForce) + " " + Vector3.Angle(cubes.LiftForce, oPart.DragCubes.LiftForce).ToString("F3") + "°";
            //    msg += "\n Normals " + MuUtils.PrettyPrint(-vesselVelocity) + " vs " + MuUtils.PrettyPrint(-oPart.dragVectorDir) + " " + Vector3.Angle(-vesselVelocity, -oPart.dragVectorDir).ToString("F3") + "°";
            //
            //    //msg += "\n vals " + bodyLiftMultiplier.ToString("F5") + " " + dynamicPressurekPa.ToString("F5") + " " + liftCurves.liftMachCurve.Evaluate(mach).ToString("F5");
            //
            //    MechJebCore.print(msg);
            //}

            return liftVector;
        }

        public virtual bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            return false;
        }

        public virtual bool Simulate(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            return false;
        }


        //TODO : rewrite the cube calls to only store and update the minimum needed data ( AreaOccluded + WeightedDrag ?)

        protected static void CopyDragCubesList(DragCubeList source, DragCubeList dest)
        {
            dest.ClearCubes();

            dest.None = source.None;

            // Procedural need access to part so things gets bad quick.
            dest.Procedural = false;

            for (int i = 0; i < source.Cubes.Count; i++)
            {
                DragCube c = new DragCube();
                CopyDragCube(source.Cubes[i], c);
                dest.Cubes.Add(c);
            }

            dest.SetDragWeights();

            for (int i=0; i<6; i++)
            {
                dest.WeightedArea[i] = source.WeightedArea[i];
                dest.WeightedDrag[i] = source.WeightedDrag[i];
                dest.AreaOccluded[i] = source.AreaOccluded[i];
                dest.WeightedDepth[i] = source.WeightedDepth[i];
            }

            dest.SetDragWeights();

            // We are missing PostOcclusionArea but it seems to be used in Thermal only
        }

        protected static void CopyDragCube(DragCube source, DragCube dest)
        {
            dest.Name = source.Name;
            dest.Weight = source.Weight;
            dest.Center = source.Center;
            dest.Size = source.Size;
            for (int i = 0; i < source.Drag.Length; i++)
            {
                dest.Drag[i] = source.Drag[i];
                dest.Area[i] = source.Area[i];
                dest.Depth[i] = source.Depth[i];
                dest.DragModifiers[i] = source.DragModifiers[i];
            }
        }



        // Unfortunately the DragCubeList SetDrag method is not thread safe
        // so here is a thread safe version
        protected void SetDrag(Vector3 dragVector, float machNumber)
        {
            areaDrag = 0f;
            liftForce = Vector3.zero;
            if (cubes.None)
            {
                return;
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 faceDirection = DragCubeList.GetFaceDirection((DragCube.DragFace)i);
                float dragDot = Vector3.Dot(dragVector, faceDirection);
                float dragValue = DragCurveValue((dragDot + 1f) * 0.5f, machNumber);
                float faceAreaDrag = cubes.AreaOccluded[i] * dragValue;
                areaDrag = areaDrag + faceAreaDrag * cubes.WeightedDrag[i];
                if (dragDot > 0f)
                {
                    float lift = simCurves.LiftCurve.Evaluate(dragDot);
                    if (!double.IsNaN(lift))
                    {
                        liftForce = liftForce - faceDirection * (dragDot * cubes.AreaOccluded[i] * cubes.WeightedDrag[i] * lift);
                    }
                }
            }
        }

        protected float DragCurveValue(float dotNormalized, float mach)
        {
            float surfaceDrag = simCurves.DragCurveSurface.Evaluate(mach);
            float multiplier = simCurves.DragCurveMultiplier.Evaluate(mach);
            if (dotNormalized <= 0.5f)
            {
                float tailDrag = simCurves.DragCurveTail.Evaluate(mach);
                return Mathf.Lerp(tailDrag, surfaceDrag, dotNormalized * 2f) * multiplier;
            }
            float tipDrag = simCurves.DragCurveTip.Evaluate(mach);
            return Mathf.Lerp(surfaceDrag, tipDrag, (dotNormalized - 0.5f) * 2f) * multiplier;
        }


        // Need to check and then simplify this, some operations are just redundant.
        protected void SetCubeWeight(string name, float newWeight)
        {
            float[] WeightedDragOrig = new float[6];

            int count = cubes.Cubes.Count;
            if (count == 0)
            {
                return;
            }

            bool noChange = true;
            for (int i = count - 1; i >= 0; i--)
            {
                if (cubes.Cubes[i].Name == name && cubes.Cubes[i].Weight != newWeight)
                {
                    cubes.Cubes[i].Weight = newWeight;
                    noChange = false;
                }
            }

            if (noChange)
                return;

            ResetCubeArray(cubes.WeightedArea);
            ResetCubeArray(cubes.WeightedDrag);
            ResetCubeArray(WeightedDragOrig);

            float weight = 0f;
            for (int i = count - 1; i >= 0; i--)
            {
                DragCube dc = cubes.Cubes[i];
                if (dc.Weight != 0f)
                {
                    weight = weight + dc.Weight;
                    AddCubeArray(cubes.WeightedArea, dc.Area, dc.Weight);
                    AddCubeArray(cubes.WeightedDrag, dc.Drag, dc.Weight);
                    AddCubeArray(WeightedDragOrig, dc.Drag, dc.Weight);
                }
            }
            if (weight != 0f)
            {
                float invWeight = 1f / weight;
                MultiplyCubeArray(cubes.WeightedArea, invWeight);
                MultiplyCubeArray(cubes.WeightedDrag, invWeight);
                MultiplyCubeArray(WeightedDragOrig, invWeight);
            }
            else
            {
                ResetCubeArray(cubes.WeightedArea);
                ResetCubeArray(cubes.WeightedDrag);
                ResetCubeArray(WeightedDragOrig);
            }

            SetCubeArray(cubes.AreaOccluded, cubes.WeightedArea);
            SetCubeArray(cubes.WeightedDrag, WeightedDragOrig);


        }

        private static void ResetCubeArray(float[] arr)
        {
            for (int i = 0; i < 6; i++)
            {
                arr[i] = 0f;
            }
        }

        private static void AddCubeArray(float[] outputArray, float[] inputArray, float multiply = 1f)
        {
            for (int i = 0; i < 6; i++)
            {
                outputArray[i] = outputArray[i] + inputArray[i] * multiply;
            }
        }

        private static void MultiplyCubeArray(float[] arr, float multiply)
        {
            for (int i = 0; i < 6; i++)
            {
                arr[i] = arr[i] * multiply;
            }
        }

        private static void SetCubeArray(float[] outputArray, float[] inputArray, float multiply = 1f)
        {
            for (int i = 0; i < 6; i++)
            {
                outputArray[i] = inputArray[i] * multiply;
            }
        }


    }
}
