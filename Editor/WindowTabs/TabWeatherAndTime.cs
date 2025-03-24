

using UnityEditor;
using UnityEngine;

namespace LandscapeDesignTool.Editor.WindowTabs
{
    /// <summary>
    /// 「天候と時間」タブを描画します。
    /// </summary>
    public class TabWeatherAndTime : IGuiTabContents
    {
        float _time = 12;

        Color _sunColor = new Color(1,0.9568f, 0.8392f);
        Color _sunColor1 = new Color(0.95294f, 0.71373f, 0.3647f);
        float _sunAngle;
        [SerializeField] GameObject _sunLight=null;
        float _morningTime=5.0f, _nightTime=19.0f;
        float _sunRoll = -20.0f;

        int _weather = 0;
        
        public void OnGUI()
        {
            // _sunLight = GameObject.Find("SunSource").gameObject;
            _sunLight = RenderSettings.sun.gameObject;
            EditorGUILayout.Space();
            /*
            if(_sunLight == null)
            {
                EditorGUILayout.ObjectField("光源指定", _sunLight, typeof(GameObject), true);
            }
            */
            LandscapeEditorStyle.Header("光源");
            EditorGUILayout.ObjectField("光源", _sunLight, typeof(GameObject), true);
            LandscapeEditorStyle.Header("時間の指定");
            _time = EditorGUILayout.Slider("時間", _time, _morningTime, _nightTime);


            EditorGUILayout.Space();
            LandscapeEditorStyle.Header("天候の設定");
            GUIContent[] popupItem = new[]
            {
                new GUIContent("晴れ"),
                new GUIContent("薄曇り"),
                new GUIContent("曇り"),
                // new GUIContent("雨"),
                // new GUIContent("雪")
            };

            _weather = EditorGUILayout.Popup(
                label: new UnityEngine.GUIContent("天候"),
                selectedIndex: _weather,
                displayedOptions: popupItem);
        }

        public void OnInspectorUpdate()
        {
            if (_sunLight == null)
            {
                _sunLight = RenderSettings.sun.gameObject;
            }

            float f = (_time - _morningTime) / (_nightTime - _morningTime);

            float r=1, g=1, b=1;
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
                    r = col.r * (((_nightTime-_time) / 1.0f));
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
            else if( _time <= (_morningTime + 2))
            {
                float f1 = (_time - _morningTime) / 2.0f;
                r = _sunColor1.r + ((_sunColor.r - _sunColor1.r) * f1);
                g = _sunColor1.g +((_sunColor.g - _sunColor1.g) * f1);
                b = _sunColor1.b +((_sunColor.b - _sunColor1.b) * f1);
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
            if(_weather == 1)
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
            _sunLight.transform.rotation = r2*r1;
            _sunLight.GetComponent<Light>().color = col;
            _sunLight.GetComponent<Light>().shadowStrength = strength;



        }

        public void OnSceneGUI()
        {
            
        }

        public void Update()
        {
            
        }
    }
}