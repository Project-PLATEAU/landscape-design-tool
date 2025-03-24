using PLATEAU.Native;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime.WalkerMode
{
    /// <summary>
    /// 歩行者モード時の緯度、経度の表示プレゼンター
    /// </summary>
    public class WalkerModeCoordinateUI
    {
        private Label latitudeValue;
        private Label longitudeValue;

        private WalkerMode walkerMode;
        private VisualElement root;
        
        public WalkerModeCoordinateUI(VisualElement parent, WalkerMode walkerMode)
        {
            this.walkerMode = walkerMode;
            root = parent.Q<VisualElement>("container_coordinate");
            
            latitudeValue = root.Q<Label>("latitude");
            longitudeValue = root.Q<Label>("longitude");
        }

        public void Show(bool isShow)
        {
            root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Update(float deltaTime)
        {
            if (!walkerMode.IsWalkerMode())
            {
                return;
            }
            var coordinate = GetCoordinate();
            latitudeValue.text = coordinate.latitude;
            longitudeValue.text = coordinate.longitude;
        }
        
        private (string latitude, string longitude) GetCoordinate()
        {
            // 歩行者の位置から緯度経度取得
            var currentPosition = walkerMode.GetWalkerPosition();
            var plateauVector3 = new PlateauVector3d(currentPosition.x, currentPosition.y, currentPosition.z);
            var coordinate = CityModelHandler.CityModel.GeoReference.Unproject(plateauVector3);

            return (coordinate.Latitude.ToString(), coordinate.Longitude.ToString());
        }
    }
}