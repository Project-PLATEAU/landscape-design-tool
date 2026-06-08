using Landscape2.Runtime.AdvertisingPlacementRestrictions;
using UnityEngine;

namespace Landscape2.Runtime.AdRegulation
{
    public class AdAreaSettingUI : ISubComponent
    {
        private IAdAreaSettingView view;
        private IAdvertisingPlacementRestrictionsModule model;
        private IAdAssetListUI assetList;

        ISubComponent viewSubComponent => view as ISubComponent;
        ISubComponent modelSubComponent => model as ISubComponent;


        public AdAreaSettingUI(IAdAreaSettingView view, IAdAssetListUI assetList, IAdvertisingPlacementRestrictionsModule model)
        {
            this.view = view;
            this.model = model;
            this.assetList = assetList;
        }

        void ISubComponent.LateUpdate(float deltaTime)
        {
            modelSubComponent?.LateUpdate(deltaTime);
            viewSubComponent?.LateUpdate(deltaTime);
        }

        void ISubComponent.OnDisable()
        {
            viewSubComponent?.OnDisable();
            modelSubComponent?.OnDisable();
        }

        void ISubComponent.OnEnable()
        {
            modelSubComponent?.OnEnable();
            viewSubComponent?.OnEnable();
            Initialize();
        }

        void ISubComponent.Start()
        {
            modelSubComponent?.Start();
            viewSubComponent?.Start();
        }

        void ISubComponent.Update(float deltaTime)
        {
            modelSubComponent?.Update(deltaTime);
            viewSubComponent?.Update(deltaTime);
        }

        private void Initialize()
        {
            assetList.OnPutAsset += (GameObject asset) =>
            {
                model.UpdateAdObjects(false);
            };

            //view.OnAdObjectSelected += (GameObject adObject) =>
            //{
            //    if (adObject == null)
            //    {
            //        // If no advertisement object is selected, clear the model
            //        model.SetAdObject(null);
            //        return;
            //    }
            //    // Handle the selection of an advertisement object
            //    Debug.Log($"Advertisement object selected: {adObject.name}");
            //    model.SetAdObject(adObject);
            //};

            view.OnEnableChanged += (v) =>
            {
                if (v == false)
                {
                    model.Reset();
                    model.EnableUpdate(false);
                }
                else
                {
                    model.EnableUpdate(true);
                    model.UpdateAdObjects(true);
                }
            };

            view.FormContainerView.OnDistanceChanged += (float distance) =>
            {
                // Handle the change in restriction distance
                Debug.Log($"Restriction distance changed: {distance}");
                model.SetRestrictionDistance(distance);
            };

            view.OnDisplayStatusChanged += (v) =>
            {
                model.DisplayArea(v);
            };

            model.OnChangedDistance += (v) =>
            {
                view.FormContainerView.SetWithoutNotify(v);
            };
        }

    }
}
