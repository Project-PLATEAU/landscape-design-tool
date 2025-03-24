using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LandScapeDesignTool
{
    public class RegulationAreaHandlerUI : MonoBehaviour
    {
        GameObject[] objects;
        // Start is called before the first frame update
        void Start()
        {

            objects = GameObject.FindGameObjectsWithTag("RegulationArea");

        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void ShowRegulaitonArea()
        {

            foreach (var obj in objects)
            {
                obj.SetActive(true);
            }

        }
        public void HideRegulationarea()
        {
            foreach( var obj in objects)
            {
                obj.SetActive(false);
            }
        }
    }
}
