using UnityEngine;
using System.Threading.Tasks;
using Landscape2.Runtime;
using PLATEAU.CityInfo;
using PLATEAU.GranularityConvert;
using PLATEAU.PolygonMesh;

namespace Landscape2.Editor
{
    /// <summary>
    /// �����ݒ�@�\
    /// UI��<see cref="InitialSettingsWindow"/>���S��
    /// </summary>
    public class InitialSettings
    {
        private GameObject[] cityModelObjs;

        // SubComponents�����݂���C�܂菉���ݒ肪���s����Ă��邩���m�F
        public bool CheckSubComponents()
        {
            var landscapeSubComponents = GameObject.FindObjectOfType<LandscapeSubComponents>();
            if (landscapeSubComponents != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       �@// �s�s���f�����C���|�[�g����Ă��邩���m�F
        public bool CheckInstancedCityModel()
        {
            var plateauInstancedCityModel = GameObject.FindObjectOfType<PLATEAUInstancedCityModel>();
            if (plateauInstancedCityModel != null)
            {
                return true;
            }
            else
            { 
                return false;   
            }
        }
        // �s�s���f����Scene�ɑ��݂��邩���m�F
        public bool CheckCityObjectGroup()
        {
            var plateauCityObjectGroups = GameObject.FindObjectsOfType<PLATEAUCityObjectGroup>();
            cityModelObjs = new GameObject[plateauCityObjectGroups.Length];
            int id = 0;

            if (plateauCityObjectGroups.Length > 0)
            {
                // PLATEAUCityObjectGroup������GameObject���擾
                foreach (var cityModel in plateauCityObjectGroups)
                {
                    cityModelObjs[id] = cityModel.gameObject;
                    id++;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        // SubComponents�𐶐�����
        public void CreateSubComponents()
        {
            // �����ݒ肪�s���Ă���ꍇ��SubComponents�𐶐����Ȃ�
            if (CheckSubComponents() == false)
            {
                var subComponentsObj = new GameObject("SubComponents");
                subComponentsObj.AddComponent<LandscapeSubComponents>();
            }
        }
        // �����ݒ�����s
        public async Task ExecuteInitialSettings()
        {
            // �}�e���A�������̉������Ƃ��āA�s�s�I�u�W�F�N�g���ŏ��n���P�ʂɕ���
            var granularityConverter = new CityGranularityConverter();
            var granularityConvertConf = new GranularityConvertOptionUnity(
                new GranularityConvertOption(MeshGranularity.PerAtomicFeatureObject, 1),
                cityModelObjs, true
            );
            var result = await granularityConverter.ConvertAsync(granularityConvertConf);
            if (!result.IsSucceed)
            {
                Debug.LogError("�Q�[���I�u�W�F�N�g�̕����Ɏ��s���܂����B");
                return;
            }
        }
    }
}