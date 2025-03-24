using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LandScapeDesignTool
{
    public class WeatherHandler : MonoBehaviour
    {
        [SerializeField] Text timeText;
        [SerializeField] GameObject rainBuiltInRpPrefab;
        [SerializeField] GameObject snowBuiltInRpPrefab;
        [SerializeField] private GameObject rainUrpPrefab;
        [SerializeField] private GameObject snowUrpPrefab;

        GameObject _sunLight = null;
        int _weather = 0;
        float _time = 12.0f;

        Color _sunColor = new Color(1, 0.9568f, 0.8392f);
        Color _sunColor1 = new Color(0.95294f, 0.71373f, 0.3647f);
        float _morningTime = 5.0f, _nightTime = 19.0f;
        GameObject _rain;
        GameObject _snow;

        // Start is called before the first frame update

        void Start()
        {
            _sunLight = RenderSettings.sun.gameObject;

            // Built-In Render Pipeline と Universal Render Pipeline で場合分けしてプレハブを生成します。
            // FIXME: HDRP には未対応です。
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            _rain = pipelineAsset == null ? Instantiate(rainBuiltInRpPrefab) : Instantiate(rainUrpPrefab);
            _snow = pipelineAsset == null ? Instantiate(snowBuiltInRpPrefab) : Instantiate(snowUrpPrefab);
            var mainCam = Camera.main;
            if (mainCam == null) return;
            var mainCamTrans = mainCam.transform;
            _rain.transform.parent = mainCamTrans;
            _snow.transform.parent = mainCamTrans;
            _snow.SetActive(false);
            _snow.transform.localPosition = new Vector3(0, 10, 0);
            _rain.SetActive(false);
            _rain.transform.localPosition = new Vector3(0, 10, 0);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnWeatherChange(int n)
        {
            _weather = n;
            switch(_weather)
            {
                case 3:
                    _rain.SetActive(true);
                    _snow.SetActive(false);
                    break;
                case 4:
                    _rain.SetActive(false);
                    _snow.SetActive(true);
                    break;
                default:
                    _rain.SetActive(false);
                    _snow.SetActive(false);
                    break;

            }
            ChangeLighting();
        }

        public void OnTimeChange(float value)
        {
            
            int h = (int)value;
            float m = value - h;
            int m2 = (int)(60 * m );
            string timestring = h.ToString("D2")+ ":"+ m2.ToString("D2");
            timeText.text = timestring;
            _time = value;
            ChangeLighting();
        }

        void ChangeLighting()
        {

            if (_sunLight == null)
            {
                _sunLight = RenderSettings.sun.gameObject;
            }

            float f = (_time - _morningTime) / (_nightTime - _morningTime);

            float r = 1, g = 1, b = 1;
            Color col = new Color();
            if (_time > (_morningTime + 2) && _time < (_nightTime - 2))
            {
                col = _sunColor;
            }
            else if (_time >= (_nightTime - 2))
            {
                float f1 = 1.0f - (_nightTime - _time) / 2.0f;
                r = _sunColor.r + ((_sunColor1.r - _sunColor.r) * f1);
                g = _sunColor.g + ((_sunColor1.g - _sunColor.g) * f1);
                b = _sunColor.b + ((_sunColor1.b - _sunColor.b) * f1);
                if (r < 0) r = 0;
                if (r > 1) r = 1;
                if (g < 0) g = 0;
                if (g > 1) g = 1;
                if (b < 0) b = 0;
                if (b > 1) b = 1;
                col.r = r;
                col.g = g;
                col.b = b;

                if (_time >= (_nightTime - 1))
                {
                    r = col.r * (((_nightTime - _time) / 1.0f));
                    g = col.g * (((_nightTime - _time) / 1.0f));
                    b = col.b * (((_nightTime - _time) / 1.0f));
                    if (r < 0) r = 0;
                    if (r > 1) r = 1;
                    if (g < 0) g = 0;
                    if (g > 1) g = 1;
                    if (b < 0) b = 0;
                    if (b > 1) b = 1;
                    col.r = r;
                    col.g = g;
                    col.b = b;
                }

            }
            else if (_time <= (_morningTime + 2))
            {
                float f1 = (_time - _morningTime) / 2.0f;
                r = _sunColor1.r + ((_sunColor.r - _sunColor1.r) * f1);
                g = _sunColor1.g + ((_sunColor.g - _sunColor1.g) * f1);
                b = _sunColor1.b + ((_sunColor.b - _sunColor1.b) * f1);
                if (r < 0) r = 0;
                if (r > 1) r = 1;
                if (g < 0) g = 0;
                if (g > 1) g = 1;
                if (b < 0) b = 0;
                if (b > 1) b = 1;
                col.r = r;
                col.g = g;
                col.b = b;

                if (_time <= (_morningTime + 1))
                {
                    r = col.r * ((_time - _morningTime) / 1.0f);
                    if (r < 0) r = 0;
                    if (r > 1) r = 0;
                    g = col.g * ((_time - _morningTime) / 1.0f);
                    if (g < 0) g = 0;
                    if (g > 1) g = 0;
                    b = col.b * ((_time - _morningTime) / 1.0f);
                    if (b < 0) b = 0;
                    if (g > 1) g = 0;
                    col.r = r;
                    col.g = g;
                    col.b = b;
                }

            }

            float strength = 1.0f;
            if (_weather == 1)
            {
                r = col.r * 0.8f;
                g = col.g * 0.8f;
                b = col.b * 0.8f;
                strength = 0.7f;
            }
            else if (_weather == 2)
            {
                r = col.r * 0.6f;
                g = col.g * 0.6f;
                b = col.b * 0.6f;
                strength = 0.4f;
            }
            else if (_weather == 3 || _weather == 4)
            {
                r = col.r * 0.3f;
                g = col.g * 0.3f;
                b = col.b * 0.3f;
                strength = 0.1f;
            }
            else
            {
                r = col.r;
                g = col.g;
                b = col.b;
                strength = 1.0f;
            }


            if (r < 0) r = 0;
            if (r > 1) r = 1;
            if (g < 0) g = 0;
            if (g > 1) g = 1;
            if (b < 0) b = 0;
            if (b > 1) b = 1;
            col.r = r;
            col.g = g;
            col.b = b;

            float angle = 180.0f - 180.0f * f;
            Quaternion r1 = Quaternion.Euler(0, -30, 0);
            Quaternion r2 = Quaternion.Euler(angle, 0, 0);
            _sunLight.transform.rotation = r2 * r1;
            _sunLight.GetComponent<Light>().color = col;
            _sunLight.GetComponent<Light>().shadowStrength = strength;



        }
    }
}
