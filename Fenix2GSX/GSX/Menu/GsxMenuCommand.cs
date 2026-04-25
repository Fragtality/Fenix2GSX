namespace Fenix2GSX.GSX.Menu
{
    public enum GsxMenuCommandType
    {
        Open = 0,
        State = 1,
        Select = 2,
        Wait = 3,
        Operator = 4,
    }

    public class GsxMenuCommand(int parameter, GsxMenuCommandType type = GsxMenuCommandType.Select, string title = "", bool waitRdy = true, bool menuReset = false)
    {
        public int Parameter { get; } = parameter;
        public string Title { get; } = title;
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
        public bool WaitReady { get; } = waitRdy;
        public bool MenuReset { get; } = menuReset;
        public GsxMenuCommandType Type { get; } = type;

        public static GsxMenuCommand Open(bool menuReset = true)
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Open, "", true, menuReset);
        }

        public static GsxMenuCommand State(int state)
        {
            return new GsxMenuCommand(state, GsxMenuCommandType.State);
        }

        public static GsxMenuCommand Select(int line, string title = "", bool waitRdy = true, bool menuReset = false)
        {
            return new GsxMenuCommand(line, GsxMenuCommandType.Select, title, waitRdy, menuReset);
        }

        public static GsxMenuCommand Wait()
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Wait);
        }

        public static GsxMenuCommand Operator(bool menuReset = true)
        {
            return new GsxMenuCommand(0, GsxMenuCommandType.Operator, "", true, menuReset);
        }
    }
}
