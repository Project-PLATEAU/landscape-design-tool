using UnityEngine;

namespace LandscapeDesignTool.Editor.WindowTabs
{
    /// <summary>
    /// Shapefile読込のタブを描画します。
    /// </summary>
    public class TabShapefileLoad : IGuiTabContents
    {
        private readonly ShapeFileEditorHelper _shapeFileEditorHelper = new ShapeFileEditorHelper();

        public void OnGUI()
        {
            _shapeFileEditorHelper.DrawGui();
        }

        public void OnSceneGUI()
        {
            
        }

        public void Update()
        {
            
        }
    }
}