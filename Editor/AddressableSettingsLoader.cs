using System.Collections.Generic;
using System.Linq;
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
        private const string PACKAGE_NAME = "com.synesthesias.landscape-design-tool";
        private static AddressableAssetSettings currentSettings;

        // ==== 公開エントリ（遅延実行で安全に呼ぶ） ====
        public static void LoadAndAddSettings()
        {
            // ここでは何もしない：次フレームにデバウンスしてまとめて実行
            DebouncedRunner.Request();
        }

        // 実処理（次フレームに1回だけ呼ばれる）
        private static void DoWork()
        {
            // Addressables設定の取得/初期化（必要な時のみ）
            currentSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (currentSettings == null)
            {
                string settingsPath = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
                string directoryPath = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log("[AddressableSettings] Created directory: " + directoryPath);
                }

                // 既に同パスに存在するかチェック
                if (!File.Exists(settingsPath))
                {
                    var initializeSettings = AddressableAssetSettings.Create(directoryPath,
                        Path.GetFileNameWithoutExtension(settingsPath), true, true);
                    AssetDatabase.SaveAssetIfDirty(initializeSettings);
                    AddressableAssetSettingsDefaultObject.Settings = initializeSettings;
                    currentSettings = initializeSettings;
                }
                else
                {
                    // 存在するのに Settings が null のときはロード
                    currentSettings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(settingsPath);
                    AddressableAssetSettingsDefaultObject.Settings = currentSettings;
                }
            }

            if (currentSettings == null) return;

            using (new ScopedSettingsEdit(currentSettings))
            {
                AddLabels();
                AddGroupsToSettings();
            }
        }

        // ====== ラベル ======
        private static void AddLabels()
        {
            AddLabelOnce("Plateau_Assets");

            AddLabelOnce("Advertisements_Assets");
            AddLabelOnce("Buildings_Assets");
            AddLabelOnce("Humans_Assets");
            AddLabelOnce("Miscellaneous_Assets");
            AddLabelOnce("Plants_Assets");
            AddLabelOnce("Signs_Assets");
            AddLabelOnce("StreetFurnitures_Assets");
            AddLabelOnce("Vehicles_Assets");

            AddLabelOnce("RuntimeTransformHandle_Assets");
            AddLabelOnce("CustomPass");
            AddLabelOnce("UIStyleCommon");
            AddLabelOnce("ViewPointIcon");
            AddLabelOnce("LandmarkIcon");
            AddLabelOnce("AssetsPicture");
        }

        private static void AddLabelOnce(string labelName)
        {
            if (!currentSettings.GetLabels().Contains(labelName))
            {
                currentSettings.AddLabel(labelName);
                EditorUtility.SetDirty(currentSettings);
            }
        }

        // ====== グループ & エントリ ======
        private static void AddGroupsToSettings()
        {
            AddAssetGroupEntry("RuntimeTransformHandle_Assets", $"Packages/{PACKAGE_NAME}/Runtime/ArrangementAsset/Prefab/RuntimeTransformHandle.prefab", new[] { "RuntimeTransformHandle_Assets" });
            AddAssetGroupEntry("CustomPass", $"Packages/{PACKAGE_NAME}/Runtime/ArrangementAsset/Prefab/CustomPass.prefab", new[] { "CustomPass" });
            AddDirectoryEntries("AssetsPicture", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Picture", "t:Texture2D", addLabels: new[] { "AssetsPicture" });

            AddPlateauDirectoryEntries("Advertisements_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Advertisements/Prefabs");
            AddPlateauDirectoryEntries("Buildings_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Buildings/Prefabs");
            AddPlateauDirectoryEntries("Humans_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Humans/Prefabs");
            AddPlateauDirectoryEntries("Miscellaneous_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Miscellaneous/Prefabs");
            AddPlateauDirectoryEntries("Plants_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Plants/Prefabs");
            AddPlateauDirectoryEntries("Signs_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Signs/Prefabs");
            AddPlateauDirectoryEntries("StreetFurnitures_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/StreetFurnitures/Prefabs");
            AddPlateauDirectoryEntries("Vehicles_Assets", $"Packages/{PACKAGE_NAME}/HDRP Sample Assets/Vehicles/Prefabs");

            AddLineOfSightIcon();
            AddUIStyleCommonGroup();
        }

        private static void AddAssetGroupEntry(string groupName, string assetPath, IEnumerable<string> addLabels)
        {
            var targetGroup = CreateOrGetGroup(groupName);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var existing = currentSettings.FindAssetEntry(guid);
            bool changed = false;

            if (existing == null || existing.parentGroup != targetGroup)
            {
                existing = currentSettings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
                changed = true;
            }

            // address は差分があるときだけ変更
            var wantAddress = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            if (existing.address != wantAddress)
            {
                existing.address = wantAddress;
                changed = true;
            }

            foreach (var label in addLabels)
            {
                if (!existing.labels.Contains(label))
                {
                    existing.SetLabel(label, true);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(targetGroup);
                EditorUtility.SetDirty(currentSettings);
            }
        }

        private static void AddDirectoryEntries(string groupName, string directoryPath, string filter, IEnumerable<string> addLabels)
        {
            var targetGroup = CreateOrGetGroup(groupName);
            string[] guids = AssetDatabase.FindAssets(filter, new[] { directoryPath });

            foreach (var guid in guids)
            {
                var existing = currentSettings.FindAssetEntry(guid);
                bool changed = false;

                if (existing == null || existing.parentGroup != targetGroup)
                {
                    existing = currentSettings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
                    changed = true;
                }

                var wantAddress = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (existing.address != wantAddress)
                {
                    existing.address = wantAddress;
                    changed = true;
                }

                foreach (var label in addLabels)
                {
                    if (!existing.labels.Contains(label))
                    {
                        existing.SetLabel(label, true);
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(targetGroup);
                    EditorUtility.SetDirty(currentSettings);
                }
            }
        }

        private static void AddPlateauDirectoryEntries(string groupName, string directoryPath)
        {
            // "Plateau_Assets" と groupName の2ラベル
            AddDirectoryEntries(groupName, directoryPath, "", new[] { groupName, "Plateau_Assets" });
        }

        private static void AddUIStyleCommonGroup()
        {
            AddAssetGroupEntry(
                "UIStyleCommon",
                $"Packages/{PACKAGE_NAME}/Runtime/UICommon/UIStyleCommon.uss",
                new[] { "UIStyleCommon" }
            );
        }

        private static void AddLineOfSightIcon()
        {
            var groupName = "LineOfSight_Icon";
            AddAssetGroupEntry(groupName, $"Packages/{PACKAGE_NAME}/Runtime/UICommon/Images/Resources/Pin_Walkview.png",
                new[] { "ViewPointIcon" });
            AddAssetGroupEntry(groupName, $"Packages/{PACKAGE_NAME}/Runtime/UICommon/Images/Resources/Pin_Landmark.png",
                new[] { "LandmarkIcon" });
        }

        private static AddressableAssetGroup CreateOrGetGroup(string groupName)
        {
            var group = currentSettings.FindGroup(groupName);
            if (group != null) return group;

            group = currentSettings.CreateGroup(groupName, false, false, false,
                new List<AddressableAssetGroupSchema>(),
                typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            if (bundled != null)
            {
                // 既に同じ設定なら何もしない
                bool changed = false;
                if (bundled.BuildPath.GetValue(currentSettings) != AddressableAssetSettings.kLocalBuildPath)
                {
                    bundled.BuildPath.SetVariableByName(currentSettings, AddressableAssetSettings.kLocalBuildPath);
                    changed = true;
                }
                if (bundled.LoadPath.GetValue(currentSettings) != AddressableAssetSettings.kLocalLoadPath)
                {
                    bundled.LoadPath.SetVariableByName(currentSettings, AddressableAssetSettings.kLocalLoadPath);
                    changed = true;
                }
                if (changed)
                {
                    EditorUtility.SetDirty(group);
                }
            }
            EditorUtility.SetDirty(currentSettings);
            return group;
        }

        // ====== ユーティリティ ======
        private sealed class ScopedSettingsEdit : System.IDisposable
        {
            private readonly AddressableAssetSettings _settings;
            public ScopedSettingsEdit(AddressableAssetSettings s) { _settings = s; }
            public void Dispose()
            {
                // 触ったものだけピンポイント保存
                if (_settings != null) AssetDatabase.SaveAssetIfDirty(_settings);
            }
        }

        // 次フレームに1回だけ実行（デバウンス）
        private static class DebouncedRunner
        {
            private static bool _pending;
            private static bool _running;

            public static void Request()
            {
                if (_pending) return;
                _pending = true;
                EditorApplication.delayCall += RunOnce;
            }

            private static void RunOnce()
            {
                if (_running) return;
                _running = true;
                _pending = false;
                try { DoWork(); }
                finally { _running = false; }
            }
        }
    }

    public class CustomAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            Debug.Log($"OnPostProcessAllAssets Start:");
            // 1) Playerビルド/Addressablesビルド中は触らない
            if (BuildPipeline.isBuildingPlayer) return;

            // 2) Addressables配下に触れたイベントは無視（自己再発火を避ける）
            if (TouchesAddressables(imported) || TouchesAddressables(moved) || TouchesAddressables(movedFrom))
                return;

            // 3) 次フレームに一度だけ実行（OnPostprocess中は書き換えない）
            AddressableSettingsLoader.LoadAndAddSettings();
        }

        private static readonly string[] AddressablesRoots = {
            "Assets/AddressableAssetsData",          // 正式
        };

        private static bool TouchesAddressables(IEnumerable<string> paths)
        {
            if (paths == null) return false;
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (AddressablesRoots.Any(root => p.StartsWith(root))) return true;
            }
            return false;
        }
    }
}
