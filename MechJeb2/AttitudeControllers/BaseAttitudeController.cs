namespace MuMech.AttitudeControllers
{
    public abstract class BaseAttitudeController
    {
        protected MechJebModuleAttitudeController ac;

        protected BaseAttitudeController(MechJebModuleAttitudeController controller)
        {
            ac = controller;
        }

        public virtual void OnModuleDisabled()
        {
        }

        public virtual void OnModuleEnabled()
        {
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            if (global != null && global.HasNode(GetType().Name))
                ConfigNode.LoadObjectFromConfig(this, global.GetNode(GetType().Name), (int)Pass.GLOBAL);
            if (type != null && type.HasNode(GetType().Name)) ConfigNode.LoadObjectFromConfig(this, type.GetNode(GetType().Name), (int)Pass.TYPE);
            if (local != null && local.HasNode(GetType().Name)) ConfigNode.LoadObjectFromConfig(this, local.GetNode(GetType().Name), (int)Pass.LOCAL);
        }

        public virtual void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            if (global != null) ConfigNode.CreateConfigFromObject(this, (int)Pass.GLOBAL, null).CopyTo(global.AddNode(GetType().Name));
            if (type != null) ConfigNode.CreateConfigFromObject(this, (int)Pass.TYPE, null).CopyTo(type.AddNode(GetType().Name));
            if (local != null) ConfigNode.CreateConfigFromObject(this, (int)Pass.LOCAL, null).CopyTo(local.AddNode(GetType().Name));
        }

        public virtual void ResetConfig()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void Reset()
        {
        }

        public abstract void DrivePre(FlightCtrlState s, out Vector3d act, out Vector3d deltaEuler);

        public virtual void GUI()
        {
        }

        public virtual void Reset(int i)
        {
            Reset();
        }
    }
}
