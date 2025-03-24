using PlateauToolkit.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Landscape2.Runtime.WeatherTimeEditor
{
    public class SkyEnvironment
    {
        private GameObject moonObj;
        private GameObject moonLightObj;
        
        private EnvironmentController environmentController;
        
        // 全体の空のカラー
        private ColorAdjustments skyColorAdjustments;
        private readonly Color skyDefaultColorFilter;
        
        // 曇り用の色調整
        private readonly Color skyCloudColor = new Color32(191,191,191,255);

        // fog
        private float defaultFogDistance;
        private float cloudFogDistance = 250;
        private readonly Color defaultFogColor;
        
        public SkyEnvironment(GameObject environmentObj)
        {
            var renderingObj = environmentObj.transform.Find("Rendering Volume").gameObject;
            if (renderingObj == null)
            {
                Debug.LogError("Failed to load Rendering Volume. 初期設定を実行してください。");
            }
            
            var moon = environmentObj.transform.Find("Moon");
            if (moon == null)
            {
                Debug.LogError("Failed to load Moon. 初期設定を実行してください。");
            }
            moonObj = moon.gameObject;
            
            var moonLight = environmentObj.transform.Find("Moon Light").gameObject;
            if (moonLight == null)
            {
                Debug.LogError("Failed to load Moon Light. 初期設定を実行してください。");
            }
            moonLightObj = moonLight.gameObject;
            
            environmentController = environmentObj.GetComponent<EnvironmentController>();
            defaultFogColor = environmentController.m_FogColor;
            defaultFogDistance = environmentController.m_FogDistance;
            
            // 空の色変更用のColorAdjustmentsを取得
            var renderingVolume = renderingObj.GetComponent<Volume>();
            renderingVolume.profile.TryGet(out skyColorAdjustments);
            skyDefaultColorFilter = skyColorAdjustments.colorFilter.value;
        }

        public void OnUpdate(WeatherTimeEditor.Weather currentWeather)
        {
            if (moonLightObj == null || moonObj == null)
            {
                return;
            }
            
            // 月がONになるタイミングで月あかりを設定するためupdateにて監視
            bool isNight = moonObj.activeInHierarchy;
            UpdateSkySettings(isNight, currentWeather);
        }
        
        private void UpdateSkySettings(bool isNight, WeatherTimeEditor.Weather currentWeather)
        {
            void SetSkySetting(bool isCloud)
            {
                skyColorAdjustments.colorFilter.value = isCloud ? skyCloudColor :  skyDefaultColorFilter;
                environmentController.m_FogColor = isCloud ? skyCloudColor : defaultFogColor;
                environmentController.m_FogDistance = isCloud ? cloudFogDistance : defaultFogDistance;
            }
            
            if (currentWeather == WeatherTimeEditor.Weather.Sun)
            {
                SetSkySetting(false);
                moonLightObj.SetActive(isNight);
            }
            else
            {
                SetSkySetting(isNight);
                moonLightObj.SetActive(false);
            }
        }
    }
}