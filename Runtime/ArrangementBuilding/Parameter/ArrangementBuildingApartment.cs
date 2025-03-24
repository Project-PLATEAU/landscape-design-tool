using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;

namespace Landscape2.Runtime
{
    public class ArrangementBuildingApartment : IArrangementBuildingParameter<ApartmentConfig.Params>
    {
        private ApartmentConfig.Params parameters;
        public ApartmentConfig.Params Parameters => parameters;
        
        public void SetParameter(ApartmentConfig.Params parameters)
        {
            // アパート用のパラメータを設定
            this.parameters = parameters;
        }

        public void SetConvexBalcony(bool isActive)
        {
            parameters.convexBalcony = isActive;
        }
        
        public void SetHasBalconyGlass(bool isActive)
        {
            parameters.hasBalconyGlass = isActive;
        }
        
        public void SetHasBalconyLeft(bool isActive)
        {
            parameters.hasBalconyLeft = isActive;
        }
        
        public void SetHasBalconyRight(bool isActive)
        {
            parameters.hasBalconyRight = isActive;
        }

        public void SetHasBalconyFront(bool isActive)
        {
            parameters.hasBalconyFront = isActive;
        }
        
        public void SetHasBalconyBack(bool isActive)
        {
            parameters.hasBalconyBack = isActive;
        }
    }
}