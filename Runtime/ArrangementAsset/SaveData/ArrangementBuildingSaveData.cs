using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;
using System;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物の保存データ
    /// </summary>
    [Serializable]
    public class ArrangementBuildingSaveData : IArrangementSandboxAssetSaveData<PlateauSandboxBuilding>
    {
        public BuildingType BuildingType;
        
        public float Height;
        public float Width;
        public float Depth;
        
        // 個別パラメータ
        public ApartmentConfig.Params apartmentParams;
        public ConvenienceStoreConfig.Params convenienceStoreParams;
        public HouseConfig.Params houseParams;
        public OfficeBuildingConfig.Params officeBuildingParams;
        
        public void Save(PlateauSandboxBuilding target)
        {
            BuildingType = target.buildingType;
            
            Height = target.buildingHeight;
            Width = target.buildingWidth;
            Depth = target.buildingDepth;

            switch (BuildingType)
            {
                case BuildingType.k_Apartment:
                    apartmentParams = target.skyscraperCondominiumParams;
                    break;
                case BuildingType.k_OfficeBuilding:
                    officeBuildingParams = target.officeBuildingParams;
                    break;
                case BuildingType.k_House:
                    houseParams = target.residenceParams;
                    break;
                case BuildingType.k_ConvenienceStore:
                    convenienceStoreParams = target.conveniParams;
                    break;
                case BuildingType.k_CommercialBuilding:
                case BuildingType.k_Hotel:
                    break;
            }
        }

        public void Apply(PlateauSandboxBuilding target)
        {
            var editor = new ArrangementBuildingEditor();
            editor.SetTarget(target);
            
            editor.SetWidth(Width);
            editor.SetHeight(Height);
            editor.SetDepth(Depth);

            switch (BuildingType)
            {
                case BuildingType.k_Apartment:
                    target.skyscraperCondominiumParams = apartmentParams;
                    break;
                case BuildingType.k_OfficeBuilding:
                    target.officeBuildingParams = officeBuildingParams;
                    break;
                case BuildingType.k_House:
                    target.residenceParams = houseParams;
                    break;
                case BuildingType.k_ConvenienceStore:
                    target.conveniParams = convenienceStoreParams;
                    break;
                case BuildingType.k_CommercialBuilding:
                case BuildingType.k_Hotel:
                    break;
            }

            editor.ApplyBuildingMesh();
        }
    }
}