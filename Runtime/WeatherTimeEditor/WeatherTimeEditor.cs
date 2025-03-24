using UnityEngine;
using PlateauToolkit.Rendering;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Landscape2.Runtime.WeatherTimeEditor
{
    /// <summary>
    /// 天気・時間帯を変更
    /// UIは<see cref="WeatherTimeEditorUI"/>が担当
    /// </summary>
    public class WeatherTimeEditor
    {
        public enum Weather
        {
            Sun,
            Cloud,
            Rain,
            Snow
        }

        private EnvironmentController environmentController;
        private Weather currentWeather; // 現在の天候
        private GameObject environmentObj; // Environmentプレハブを格納するGameObject
        private GameObject volumeObj; // Environment Volumeを格納するGameObject
        private Volume volume;
        private VisualEnvironment visualEnvironment;
        private SkyEnvironment skyEnvironment;
        
        public WeatherTimeEditor()
        {
            environmentController = GameObject.FindObjectOfType<EnvironmentController>();
            // EnvironmentControllerを取得
            if (environmentController == null)
            {
                Debug.LogError("Failed to load Environment Prefab.");
            }
            else 
            { 
                environmentObj = environmentController.gameObject;
            }

            // EnvironmentプレハブのEnvironment VolumeにあるVolumeコンポーネントを取得
            volumeObj = environmentObj.transform.Find("Environment Volume").gameObject;
            if (volumeObj == null)
            {
                Debug.LogError("Failed to load Environment Volume.");
            }
            volume = volumeObj.GetComponent<Volume>();
            
            // VolumeコンポーネントにあるVisualEnvironmentを取得
            volume.profile.TryGet(out visualEnvironment);
            if(visualEnvironment == null)
            {
                Debug.LogError("Failed to load VisualEnvironment.");
            }
            
            currentWeather = Weather.Sun;
            
            skyEnvironment = new SkyEnvironment(environmentObj);
        }
        
        /// <summary>
        /// 天候を変更
        /// </summary>
        public void SwitchWeather(int weatherID)
        {
            currentWeather = (Weather)weatherID;
            switch (currentWeather)
            {
                case Weather.Sun:
                    environmentController.m_Rain = 0.0f;
                    environmentController.m_Snow = 0.0f;
                    environmentController.m_Cloud = 0.33f;
                    break;
                case Weather.Rain:
                    environmentController.m_Rain = 1.0f;
                    environmentController.m_Snow = 0.0f;
                    environmentController.m_Cloud = 1.0f;
                    break;
                case Weather.Cloud:
                    environmentController.m_Rain = 0.0f;
                    environmentController.m_Snow = 0.0f;
                    environmentController.m_Cloud = 1.0f;
                    break;
                case Weather.Snow:
                    environmentController.m_Rain = 0.0f;
                    environmentController.m_Snow = 1.0f;
                    environmentController.m_Cloud = 1.0f;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 時間帯を変更
        /// </summary>
        public void EditTime(float timeValue)
        {
            environmentController.m_TimeOfDay = timeValue;

            // 晴れのときとそうでないときでSkyTypeを変更
            if (currentWeather == Weather.Sun)
            {
                // SkyTypeをPhysically Based Skyに変更
                visualEnvironment.skyType.value = (int)SkyType.PhysicallyBased;
            }
            else
            {
                // SkyTypeをHDRI Skyに変更
                visualEnvironment.skyType.value = (int)SkyType.HDRI;
            }
        }
        /// <summary>
        /// timeValue値から時刻を計算
        /// </summary>
        private DateTime CalculateTime(float timeValue)
        {
            int year = (int)environmentController.m_Date.x;
            int month = (int)environmentController.m_Date.y;
            int day = (int)environmentController.m_Date.z;
            double totalHours = timeValue * 24;
            int hour = (int)totalHours;
            int minute = (int)((totalHours - hour) * 60);
            int second = (int)((((totalHours - hour) * 60) - minute) * 60);
            DateTime combinedDateTime;

            if (timeValue >= 0.9999)
            {
                hour = hour % 24;
            }

            try
            {
                combinedDateTime = new DateTime(year, month, day, hour, minute, second, environmentController.m_TimeZone);
            }
            catch
            {
                combinedDateTime = DateTime.Now;
            }

            return combinedDateTime;
        }
        /// <summary>
        /// timeValue値から(HH:mm)のフォーマットで時刻を取得
        /// </summary>
        public string GetTimeString(float timeValue)
        {      
            DateTime time = CalculateTime(timeValue);
            return time.ToString("HH:mm");
        }

        public void OnUpdate()
        {
            skyEnvironment.OnUpdate(currentWeather);
        }
    }
}
