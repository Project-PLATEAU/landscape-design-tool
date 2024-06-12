using System.Collections.Generic;
using PLATEAU.Util.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landscape2.Editor
{
    /// <summary>
    /// �i�σc�[����InitialSettingsWindow�̃G���g���[�|�C���g�ł��B
    /// </summary>
    public class InitialSettingsWindow : EditorWindow
    {
        [SerializeField]private VisualTreeAsset m_VisualTreeAsset = default;
        [SerializeField]private Texture checkTexture;
        [SerializeField]private Texture errorTexture;

        private InitialSettings initialSettings = new InitialSettings();
        private const string UIRunButton = "RunButton"; // �����ݒ���s�{�^�����O
        private const string UIImportCheckColumn = "ImportCheckColumn"; // �s�s���f���C���|�[�g�ςݔ��藓���O
        private const string UICityObjectCheckColumn = "CityObjectCheckColumn"; // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔��藓���O
        private const string UIImportHelpboxColumn = "ImportHelpboxColumn"; // �s�s���f���C���|�[�g�ςݔ���Helpbox�����O
        private const string UICityObjectHelpboxColumn = "CityObjectHelpboxColumn"; // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���Helpbox�����O

        private List<bool> checkList = new List<bool>(); // �����ݒ���s�\���̔���p���X�g

        [MenuItem("PLATEAU/InitialSettings")]
        public static void Open()
        {
            var window = GetWindow<InitialSettingsWindow>("InitialSettings");
            window.Show();
        }

        public void CreateGUI()
        {
            VisualElement uiRoot = rootVisualElement;
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            uiRoot.Add(labelFromUXML);

            var runButton = uiRoot.Q<Button>(UIRunButton);
            var importCheckImage = new Image();
            var cityObjectGroupCheckImage = new Image();
            var importCheckColumn = uiRoot.Q<VisualElement>(UIImportCheckColumn);
            var cityObjectCheckColumn = uiRoot.Q<VisualElement>(UICityObjectCheckColumn);
            var importHelpboxColumn = uiRoot.Q<VisualElement>(UIImportHelpboxColumn);
            var cityObjectHelpboxColumn = uiRoot.Q<VisualElement>(UICityObjectHelpboxColumn);

            var initialSettingsHelpBox = new HelpBox("�����ݒ肪���ɍs���Ă��܂�", HelpBoxMessageType.Info);
            var importCheckHelpBox = new HelpBox("�s�s���f�����C���|�[�g����Ă��邩�m�F���Ă�������", HelpBoxMessageType.Error);
            var cityObjectCheckHelpBox = new HelpBox("�s�s�I�u�W�F�N�g���z�u����Ă��邩�m�F���Ă�������", HelpBoxMessageType.Error);

            importCheckColumn.Add(importCheckImage);
            cityObjectCheckColumn.Add(cityObjectGroupCheckImage);

            if (initialSettings.CheckSubComponents() == true)
            {
                uiRoot.Add(initialSettingsHelpBox);
            }
            else
            {
                uiRoot.Remove(initialSettingsHelpBox);
            }

            // �����ݒ���s�{�^���������ꂽ�Ƃ�
            runButton.clicked += () =>
            {
                // �s�s���f���C���|�[�g�ςݔ���
                checkList.Add(initialSettings.CheckInstancedCityModel());
                if(initialSettings.CheckInstancedCityModel() == true)
                {
                    importCheckImage.image = checkTexture;
                    importHelpboxColumn.Remove(importCheckHelpBox);
                }
                else
                {
                    importCheckImage.image = errorTexture;
                    importHelpboxColumn.Add(importCheckHelpBox);
                }

                // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���
                checkList.Add(initialSettings.CheckCityObjectGroup());
                if(initialSettings.CheckCityObjectGroup() == true)
                {
                    cityObjectGroupCheckImage.image = checkTexture;
                    cityObjectHelpboxColumn.Remove(cityObjectCheckHelpBox);
                }
                else
                {
                    cityObjectGroupCheckImage.image = errorTexture;
                    cityObjectHelpboxColumn.Add(cityObjectCheckHelpBox);                    
                }

                // �`�F�b�N���ڂ����ׂĖ������Ă���ꍇ�����ݒ�����s
                if(checkList.Contains(false) == false)
                {
                    initialSettings.CreateSubComponents();
                    initialSettings.ExecuteInitialSettings().ContinueWithErrorCatch();
                }
            };
        }
    }
}
