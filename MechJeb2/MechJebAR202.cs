using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebAR202 : PartModule
    {
        private MechJebCore core;
        private Light greenLight;
        private Renderer greenLightRenderer;
        private Transform greenLightTransform;
        private LightColor litLight = 0;
        private Light redLight;
        private Renderer redLightRenderer;
        private Transform redLightTransform;
        private int emissionId;

        public override void OnStart(StartState state)
        {
            core = part.Modules.OfType<MechJebCore>().FirstOrDefault();

            if (state != StartState.None && state != StartState.Editor)
            {
                InitializeLights();
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsEditor) HandleLights();
        }

        private void InitializeLights()
        {
            greenLightTransform = null;
            redLightTransform = null;
            
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name.Equals("light_green")) greenLightTransform = t;
                if (t.name.Equals("light_red")) redLightTransform = t;
            }

            emissionId = Shader.PropertyToID("_EmissiveColor");

            if (greenLightTransform != null)
            {
                if (greenLightTransform.GetComponent<Light>() == null)
                {
                    greenLightRenderer = greenLightTransform.GetComponent<Renderer>();
                    greenLight = greenLightTransform.gameObject.AddComponent<Light>();
                    greenLight.transform.parent = greenLightTransform;
                    greenLight.type = LightType.Point;
                    greenLight.renderMode = LightRenderMode.ForcePixel;
                    greenLight.shadows = LightShadows.None;
                    greenLight.enabled = false;
                    greenLight.color = Color.green;
                    greenLight.range = 1.5F;
                }
                else
                {
                    greenLight = greenLightTransform.GetComponent<Light>();
                }
            }

            if (redLightTransform != null)
            {
                if (redLightTransform.GetComponent<Light>() == null)
                {
                    redLightRenderer = redLightTransform.GetComponent<Renderer>();
                    redLight = redLightTransform.gameObject.AddComponent<Light>();
                    redLight.transform.parent = redLightTransform;
                    redLight.type = LightType.Point;
                    redLight.renderMode = LightRenderMode.ForcePixel;
                    redLight.shadows = LightShadows.None;
                    redLight.enabled = false;
                    redLight.color = Color.red;
                    redLight.range = 1.5F;
                }
                else
                {
                    redLight = redLightTransform.GetComponent<Light>();
                }
            }
        }

        private void HandleLights()
        {
            if (greenLight == null || redLight == null) InitializeLights();
            if (greenLight == null || redLight == null) return;

            if (core == null || MapView.MapIsEnabled)
            {
                litLight = LightColor.NEITHER;
            }
            else
            {
                bool somethingEnabled = false;
                if (vessel.GetMasterMechJeb() == core)
                {
                    foreach (DisplayModule display in core.GetDisplayModules(MechJebModuleMenu.DisplayOrder.instance))
                    {
                        if (display is MechJebModuleMenu) continue;
                        if (display.enabled && display.showInCurrentScene)
                        {
                            somethingEnabled = true;
                        }
                    }
                }

                litLight = somethingEnabled ? LightColor.GREEN : LightColor.RED;
            }

            switch (litLight)
            {
                case LightColor.GREEN:
                    if (!greenLight.enabled) TurnOnLight(LightColor.GREEN);
                    if (redLight.enabled) TurnOffLight(LightColor.RED);
                    break;

                case LightColor.RED:
                    if (greenLight.enabled) TurnOffLight(LightColor.GREEN);
                    if (!redLight.enabled) TurnOnLight(LightColor.RED);
                    break;

                case LightColor.NEITHER:
                    if (greenLight.enabled) TurnOffLight(LightColor.GREEN);
                    if (redLight.enabled) TurnOffLight(LightColor.RED);
                    break;
            }
        }

        private void TurnOnLight(LightColor which)
        {
            switch (which)
            {
                case LightColor.GREEN:
                    if (greenLightTransform != null)
                    {
                        greenLightRenderer.material.SetColor(emissionId, Color.green);
                        greenLight.enabled = true;
                    }
                    break;

                case LightColor.RED:
                    if (redLightTransform != null)
                    {
                        redLightRenderer.material.SetColor(emissionId, Color.red);
                        redLight.enabled = true;
                    }
                    break;
            }
        }

        private void TurnOffLight(LightColor which)
        {
            switch (which)
            {
                case LightColor.GREEN:
                    if (greenLightTransform != null)
                    {
                        greenLightRenderer.material.SetColor(emissionId, Color.black);
                        greenLight.enabled = false;
                    }
                    break;

                case LightColor.RED:
                    if (redLightTransform != null)
                    {
                        redLightRenderer.material.SetColor(emissionId, Color.black);
                        redLight.enabled = false;
                    }
                    break;
            }
        }

        //light stuff
        private enum LightColor
        {
            NEITHER,
            GREEN,
            RED
        }
    }
}