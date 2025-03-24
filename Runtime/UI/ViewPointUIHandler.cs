using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LandScapeDesignTool
{
    public class ViewPointUIHandler : MonoBehaviour
    {
        [SerializeField] GameObject scrollContent;
        [SerializeField] Button buttonPrefab;

        GameObject[] viewpoints;
        // Start is called before the first frame update
        void Awake()
        {
            int n = 0;
            viewpoints = GameObject.FindGameObjectsWithTag("ViewPoint");
            foreach (var vp in viewpoints)
            {

                Button btn = Instantiate(buttonPrefab);
                btn.transform.SetParent(scrollContent.transform);

                LandscapeViewPoint viewpoint = vp.GetComponent<LandscapeViewPoint>();
                btn.GetComponentInChildren<Text>().text = viewpoint.Name;

                btn.onClick.AddListener(() => OnViewpointButton(viewpoint));
                n++;
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
        public void OnViewpointButton(LandscapeViewPoint vp)
        {
            LandscapeViewPoint  viewpoint = vp;
            Vector3 pos = viewpoint.transform.position;
            Quaternion rot = viewpoint.transform.localRotation;
            float fov = viewpoint.Fov;
            Camera camera = Camera.main;
            camera.transform.position = pos;
            camera.transform.localRotation = rot;
            camera.fieldOfView = fov;
        }

    }
}
