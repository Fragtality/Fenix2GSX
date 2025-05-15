namespace Fenix2GSX.GSX.Menu
{
    public enum GsxMenuCommandType
    {
        Number = 0,
        DummyWait = 1,
        Operator = 2,
        Reset = 3,
    }

    public class GsxMenuCommand(int number, string title = "", bool open = false, GsxMenuCommandType type = GsxMenuCommandType.Number)
    {
        public int Number { get; } = number;
        public string Title { get; } = title;
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
        public bool OpenMenu { get; set; } = open;
        public bool NoHide { get; set; } = false;
        public bool WaitReady { get; set; } = open;
        public GsxMenuCommandType Type { get; } = type;

        public static GsxMenuCommand CreateOperator()
        {
            return new GsxMenuCommand(0, "", false, GsxMenuCommandType.Operator);
        }

        public static GsxMenuCommand CreateDummy(bool open = false)
        {
            return new GsxMenuCommand(0, "", open, GsxMenuCommandType.DummyWait);
        }

        public static GsxMenuCommand CreateReset()
        {
            return new GsxMenuCommand(0, "", false, GsxMenuCommandType.Reset);
        }
    }
}
