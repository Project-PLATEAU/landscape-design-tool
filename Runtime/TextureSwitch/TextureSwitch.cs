using PLATEAU.CityInfo;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    /// <summary>
    /// 建物のテクスチャを切り替える機能
    /// </summary>
    public class TextureSwitch : ISubComponent
    {
        // テクスチャを保存するリスト
        private List<List<Texture2D>> textureList = new List<List<Texture2D>>();
        private PLATEAUCityObjectGroup[] cityObjcects;
        private Toggle switchToggle;
        private bool isTextureNull = false;

        public TextureSwitch(VisualElement uiRoot)
        {
            switchToggle = uiRoot.Q<Toggle>("Toggle_Material");
            switchToggle.RegisterValueChangedCallback((evt) =>
            {
                isTextureNull = evt.newValue;
                SetTexture();
            });

            cityObjcects = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();
            foreach (var building in cityObjcects)
            {
                var materials = building.GetComponent<MeshRenderer>().materials;
                // 各テクスチャのコピーを取得
                List<Texture2D> textures = new List<Texture2D>();
                foreach (var material in materials)
                {
                    textures.Add(material.mainTexture as Texture2D);
                }
                textureList.Add(textures);
            }
        }

        //  建物のテクスチャを切り替える
        private void SetTexture()
        {
            int count = cityObjcects.Length;
            for (int index = 0; index < count; index++)
            {
                var cityObject = cityObjcects[index];

                if (!cityObject.gameObject.name.StartsWith("bldg_")) continue;

                // MeshRendererの取得とマテリアル参照のキャッシュ
                var meshRenderer = cityObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                var materials = meshRenderer.materials;

                // マテリアルごとの処理
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].mainTexture = isTextureNull ? null : textureList[index][i];
                    // LOD1のShader設定
                    float sideTitling = isTextureNull ? 0f : 0.4f;
                    materials[i].SetFloat("_Side_Titling", sideTitling);
                }
            }
        }

        public void Start()
        {
        }
        public void Update(float deltaTime)
        {
        }
        public void OnEnable()
        {
        }
        public void OnDisable()
        {
        }

        public void LateUpdate(float deltaTime)
        {
        }

    }
}
