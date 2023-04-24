﻿using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionIf : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent, IMechJebModuleScriptActionContainer
    {
        public static    string                         NAME = "If";
        private readonly MechJebModuleScriptActionsList actionsThen;
        private readonly MechJebModuleScriptActionsList actionsElse;
        private readonly MechJebModuleScriptCondition   condition;
        private static   GUIStyle                       sBorderY;
        private static   GUIStyle                       sBorderG;
        private static   GUIStyle                       sBorderR;
        private readonly Texture2D                      backgroundY;
        private readonly Texture2D                      backgroundG;
        private readonly Texture2D                      backgroundR;

        public MechJebModuleScriptActionIf(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(
            scriptModule, core, actionsList, NAME)
        {
            actionsThen = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
            actionsElse = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
            condition   = new MechJebModuleScriptCondition(scriptModule, core, this);
            if (sBorderY == null)
            {
                sBorderY         = new GUIStyle();
                sBorderY.border  = new RectOffset(1, 1, 1, 1);
                sBorderG         = new GUIStyle();
                sBorderG.border  = new RectOffset(1, 1, 1, 1);
                sBorderR         = new GUIStyle();
                sBorderR.border  = new RectOffset(1, 1, 1, 1);
                sBorderY.padding = new RectOffset(1, 1, 1, 1);
                sBorderG.padding = new RectOffset(1, 1, 1, 1);
                sBorderR.padding = new RectOffset(1, 1, 1, 1);
            }

            if (backgroundY == null)
            {
                backgroundY = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                backgroundG = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                backgroundR = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int x = 0; x < backgroundY.width; x++)
                {
                    for (int y = 0; y < backgroundY.height; y++)
                    {
                        if (x == 0 || x == 15 || y == 0 || y == 15)
                        {
                            backgroundY.SetPixel(x, y, Color.yellow);
                            backgroundG.SetPixel(x, y, Color.green);
                            backgroundR.SetPixel(x, y, Color.red);
                        }
                        else
                        {
                            backgroundY.SetPixel(x, y, Color.clear);
                            backgroundG.SetPixel(x, y, Color.clear);
                            backgroundR.SetPixel(x, y, Color.clear);
                        }
                    }
                }

                backgroundY.Apply();
                backgroundG.Apply();
                backgroundR.Apply();
                sBorderY.normal.background   = backgroundY;
                sBorderY.onNormal.background = backgroundY;
                sBorderG.normal.background   = backgroundG;
                sBorderG.onNormal.background = backgroundG;
                sBorderR.normal.background   = backgroundR;
                sBorderR.onNormal.background = backgroundR;
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            if (condition.checkCondition())
            {
                actionsThen.start();
            }
            else
            {
                actionsElse.start();
            }
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical(sBorderY);
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("If", GuiUtils.yellowLabel, GUILayout.ExpandWidth(false));
            condition.WindowGUI(windowID);
            GUILayout.Label("Then", GuiUtils.yellowLabel, GUILayout.ExpandWidth(false));
            postWindowGUI(windowID);

            GUILayout.BeginHorizontal();
            GUILayout.Space(50);
            GUILayout.BeginVertical(sBorderG);
            actionsThen.actionsWindowGui(windowID);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(50);
            GUILayout.BeginVertical();
            GUILayout.Label("Else", GuiUtils.yellowLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(50);
            GUILayout.BeginVertical(sBorderR);
            actionsElse.actionsWindowGui(windowID);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public int getRecursiveCount()
        {
            return actionsThen.getRecursiveCount() + actionsElse.getRecursiveCount();
        }

        public List<MechJebModuleScriptAction> getRecursiveActionsList()
        {
            var actionsRes = new List<MechJebModuleScriptAction>();
            actionsRes.AddRange(actionsThen.getRecursiveActionsList());
            actionsRes.AddRange(actionsElse.getRecursiveActionsList());
            return actionsRes;
        }

        public void notifyEndActionsList()
        {
            endAction();
        }

        public List<MechJebModuleScriptActionsList> getActionsListsObjects()
        {
            var lists = new List<MechJebModuleScriptActionsList>();
            lists.Add(actionsThen);
            lists.Add(actionsElse);
            return lists;
        }

        public override void afterOnFixedUpdate()
        {
            if (condition.getConditionVerified())
            {
                actionsThen.OnFixedUpdate();
            }
            else
            {
                actionsElse.OnFixedUpdate();
            }
        }

        public override void postLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(condition, node.GetNode("Condition"));
            ConfigNode nodeListThen = node.GetNode("ActionsListThen");
            if (nodeListThen != null)
            {
                actionsThen.LoadConfig(nodeListThen);
            }

            ConfigNode nodeListElse = node.GetNode("ActionsListElse");
            if (nodeListElse != null)
            {
                actionsElse.LoadConfig(nodeListElse);
            }
        }

        public override void postSave(ConfigNode node)
        {
            var conditionNode = ConfigNode.CreateConfigFromObject(condition, (int)Pass.Type, null);
            conditionNode.CopyTo(node.AddNode("Condition"));
            var nodeListThen = new ConfigNode("ActionsListThen");
            actionsThen.SaveConfig(nodeListThen);
            node.AddNode(nodeListThen);
            var nodeListElse = new ConfigNode("ActionsListElse");
            actionsElse.SaveConfig(nodeListElse);
            node.AddNode(nodeListElse);
        }
    }
}
