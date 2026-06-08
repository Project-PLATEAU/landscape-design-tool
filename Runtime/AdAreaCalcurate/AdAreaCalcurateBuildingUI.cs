using UnityEngine;

namespace Landscape2.Runtime
{
    public interface IAdAreaCalculateBuildingModel
    {
        public event System.Action<float> OnWallAreaValueChanged;
    }

    public class AdAreaCalcurateBuildingUI
    {

        IAdAreaCalcurateBuildingUIView view;
        IAdAreaCalculateBuildingModel model;

        public AdAreaCalcurateBuildingUI(IAdAreaCalcurateBuildingUIView ibuildingView, IAdAreaCalculateBuildingModel model)
        {

            view = ibuildingView;
            this.model = model;

            // 総壁面面積が更新されたのでviewに通知
            this.model.OnWallAreaValueChanged += (v) =>
            {
                view.WallArea = v;
            };

        }

        private float CalculateAdAreaPercentage(float wallArea, float adArea)
        {
            if (wallArea <= 0f)
            {
                return 0f;
            }
            return (adArea / wallArea) * 100f;
        }
    }
}
