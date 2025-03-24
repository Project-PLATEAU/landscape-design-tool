using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LandScapeDesignTool
{
    public class MenuHandler : MonoBehaviour
    {
        [SerializeField] GameObject menuPanel;
        [SerializeField] GameObject viewpointPanel;
        [SerializeField] GameObject weatherPanel;
        [SerializeField] GameObject ChangeColorPanel;
        [SerializeField] GameObject ChangeHeightPanel;
        [SerializeField] GameObject HeightRegulationAreatPanel;
        [SerializeField] GameObject ViewRegulationAreaPanel;
        [SerializeField] GameObject RegulationAreaPanel;

        // Start is called before the first frame update
        void Start()
        {
            menuPanel.SetActive(false);
            viewpointPanel.SetActive(false);
            weatherPanel.SetActive(false);
            ChangeColorPanel.SetActive(false);
            ChangeHeightPanel.SetActive(false);
            HeightRegulationAreatPanel.SetActive(false);
            ViewRegulationAreaPanel.SetActive(false);
            RegulationAreaPanel.SetActive(false);

            // Playモード開始時、カメラをViewPointの1つに移動します。
            var firstViewPoint = FindObjectOfType<LandscapeViewPoint>();
            if (firstViewPoint != null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    var camTrans = mainCam.transform;
                    var vpTrans = firstViewPoint.transform;
                    camTrans.position = vpTrans.position;
                    camTrans.rotation = vpTrans.rotation;
                    mainCam.fieldOfView = firstViewPoint.GetComponent<LandscapeViewPoint>().Fov;
                }
                
            }
            
            // InputModuleが存在しなければ、新たに作ります。
            if (FindObjectOfType<StandaloneInputModule>() == null)
            {
                var obj = new GameObject("StandaloneInputModule");
                obj.AddComponent<StandaloneInputModule>();
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void ToggleMenu()
        {
           menuPanel.SetActive( menuPanel.activeSelf ? false : true);
           if( menuPanel.activeSelf == true)
            {
                viewpointPanel.SetActive(false);
            }


        }

        public void ToggleViewPointPanel()
        {
            viewpointPanel.SetActive(viewpointPanel.activeSelf ? false : true); 
            if (viewpointPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }

        public void ToggleWeatherPanel()
        {
            weatherPanel.SetActive(weatherPanel.activeSelf ? false : true);
            if (weatherPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }
        public void ToggleColorPanel()
        {
            ChangeColorPanel.SetActive(ChangeColorPanel.activeSelf ? false : true);
            if (ChangeColorPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }

        public void ToggleHeightrPanel()
        {
            ChangeHeightPanel.SetActive(ChangeHeightPanel.activeSelf ? false : true);
            if (ChangeHeightPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }
        public void ToggleHeightRegulationPanel()
        {
            HeightRegulationAreatPanel.SetActive(HeightRegulationAreatPanel.activeSelf ? false : true);
            if (HeightRegulationAreatPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }
        public void ToggleViewRegulationPanel()
        {
            ViewRegulationAreaPanel.SetActive(ViewRegulationAreaPanel.activeSelf ? false : true);
            if (ViewRegulationAreaPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }
        public void ToggleRegulationPanel()
        {
            RegulationAreaPanel.SetActive(RegulationAreaPanel.activeSelf ? false : true);
            if (RegulationAreaPanel.activeSelf == false)
            {
                menuPanel.SetActive(true);
            }
        }
    }
}
