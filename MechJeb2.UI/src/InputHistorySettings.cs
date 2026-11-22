using KodeKit;
using KodeKit.Headless;

namespace MuMech.UI
{
    /// <summary>
    /// KodeKit's input history (<see cref="KodeWindow.InputHistory"/>) in MechJeb's global settings
    /// (<see cref="MechJebModuleSettings.InputHistory"/>): one FIELD node per key, the key recorded into most recently first, and its
    /// values, the most recent first.
    /// <code>
    /// InputHistory { FIELD { key = planner.apoapsis  value = 250  value = 100 } }
    /// </code>
    /// The history's caps bound the node (64 keys of 8 values of 40 characters at most).
    /// </summary>
    internal static class InputHistorySettings
    {
        const string Field = "FIELD", Key = "key", Value = "value";

        // the settings' load the history was last read from
        static int read;

        /// <summary>Reads the history from the settings when they were loaded since (once a frame: an int compare otherwise).</summary>
        public static void Follow()
        {
            if (MechJebModuleSettings.InputHistoryLoads == read) return;
            read = MechJebModuleSettings.InputHistoryLoads;
            Import(MechJebModuleSettings.InputHistory, KodeWindow.InputHistory);
        }

        /// <summary>The history changed: it goes to the settings, saved with the next save of <paramref name="settings"/>.</summary>
        public static void Save(MechJebModuleSettings settings)
        {
            MechJebModuleSettings.SetInputHistory(Export(KodeWindow.InputHistory));
            if (settings != null) settings.Dirty = true;
        }

        public static ConfigNode Export(InputHistory history)
        {
            var node = new ConfigNode(MechJebModuleSettings.InputHistoryNodeName);
            for (int i = 0; i < history.Count; i++)
            {
                string key = history.KeyAt(i);
                if (!Safe(key)) continue;
                ConfigNode field = node.AddNode(Field);
                field.AddValue(Key, key);
                foreach (string v in history.Values(key))
                    if (Safe(v)) field.AddValue(Value, v);
            }
            return node;
        }

        public static void Import(ConfigNode node, InputHistory history)
        {
            history.Clear(false);
            if (node == null) return;
            foreach (ConfigNode field in node.GetNodes(Field))
            {
                string key = field.GetValue(Key);
                if (!string.IsNullOrEmpty(key)) history.Load(key, field.GetValues(Value));
            }
        }

        // what a config file keeps as written: no braces (nodes), no "//" (a comment), no line break, no '='
        static bool Safe(string s) => !string.IsNullOrEmpty(s) && s.IndexOfAny(unsafeChars) < 0 && !s.Contains("//");

        static readonly char[] unsafeChars = { '{', '}', '\n', '\r', '=' };
    }
}
