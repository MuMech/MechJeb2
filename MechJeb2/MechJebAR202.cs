using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebAR202 : PartModule
    {
        MechJebCore core = null;

        //light stuff
        enum LightColor { NEITHER, GREEN, RED };
        LightColor litLight = 0;
        Shader originalLensShader;
        Shader lightShader;
        Color originalLensColor = new Color(0, 0, 0, 0);
        Light greenLight;
        Light redLight;
        Transform greenLightTransform;
        Transform redLightTransform;

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

        void InitializeLights()
        {
            greenLightTransform = null;
            redLightTransform = null;

            lightShader = new Material(Encoding.ASCII.GetString(Properties.Resources.shader)).shader;

            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name.Equals("light_green")) greenLightTransform = t;
                if (t.name.Equals("light_red")) redLightTransform = t;
            }

            if ((greenLightTransform != null) && (greenLightTransform.light == null))
            {
                originalLensShader = greenLightTransform.renderer.material.shader;
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
                greenLight = greenLightTransform.light;
            }
            if ((redLightTransform != null) && (redLightTransform.light == null))
            {
                originalLensShader = redLightTransform.renderer.material.shader;
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
                redLight = redLightTransform.light;
            }
        }

        void HandleLights()
        {
            if (greenLight == null || redLight == null) InitializeLights();

            if (core == null || MapView.MapIsEnabled)
            {
                litLight = LightColor.NEITHER;
            }
            else
            {
                bool somethingEnabled = false;
                if (vessel.GetMasterMechJeb() == core)
                {
                    foreach (DisplayModule display in core.GetComputerModules<DisplayModule>())
                    {
                        if (display is MechJebModuleMenu) continue;
                        if (display.enabled && display.showInCurrentScene)
                        {
                            somethingEnabled = true;
                        }
                    }
                }

                litLight = (somethingEnabled ? LightColor.GREEN : LightColor.RED);
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

        void TurnOnLight(LightColor which)
        {
            switch (which)
            {
                case LightColor.GREEN:
                    if (greenLightTransform != null)
                    {
                        greenLightTransform.renderer.material.shader = lightShader;                        
                        greenLightTransform.renderer.material.color = Color.green;
                        greenLight.enabled = true;
                    }
                    break;

                case LightColor.RED:
                    if (redLightTransform != null)
                    {
                        redLightTransform.renderer.material.shader = lightShader;                        
                        redLightTransform.renderer.material.color = Color.red;
                        redLight.enabled = true;
                    }
                    break;
            }
        }

        void TurnOffLight(LightColor which)
        {
            switch (which)
            {
                case LightColor.GREEN:
                    if (greenLightTransform != null)
                    {
                        greenLightTransform.renderer.material.shader = originalLensShader;
                        greenLightTransform.renderer.material.color = originalLensColor;
                        greenLight.enabled = false;
                    }
                    break;

                case LightColor.RED:
                    if (redLightTransform != null)
                    {
                        redLightTransform.renderer.material.shader = originalLensShader;
                        redLightTransform.renderer.material.color = originalLensColor;
                        redLight.enabled = false;
                    }
                    break;
            }
        }
    }
}
