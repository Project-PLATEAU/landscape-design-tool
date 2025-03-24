using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;

namespace Landscape2.Editor
{
    public static class AddressableSettingsLoader
    {
        private static AddressableAssetSettings currentSettings;
        public static void LoadAndAddSettings()
        {
            // Addressableの取得
            currentSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (currentSettings == null)
            {
                string settingsPath = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
                string directoryPath = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log("Created directory: " + directoryPath);
                }
                AddressableAssetSettings initializeSettings = AddressableAssetSettings.Create(directoryPath, Path.GetFileNameWithoutExtension(settingsPath), true, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AddressableAssetSettingsDefaultObject.Settings = initializeSettings;
                currentSettings = AddressableAssetSettingsDefaultObject.Settings;
            }
            // ラベルの追加
            AddLabels();
            AddGroupsToSettings();
        }
        private static void AddLabels()
        {
            AddLabel("Plateau_Assets");

            AddLabel("Advertisements_Assets");
            AddLabel("Buildings_Assets");
            AddLabel("Humans_Assets");
            AddLabel("Miscellaneous_Assets");
            AddLabel("Plants_Assets");
            AddLabel("Signs_Assets");
            AddLabel("StreetFurnitures_Assets");
            AddLabel("Vehicles_Assets");

            AddLabel("RuntimeTransformHandle_Assets");
            AddLabel("CustomPass");
            AddLabel("UIStyleCommon");
            AddLabel("ViewPointIcon");
            AddLabel("LandmarkIcon");
            AddLabel("AssetsPicture");
        }

        private static void AddLabel(string labelName)
        {
            if (!currentSettings.GetLabels().Contains(labelName))
            {
                currentSettings.AddLabel(labelName);
            }
        }

        private static void AddGroupsToSettings()
        {
            // 第一引数 : グループ名、第二引数 : パス 
            AddAssetGroup("RuntimeTransformHandle_Assets", "Packages/com.synesthesias.landscape-design-tool-2/Runtime/ArrangementAsset/Prefab/RuntimeTransformHandle.prefab");
            AddAssetGroup("CustomPass", "Packages/com.synesthesias.landscape-design-tool-2/Runtime/ArrangementAsset/Prefab/CustomPass.prefab");
            AddAssetsPictureGroup("AssetsPicture", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Picture");

            AddPlateauAssetGroup("Advertisements_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Advertisements/Prefabs");
            AddPlateauAssetGroup("Buildings_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Buildings/Prefabs");
            AddPlateauAssetGroup("Humans_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Humans/Prefabs");
            AddPlateauAssetGroup("Miscellaneous_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Miscellaneous/Prefabs");
            AddPlateauAssetGroup("Plants_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Plants/Prefabs");
            AddPlateauAssetGroup("Signs_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Signs/Prefabs");
            AddPlateauAssetGroup("StreetFurnitures_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/StreetFurnitures/Prefabs");
            AddPlateauAssetGroup("Vehicle_Assets", "Packages/com.synesthesias.landscape-design-tool-2/HDRP Sample Assets/Vehicles/Prefabs");

            AddUIStyleCommonGroup();
            AddLineOfSightIcon();
        }

        private static void AddAssetGroup(string groupName, string path)
        {
            var targetGroup = CreateGroup(groupName);
            var guid = AssetDatabase.AssetPathToGUID(path);
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel(groupName, true);
        }

        private static void AddAssetsPictureGroup(string groupName, string directoryPath)
        {
            var targetGroup = CreateGroup(groupName);
            string[] picturesGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { directoryPath });

            foreach (var guid in picturesGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
                entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                entry.SetLabel(groupName, true);
            }
        }
        private static void AddUIStyleCommonGroup()
        {
            var groupName = "UIStyleCommon";
            var targetGroup = CreateGroup(groupName);

            var guid = AssetDatabase.AssetPathToGUID("Packages/com.synesthesias.landscape-design-tool-2/Runtime/UICommon/UIStyleCommon.uss");
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel("UIStyleCommon", true);
        }
        private static void AddPlateauAssetGroup(string groupName, string directoryPath)
        {
            var targetGroup = CreateGroup(groupName);
            string[] propsAssetGUIDs = AssetDatabase.FindAssets("", new[] { directoryPath });

            foreach (var guid in propsAssetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
                entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                entry.SetLabel(groupName, true);
                entry.SetLabel("Plateau_Assets", true);
            }
        }
        private static void AddLineOfSightIcon()
        {
            var groupName = "LineOfSight_Icon";
            var targetGroup = CreateGroup(groupName);
            // 視点場アイコン
            var guid = AssetDatabase.AssetPathToGUID("Packages/com.synesthesias.landscape-design-tool-2/Runtime/UICommon/Images/Resources/Pin_Walkview.png");
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel("ViewPointIcon", true);
            // 眺望視点アイコン
            guid = AssetDatabase.AssetPathToGUID("Packages/com.synesthesias.landscape-design-tool-2/Runtime/UICommon/Images/Resources/Pin_Landmark.png");
            assetPath = AssetDatabase.GUIDToAssetPath(guid);
            entry = currentSettings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel("LandmarkIcon", true);

        }
        private static AddressableAssetGroup CreateGroup(string groupName)
        {
            var targetGroup = currentSettings.FindGroup(groupName);
            if (targetGroup != null)
            {
                return targetGroup;
            }
            targetGroup = currentSettings.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema>(), typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            var bundledAssetGroupSchema = targetGroup.GetSchema<BundledAssetGroupSchema>();
            if (bundledAssetGroupSchema != null)
            {
                // Build & Load PathをLocalに変更
                bundledAssetGroupSchema.BuildPath.SetVariableByName(currentSettings, AddressableAssetSettings.kLocalBuildPath);
                bundledAssetGroupSchema.LoadPath.SetVariableByName(currentSettings, AddressableAssetSettings.kLocalLoadPath);
                EditorUtility.SetDirty(targetGroup);
                AssetDatabase.SaveAssets();
            }
            return targetGroup;
        }
    }

    public class CustomAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            AddressableSettingsLoader.LoadAndAddSettings();
        }
    }
}