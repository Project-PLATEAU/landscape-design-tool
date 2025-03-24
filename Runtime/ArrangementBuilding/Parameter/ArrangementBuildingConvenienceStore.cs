using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;
using System;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingConvenienceStore : IArrangementBuildingParameter<ConvenienceStoreConfig.Params>
    {
        private ConvenienceStoreConfig.Params parameters;
        public ConvenienceStoreConfig.Params Parameters => parameters;
        
        public void SetParameter(ConvenienceStoreConfig.Params parameters)
        {
            this.parameters = parameters;
        }
        
        public void SetIsSideWall(bool isActive)
        {
            parameters.isSideWall = isActive;
        }
        
        public void SetRoofThickness(float roofThickness)
        {
            parameters.roofThickness = roofThickness;
        }
    }
}