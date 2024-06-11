using System.Collections.Generic;
using PLATEAU.Util.Async;
using System.Linq;
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
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        public InitialSettings initialSettings = new InitialSettings();

        // �����ݒ���s�{�^��
        public Button runButton;
        // �����ݒ���s�{�^�����O
        public const string UIRunButton = "RunButton";
        // �����ݒ���s�\���̔���p���X�g
        List<bool> checkList = new List<bool>();
        // �s�s���f���C���|�[�g�ςݔ���p�̔���p���x��
        public Label importCheckLabel;
        // �s�s���f���C���|�[�g�ςݔ���p����p���x�����O
        public const string UIImportCheckLabel = "ImportCheck";
        // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���p���x��
        public Label cityObjectGroupCheckLabel;
        // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���p���x�����O
        public const string UICityObjectGroupCheckLabel = "CityObjectCheck";

        public InitialSettingsWindow()
        {
        }

        [MenuItem("PLATEAU/InitialSettings")]
        public static void Open()
        {
            var window = GetWindow<InitialSettingsWindow>("InitialSettings");
            window.Show();
        }

        public void CreateGUI()
        {
            // Instantiate UXML
            VisualElement uiRoot = rootVisualElement;

            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            uiRoot.Add(labelFromUXML);
            runButton = uiRoot.Q<Button>(UIRunButton);
            importCheckLabel = uiRoot.Q<Label>(UIImportCheckLabel);
            cityObjectGroupCheckLabel = uiRoot.Q<Label>(UICityObjectGroupCheckLabel);

            if(initialSettings.CheckInitialSettings() ==true)
            {
                uiRoot.Add(new HelpBox("�����ݒ肪���ɍs���Ă��܂�", HelpBoxMessageType.Info));
            }

            // �����ݒ���s�{�^���������ꂽ�Ƃ�
            runButton.clicked += () =>
            {
                checkList.Add(initialSettings.CheckInstancedCityModel());
                if(initialSettings.CheckInstancedCityModel() == true)
                {
                    importCheckLabel.text = "�Z";
                }
                else
                {
                    importCheckLabel.text = "�~";
                    uiRoot.Add(new HelpBox("�s�s���f�����C���|�[�g����Ă��܂���", HelpBoxMessageType.Error));
                }
                checkList.Add(initialSettings.CheckCityObjectGroup());
                if(initialSettings.CheckCityObjectGroup() == true)
                {
                    cityObjectGroupCheckLabel.text = "�Z";
                }
                else
                {
                    cityObjectGroupCheckLabel.text = "�~";
                    uiRoot.Add(new HelpBox("���������܂ޓs�s�I�u�W�F�N�g���z�u����Ă��܂���", HelpBoxMessageType.Error));
                }

                // �`�F�b�N���ڂ����ׂĖ������Ă���ꍇ�����ݒ�����s
                if(checkList.All(value => true))
                {
                    initialSettings.CreateSubComponents();
                    initialSettings.ExecuteInitialSettings().ContinueWithErrorCatch();
                }
            };
        }
    }
}
