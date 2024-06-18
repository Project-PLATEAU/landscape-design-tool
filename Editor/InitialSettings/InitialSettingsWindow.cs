using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static PlasticPipe.PlasticProtocol.Messages.Serialization.ItemHandlerMessagesSerialization;

namespace Landscape2.Editor
{
    /// <summary>
    /// �i�σc�[����InitialSettingsWindow�̃G���g���[�|�C���g�ł��B
    /// </summary>
    public class InitialSettingsWindow : EditorWindow
    {
        [SerializeField]private VisualTreeAsset visualTreeAsset = default;
        [SerializeField]private Texture checkTexture;
        [SerializeField]private Texture errorTexture;
        private InitialSettings initialSettings = new InitialSettings();
        private VisualElement uiRoot;
        private const string UIRunButton = "RunButton"; // �����ݒ���s�{�^�����O

        private const string UIImportCheck = "ImportCheckColumn"; // �s�s���f���C���|�[�g�ςݔ��藓���O
        private const string UIImportHelpbox = "ImportHelpboxColumn"; // �s�s���f���C���|�[�g�ςݔ���Helpbox�����O
        private const string UICityObjectCheck = "CityObjectCheckColumn"; // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔��藓���O
        private const string UICityObjectHelpbox = "CityObjectHelpboxColumn"; // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���Helpbox�����O
        private const string UISubComponentsCheck = "SubComponentsCheckColumn"; // SubCompornents���������ꂽ���̔��藓���O
        private const string UISubComponentsHelpbox = "SubComponentsHelpboxColumn"; // SubCompornents���������ꂽ���̔���Helpbox�����O

        private List<bool> checkList = new List<bool>(); // �����ݒ���s�\���̔���p���X�g

        [MenuItem("PLATEAU/InitialSettings")]
        public static void Open()
        {
            var window = GetWindow<InitialSettingsWindow>("InitialSettings");
            window.Show();
        }

        public void CreateGUI()
        {
            HelpBox initialSettingsHelpBox = new HelpBox("�����ݒ肪���ɍs���Ă��܂�", HelpBoxMessageType.Info);
            HelpBox importCheckHelpBox = new HelpBox("�s�s���f�����C���|�[�g����Ă��邩�m�F���Ă�������", HelpBoxMessageType.Error);
            HelpBox cityObjectCheckHelpBox = new HelpBox("�s�s�I�u�W�F�N�g���z�u����Ă��邩�m�F���Ă�������", HelpBoxMessageType.Error);
            HelpBox subCompornentsCheckHelpBox = new HelpBox("SubCompornents�̐����Ɏ��s���܂���", HelpBoxMessageType.Error);

            uiRoot = rootVisualElement;
            VisualElement labelFromUXML = visualTreeAsset.Instantiate();
            uiRoot.Add(labelFromUXML);
            var runButton = uiRoot.Q<Button>(UIRunButton);
            runButton.SetEnabled(false);

            // �����ݒ肪���Ɏ��s����Ă��邩�̔���
            checkList.Add(!initialSettings.CheckSubComponents());
            if(initialSettings.CheckSubComponents() == true)
            {
                uiRoot.Add(initialSettingsHelpBox);
            }
            else
            {
                if(uiRoot.Contains(initialSettingsHelpBox))
                {
                    uiRoot.Remove(initialSettingsHelpBox);
                }
            }

            // �s�s���f���C���|�[�g�ς݂��̔���
            var isImport = initialSettings.CheckImportCityModel();
            AddCheckListUI(isImport, UIImportCheck, UIImportHelpbox, importCheckHelpBox);

            // �s�s�I�u�W�F�N�g���z�u����Ă��邩�̔���
            var isCityObject = initialSettings.CheckCityObjectGroup();
            AddCheckListUI(isCityObject, UICityObjectCheck, UICityObjectHelpbox, cityObjectCheckHelpBox);

            // �`�F�b�N���ڂ����ׂĖ������Ă���ꍇ�����ݒ�����s�ł���悤�ɂ���
            if (checkList.Contains(false) == false)
            {
                runButton.SetEnabled(true);
            }

            // �����ݒ���s�{�^���������ꂽ�Ƃ�
            runButton.clicked += () =>
            {
                // SubComponents�𐶐�
                var isCreate = initialSettings.CreateSubComponents();
                AddCheckListUI(isCreate, UISubComponentsCheck, UISubComponentsHelpbox, subCompornentsCheckHelpBox);

                // �����ݒ��͍Ăю��s�ł��Ȃ��悤�ɂ���
                uiRoot.Add(initialSettingsHelpBox);
                runButton.SetEnabled(false);
            };
        }
        // �`�F�b�N���X�g��UI����
        void AddCheckListUI(bool isCheck,string checkUI,string helpBoxUI,HelpBox helpbox)
        {
            var chackImage = new Image();
            var checkColumn = uiRoot.Q<VisualElement>(checkUI);
            var helpboxColumn = uiRoot.Q<VisualElement>(helpBoxUI);
            checkColumn.Add(chackImage);
            
            checkList.Add(isCheck);
            if (isCheck == true)
            {
                chackImage.image = checkTexture;
                if (helpboxColumn.Contains(helpbox))
                {
                    helpboxColumn.Remove(helpbox);
                }
            }
            else
            {
                chackImage.image = errorTexture;
                helpboxColumn.Add(helpbox);
            }
        }
    }
}
