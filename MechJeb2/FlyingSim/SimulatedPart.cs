using System.Collections.Generic;
using Smooth.Pools;
using UnityEngine;

namespace MuMech
{
    public class SimulatedPart
    {
        protected DragCubeList cubes = new DragCubeList();

        public double totalMass = 0;
        public bool shieldedFromAirstream;
        public bool noDrag;
        public bool hasLiftModule;
        private double bodyLiftMultiplier;

        private ReentrySimulation.SimCurves simCurves;

        private QuaternionD vesselToPart;
        private QuaternionD partToVessel;

        private static readonly Pool<SimulatedPart> pool = new Pool<SimulatedPart>(Create, Reset);

        public static int PoolSize
        {
            get { return pool.Size; }
        }

        private static SimulatedPart Create()
        {
            SimulatedPart part = new SimulatedPart();
            part.cubes.BodyLiftCurve = new PhysicsGlobals.LiftingSurfaceCurve();
            part.cubes.SurfaceCurves = new PhysicsGlobals.SurfaceCurvesList();
            return part;
        }

        public virtual void Release()
        {
            foreach (DragCube cube in cubes.Cubes)
            {
                DragCubePool.Instance.Release(cube);
            }
            pool.Release(this);
        }

        public static void Release(List<SimulatedPart> objList)
        {
            for (int i = 0; i < objList.Count; ++i)
            {
                objList[i].Release();
            }
        }

        private static void Reset(SimulatedPart obj)
        {
        }

        public static SimulatedPart Borrow(Part p, ReentrySimulation.SimCurves simCurve)
        {
            SimulatedPart part = pool.Borrow();
            part.Init(p, simCurve);
            return part;
        }

        protected void Init(Part p, ReentrySimulation.SimCurves _simCurves)
        {
            Rigidbody rigidbody = p.rb;

            totalMass = rigidbody == null ? 0 : rigidbody.mass; // TODO : check if we need to use this or the one without the childMass
            shieldedFromAirstream = p.ShieldedFromAirstream;

            noDrag = rigidbody == null && !PhysicsGlobals.ApplyDragToNonPhysicsParts;
            hasLiftModule = p.hasLiftModule;
            bodyLiftMultiplier = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier;

            simCurves = _simCurves;

            //cubes = new DragCubeList();
            CopyDragCubesList(p.DragCubes, cubes);
            cubes.ForceUpdate(true, true);

            // Rotation to convert the vessel space vesselVelocity to the part space vesselVelocity
            // QuaternionD.LookRotation is not working...
            partToVessel = Quaternion.LookRotation(p.vessel.GetTransform().InverseTransformDirection(p.transform.forward), p.vessel.GetTransform().InverseTransformDirection(p.transform.up));
            vesselToPart = Quaternion.Inverse(partToVessel);

            //DragCubeMultiplier = PhysicsGlobals.DragCubeMultiplier;
            //DragMultiplier = PhysicsGlobals.DragMultiplier;


            //if (p.dragModel != Part.DragModel.CUBE)
            //    MechJebCore.print(p.name + " " + p.dragModel);

            //oPart = p;

        }

        public virtual Vector3d Drag(Vector3d vesselVelocity, double dragFactor, float mach)
        {
            if (shieldedFromAirstream || noDrag)
                return Vector3d.zero;

            Vector3d dragVectorDirLocal = -(vesselToPart * vesselVelocity).normalized;

            cubes.SetDrag(dragVectorDirLocal, mach);

            Vector3d drag = -vesselVelocity.normalized * cubes.AreaDrag * dragFactor;

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

        public virtual Vector3d Lift(Vector3d vesselVelocity, double liftFactor)
        {
            if (shieldedFromAirstream || hasLiftModule)
                return Vector3d.zero;

            // direction of the lift in a vessel centric reference
            Vector3d liftV = partToVessel * ((Vector3d)cubes.LiftForce * bodyLiftMultiplier * liftFactor);

            Vector3d liftVector = liftV.ProjectOnPlane(vesselVelocity);

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

        public virtual bool SimulateAndRollback(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time, double semiDeployMultiplier)
        {
            return false;
        }

        public virtual bool Simulate(double altATGL, double altASL, double endASL, double pressure, double shockTemp, double time, double semiDeployMultiplier)
        {
            return false;
        }

        public static class DragCubePool
        {
            private static readonly Pool<DragCube> _Instance = new Pool<DragCube>(
                () => new DragCube(), cube => { });


            public static Pool<DragCube> Instance { get { return _Instance; } }
        }

        protected void CopyDragCubesList(DragCubeList source, DragCubeList dest)
        {
            dest.ClearCubes();

            dest.SetPart(source.Part);

            dest.None = source.None;

            // Procedural need access to part so things gets bad quick.
            dest.Procedural = false;

            for (int i = 0; i < source.Cubes.Count; i++)
            {
                DragCube c = DragCubePool.Instance.Borrow();
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

            dest.DragCurveCd = simCurves.DragCurveCd;
            dest.DragCurveCdPower = simCurves.DragCurveCdPower;
            dest.DragCurveMultiplier = simCurves.DragCurveMultiplier;

            dest.BodyLiftCurve.liftCurve = simCurves.LiftCurve;
            dest.BodyLiftCurve.dragCurve = simCurves.DragCurve;
            dest.BodyLiftCurve.dragMachCurve = simCurves.DragMachCurve;
            dest.BodyLiftCurve.liftMachCurve = simCurves.LiftMachCurve;

            dest.SurfaceCurves.dragCurveMultiplier = simCurves.DragCurveMultiplier;
            dest.SurfaceCurves.dragCurveSurface = simCurves.DragCurveSurface;
            dest.SurfaceCurves.dragCurveTail = simCurves.DragCurveTail;
            dest.SurfaceCurves.dragCurveTip = simCurves.DragCurveTip;
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

        protected void SetCubeWeight(string name, float newWeight)
        {
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

            cubes.SetDragWeights();
        }
    }
}
