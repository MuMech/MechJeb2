namespace MuMech
{
    public enum AscentStatusLevel { NORMAL, WARNING, ERROR }

    /// <summary>The status line an ascent autopilot shows in the ascent window and kOS.</summary>
    public sealed class AscentStatus
    {
        public string Text { get; private set; } = "";

        public AscentStatusLevel Severity { get; private set; }

        public void Set(string text, AscentStatusLevel severity = AscentStatusLevel.NORMAL)
        {
            Text = text;
            Severity = severity;
        }
    }

    /// <summary>What the rest of MechJeb needs from whichever ascent autopilot is selected (see MechJebCore.Ascent).</summary>
    public interface IAscentAutopilot
    {
        bool Enabled { get; set; }

        UserPool Users { get; }

        AscentStatus Status { get; }
    }
}
