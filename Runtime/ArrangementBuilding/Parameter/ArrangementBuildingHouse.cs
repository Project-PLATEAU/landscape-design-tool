using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;
using System;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingHouse : IArrangementBuildingParameter<HouseConfig.Params>
    {
        private HouseConfig.Params parameters;
        public HouseConfig.Params Parameters => parameters;
        
        public void SetParameter(HouseConfig.Params parameters)
        {
            this.parameters = parameters;
        }
        
        public void SetNumFloor(int numFloor)
        {
            parameters.numFloor = numFloor;
        }
        
        public void SetHasEntranceRoof(bool hasEntranceRoof)
        {
            parameters.hasEntranceRoof = hasEntranceRoof;
        }
        
        public void SetRoofType(string roofTypeString)
        {
            switch (roofTypeString)
            {
                case "平屋根":
                    parameters.roofType = RoofType.flat;
                    break;
                case "寄棟屋根":
                    parameters.roofType = RoofType.hipped;
                    break;
                default:
                    break;
            }
        }
        
        public string GetRoofTypeString(RoofType roofType)
        {
            switch (roofType)
            {
                case RoofType.flat:
                    return "平屋根";
                case RoofType.hipped:
                    return "寄棟屋根";
                default:
                    return "";
            }
        }
        
        public void SetRoofThickness(float roofThickness)
        {
            parameters.roofThickness = roofThickness;
        }
    }
}