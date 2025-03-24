using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingOfficeBuilding : IArrangementBuildingParameter<OfficeBuildingConfig.Params>
    {
        private OfficeBuildingConfig.Params parameters;
        public OfficeBuildingConfig.Params Parameters => parameters;

        public void SetParameter(OfficeBuildingConfig.Params parameters)
        {
            this.parameters = parameters;
        }
        
        public void SetUseWindow(bool useWindow)
        {
            parameters.useWindow = useWindow;
        }
        
        public void SetSpandrelHeight(float spandrelHeight)
        {
            parameters.spandrelHeight = spandrelHeight;
        }
    }
}