namespace LandscapeDesignTool.Editor.WindowTabs
{
    public interface IGuiTabContents
    {
        void OnGUI();
        void OnSceneGUI();
        void Update();
    }
}