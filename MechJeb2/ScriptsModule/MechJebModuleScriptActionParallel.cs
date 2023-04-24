using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionParallel : MechJebModuleScriptAction, IMechJebModuleScriptActionsListParent,
        IMechJebModuleScriptActionContainer
    {
        public static    string                         NAME = "Parallel";
        private readonly MechJebModuleScriptActionsList actions1;
        private readonly MechJebModuleScriptActionsList actions2;
        private readonly GUIStyle                       sBorderY;
        private readonly GUIStyle                       sBorderB;
        private          int                            countEndList;
        private          bool                           panel1Hidden;
        private          bool                           panel2Hidden;

        public MechJebModuleScriptActionParallel(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            actions1        = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
            actions2        = new MechJebModuleScriptActionsList(core, scriptModule, this, actionsList.getDepth() + 1);
            sBorderB        = new GUIStyle();
            sBorderB.border = new RectOffset(1, 1, 1, 1);
            var backgroundB = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            sBorderY        = new GUIStyle();
            sBorderY.border = new RectOffset(1, 1, 1, 1);
            var backgroundY = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int x = 0; x < backgroundB.width; x++)
            {
                for (int y = 0; y < backgroundB.height; y++)
                {
                    if (x == 0 || x == 15 || y == 0 || y == 15)
                    {
                        backgroundB.SetPixel(x, y, Color.blue);
                        backgroundY.SetPixel(x, y, Color.yellow);
                    }
                    else
                    {
                        backgroundB.SetPixel(x, y, Color.clear);
                        backgroundY.SetPixel(x, y, Color.clear);
                    }
                }
            }

            backgroundB.Apply();
            backgroundY.Apply();

            sBorderB.normal.background   = backgroundB;
            sBorderB.onNormal.background = backgroundB;
            sBorderB.padding             = new RectOffset(1, 1, 1, 1);
            sBorderY.normal.background   = backgroundY;
            sBorderY.onNormal.background = backgroundY;
            sBorderY.padding             = new RectOffset(1, 1, 1, 1);
        }

        public override void activateAction()
        {
            base.activateAction();
            actions1.start();
            actions2.start();
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
            GUILayout.Label("Parallel Execution", GuiUtils.yellowLabel, GUILayout.ExpandWidth(false));
            if (panel1Hidden)
            {
                if (GUILayout.Button("<<Show Panel 1", GUILayout.ExpandWidth(false)))
                {
                    panel1Hidden = false;
                }
            }
            else if (!panel2Hidden)
            {
                if (GUILayout.Button(">>Hide Panel 1", GUILayout.ExpandWidth(false)))
                {
                    panel1Hidden = true;
                }
            }
            else
            {
                if (GUILayout.Button(">>Hide Panel 1 and <<Show Panel 2", GUILayout.ExpandWidth(false)))
                {
                    panel1Hidden = true;
                    panel2Hidden = false;
                }
            }

            if (panel2Hidden)
            {
                if (GUILayout.Button("<<Show Panel 2", GUILayout.ExpandWidth(false)))
                {
                    panel2Hidden = false;
                }
            }
            else if (!panel1Hidden)
            {
                if (GUILayout.Button(">>Hide Panel 2", GUILayout.ExpandWidth(false)))
                {
                    panel2Hidden = true;
                }
            }
            else
            {
                if (GUILayout.Button(">>Hide Panel 2 and <<Show Panel 1", GUILayout.ExpandWidth(false)))
                {
                    panel2Hidden = true;
                    panel1Hidden = false;
                }
            }

            postWindowGUI(windowID);
            GUILayout.BeginHorizontal();
            if (!panel1Hidden)
            {
                GUILayout.BeginVertical(sBorderB);
                actions1.actionsWindowGui(windowID);
                GUILayout.EndVertical();
            }

            if (!panel2Hidden)
            {
                GUILayout.BeginVertical(sBorderB);
                actions2.actionsWindowGui(windowID);
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public int getRecursiveCount()
        {
            return actions1.getRecursiveCount() + actions2.getRecursiveCount();
        }

        public List<MechJebModuleScriptAction> getRecursiveActionsList()
        {
            var actionsRes = new List<MechJebModuleScriptAction>();
            actionsRes.AddRange(actions1.getRecursiveActionsList());
            actionsRes.AddRange(actions2.getRecursiveActionsList());
            return actionsRes;
        }

        public void notifyEndActionsList()
        {
            countEndList++;
            if (countEndList == 2)
            {
                endAction();
            }
        }

        public List<MechJebModuleScriptActionsList> getActionsListsObjects()
        {
            var lists = new List<MechJebModuleScriptActionsList>();
            lists.Add(actions1);
            lists.Add(actions2);
            return lists;
        }

        public override void afterOnFixedUpdate()
        {
            if (actions1.isStarted() && !actions1.isExecuted())
            {
                actions1.OnFixedUpdate();
            }

            if (actions2.isStarted() && !actions2.isExecuted())
            {
                actions2.OnFixedUpdate();
            }
        }

        public override void postLoad(ConfigNode node)
        {
            ConfigNode nodeList1 = node.GetNode("ActionsList1");
            if (nodeList1 != null)
            {
                actions1.LoadConfig(nodeList1);
            }

            ConfigNode nodeList2 = node.GetNode("ActionsList2");
            if (nodeList2 != null)
            {
                actions2.LoadConfig(nodeList2);
            }
        }

        public override void postSave(ConfigNode node)
        {
            var nodeList1 = new ConfigNode("ActionsList1");
            actions1.SaveConfig(nodeList1);
            node.AddNode(nodeList1);
            var nodeList2 = new ConfigNode("ActionsList2");
            actions2.SaveConfig(nodeList2);
            node.AddNode(nodeList2);
        }
    }
}
