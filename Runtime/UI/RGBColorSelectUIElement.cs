using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LandScapeDesignTool
{
    public class RGBColorSelectUIElement : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Text text;

        public void SetValue(float value)
        {
            slider.value = value;
            text.text = Mathf.CeilToInt(value * 255).ToString();
        }

        public float GetValue()
        {
            return slider.value;
        }

        private void Reset()
        {
            slider = GetComponentInChildren<Slider>();
            text = GetComponentInChildren<Text>();
        }
    }
}
