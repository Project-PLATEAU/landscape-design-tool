using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandScapeDesignTool
{
    public class RGBColorSelectUI : MonoBehaviour
    {
        [SerializeField] private RGBColorSelectUIElement redUI;
        [SerializeField] private RGBColorSelectUIElement greenUI;
        [SerializeField] private RGBColorSelectUIElement blueUI;
        
        private Color color = Color.white;

        public Color Color
        {
            get => this.color;
            set
            {
                this.color = value;
                redUI.SetValue(value.r);
                greenUI.SetValue(value.g);
                blueUI.SetValue(value.b);
            }
        }

        private void Update()
        {
            if (Math.Abs(Color.r - redUI.GetValue()) > 0.001f)
            {
                var newColor = Color;
                newColor.r = redUI.GetValue();
                Color = newColor;
            }

            if (Math.Abs(Color.g - greenUI.GetValue()) > 0.001f)
            {
                var newColor = Color;
                newColor.g = greenUI.GetValue();
                Color = newColor;
            }

            if (Math.Abs(Color.b - blueUI.GetValue()) > 0.001f)
            {
                var newColor = Color;
                newColor.b = blueUI.GetValue();
                Color = newColor;
            }
        }
    }
}
