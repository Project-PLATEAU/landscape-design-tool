using Landscape2.Runtime.Common;
using PLATEAU.CityInfo;
using PLATEAU.Native;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Landscape2.Runtime
{
    public class BIMImport : ISubComponent
    {
        protected class LoadIfcData
        {
            public GameObject obj;
            public byte[] glbData;

            public Mesh combinedMesh;
        };
        enum LayoutMode
        {
            Trans,
            Rot,
            Scale,
            None
        };
        private const float K_GroundCheckLength = 10000.0f;

        VisualElement uiRoot;

        BIMImportUI ui;

        BIMLayoutUI layoutUI;

        EditMode editMode = new();

        string filePath;

        GameObject currentLoadIfcObject;

        Dictionary<string, LoadIfcData> loadIfcDataDict = new();

        BIMImportSaveLoadSystem saveLoadSystem;


        // uiの表示状態
        bool uiStatus = false;

        LayoutMode layoutMode;

        PLATEAUInstancedCityModel instancedCityModel;

        bool IsRootUIVisible => uiRoot.style.display == DisplayStyle.Flex;

        public BIMImport(VisualElement uiRoot, SaveSystem saveSystem)
        {

            this.uiRoot = uiRoot;
            ui = new(uiRoot);

            ui.openFileAction += (path) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("pathがemptyかnullです");
                    return;
                }

                Debug.Log($"path: {path}");
                filePath = path;
                ui.FileNameLabel = Path.GetFileName(filePath);
            };

            // import開始
            ui.importButtonOnClickAction += async () => await ImportButtonOnClickAction();


            InitializeLayoutUI(uiRoot);
            ChangeEditMode(LayoutMode.None);

            saveLoadSystem = new(saveSystem);

            saveLoadSystem.loadCallback += async list =>
            {
                foreach (var ifc in list)
                {
                    var go = await BIMLoader.Instance.CreateBIM(ifc.GlbArray);
                    go.name = ifc.Name;
                    ifc.SetID(go.GetInstanceID().ToString());
                    
                    go.transform.position = ifc.Position;
                    go.transform.rotation = Quaternion.Euler(ifc.Angle);
                    go.transform.localScale = ifc.Scale;

                    var mesh = CombineIFCMesh(go);

                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mesh;
                    
                    saveLoadSystem.AddSaveData(ifc);
                    loadIfcDataDict.TryAdd(
                        go.GetInstanceID().ToString(),
                        new()
                        {
                            obj = go,
                            glbData = ifc.GlbArray,
                            combinedMesh = mesh
                        }
                    );
                }
            };
            
            saveLoadSystem.deleteCallback += list =>
            {
                
                foreach (var ifc in list)
                {
                    var gameObjectID = ifc.ID;
                    if (gameObjectID == currentLoadIfcObject?.GetInstanceID().ToString())
                    {
                        ResetSelect();
                    }
                    
                    if (loadIfcDataDict.ContainsKey(gameObjectID))
                    {
                        var obj = loadIfcDataDict[gameObjectID].obj;
                        GameObject.DestroyImmediate(obj, true);
                        loadIfcDataDict.Remove(gameObjectID);
                    }
                }
            };
            
            saveLoadSystem.projectChangeCallback += (editList, noEditList) =>
            {
                // 選択状態解除
                ResetSelect();
                
                foreach (var bimImportSaveData in editList)
                {
                    if (!loadIfcDataDict.TryGetValue(bimImportSaveData.ID, out var value))
                    {
                        continue;
                    }
                    var ifc = value.obj;
                    LayerMaskUtil.SetIgnore(ifc, false);
                }
                
                foreach (var bimImportSaveData in noEditList)
                {
                    if (!loadIfcDataDict.TryGetValue(bimImportSaveData.ID, out var value))
                    {
                        continue;
                    }
                    var ifc = value.obj;
                    
                    // 押せないようにする
                    LayerMaskUtil.SetIgnore(ifc, true);
                }
            };
        }

        private void ResetSelect()
        {
            ChangeEditMode(LayoutMode.None);
            layoutUI.ReleaseTarget();
            layoutUI.Show(false);

            currentLoadIfcObject = null;

            ui.Show(true);
        }

        void InitializeLayoutUI(VisualElement uiRoot)
        {
            layoutUI = new(uiRoot);

            layoutUI.OnClickTransButton += () =>
            {
                ChangeEditMode(LayoutMode.Trans);
            };

            layoutUI.OnClickRotateButton += () =>
            {
                ChangeEditMode(LayoutMode.Rot);
            };

            layoutUI.OnClickScaleButton += () =>
            {
                ChangeEditMode(LayoutMode.Scale);
            };

            layoutUI.OnClickDeleteButton += () =>
            {

                saveLoadSystem.RemoveSaveData(currentLoadIfcObject.name);

                ChangeEditMode(LayoutMode.None);
                layoutUI.ReleaseTarget();
                layoutUI.Show(false);

                var gameObjectID = currentLoadIfcObject.GetInstanceID().ToString();
                if (loadIfcDataDict.ContainsKey(gameObjectID))
                {
                    loadIfcDataDict.Remove(gameObjectID);
                }

                GameObject.DestroyImmediate(currentLoadIfcObject, true);
                currentLoadIfcObject = null;

                ui.Show(true);
            };

            layoutUI.OnClickOKButton += () =>
            {
                
                if (saveLoadSystem.SaveDataList.Any(x => x.ID == currentLoadIfcObject.GetInstanceID().ToString()))
                {
                    saveLoadSystem.UpdateSaveData(
                        currentLoadIfcObject.transform.position,
                        currentLoadIfcObject.transform.eulerAngles,
                        currentLoadIfcObject.transform.localScale,
                        currentLoadIfcObject.GetInstanceID().ToString()
                    );
                }
                else
                {
                    saveLoadSystem.AddSaveData(
                        new BIMImportSaveData(
                            currentLoadIfcObject,
                            loadIfcDataDict[currentLoadIfcObject.GetInstanceID().ToString()]
                                .glbData
                        )
                    );
                }


                ChangeEditMode(LayoutMode.None);
                layoutUI.ReleaseTarget();
                layoutUI.Show(false);

                currentLoadIfcObject = null;

                ui.Show(true);
            };
        }

        async Task ImportButtonOnClickAction()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning($"filePathがnull or emptyです");
                return;
            }

            var loadedBIM = await BIMLoader.Instance.LoadBIM(filePath);
            var ifcObj = loadedBIM.Item1;

            if (ifcObj == null)
            {
                Debug.LogWarning($"ifc:[{filePath}] is null!!!");
                return;
            }
            
            ifcObj.layer = LayerMask.NameToLayer("Default");

            // 配置用情報作成
            var lat = ui.LatitudeValue;
            var lon = ui.LongitudeValue;
            var height = ui.HeightSliderValue;
            var yRot = ui.YawSliderValue;

            SetInstanceTransformFromInputState(ifcObj, lat, lon, height, yRot);
            var mesh = CombineIFCMesh(ifcObj);

            var mc = ifcObj.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            // TODO: 配置できたIFCファイルは
            // bytearrayとしてProjectに保存できる様にする
            // copy先のpathとlatlon,height,yawを保持する

            loadIfcDataDict.Add(
                ifcObj.gameObject.GetInstanceID().ToString(),
                new()
                {
                    obj = ifcObj,
                    glbData = loadedBIM.Item2,
                    combinedMesh = mesh
                }
            );

            saveLoadSystem.AddSaveData(
                new BIMImportSaveData(
                    ifcObj,
                    loadedBIM.Item2
                )
            );

            SetupEditMode(ifcObj);
        }


        /// <summary>
        /// instanceのTRSをlat,lon,height,yRotから設定する
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="height"></param>
        /// <param name="yRot"></param>
        private void SetInstanceTransformFromInputState(GameObject instance, float lat, float lon, int height, int yRot)
        {
            // latlon,heightからunity座標系を取得する
            var geoCoordinate = new GeoCoordinate(lat, lon, 0f);
            PlateauVector3d plateauPosition = instancedCityModel.GeoReference.Project(geoCoordinate);
            var unityPosition = new Vector3((float)plateauPosition.X, (float)plateauPosition.Y, (float)plateauPosition.Z);

            // heightは地面高さ+uiのheightがy座標になる
            if (TryGetColliderHeight(unityPosition, out var colliderHeight))
            {
                unityPosition.y = colliderHeight + height;
            }

            instance.transform.SetPositionAndRotation(
                unityPosition,
                Quaternion.Euler(0f, yRot, 0f)
            );
        }


        /// <summary>
        /// PlateauSandboxPrefabPlacementから拾ってきました。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="colliderHeight"></param>
        /// <returns></returns>
        private bool TryGetColliderHeight(Vector3 position, out float colliderHeight)
        {
            var rayStartPosition = new Vector3(position.x, K_GroundCheckLength, position.z);
            float rayDistance = K_GroundCheckLength * 2;

            // Send a ray downward to get the height of the collider.
            var ray = new Ray(rayStartPosition, Vector3.down);

            var hitPointHeights = new List<float>();
            RaycastHit[] results = Physics.RaycastAll(ray, rayDistance);
            foreach (RaycastHit rayCastHit in results)
            {
                if (rayCastHit.transform.TryGetComponent(out PLATEAUCityObjectGroup cityObjectGroup))
                {
                    if (cityObjectGroup.CityObjects.rootCityObjects.Any(o => o.CityObjectType == PLATEAU.CityGML.CityObjectType.COT_Building))
                    {
                        // 建物であればスキップ
                        continue;
                    }

                    // その他のオブジェクトは配置可能
                    hitPointHeights.Add(rayCastHit.point.y);
                }
            }

            if (0 < hitPointHeights.Count)
            {
                // 一番上にヒットしたコライダーの高さを取得
                colliderHeight = hitPointHeights.Max();
                return true;
            }

            // Not found.
            colliderHeight = 0.0f;
            return false;
        }


        /// <summary>
        /// カメラの座標から前方向にrayを飛ばした結果最初に当ったobjectの当った位置を返す
        /// </summary>
        /// <returns>当った位置。何もあたらない場合はnull</returns>
        private Vector3? UpdateCameraRayTargetPosition()
        {
            Transform cam = Camera.main.transform;
            var result = Physics.RaycastAll(cam.position, cam.forward, float.MaxValue);
            if (result.Length < 1)
            {
                Debug.LogWarning("non hitting...");
                return null;
            }

            return result[0].point;
        }

        private void UpdateUILatLon(Vector3? hitPos)
        {
            GeoCoordinate coord = default;
            if (hitPos != null)
            {
                // 何かにあたった
                coord = instancedCityModel.GeoReference.Unproject(
                    new PlateauVector3d(
                        hitPos.Value.x,
                        hitPos.Value.y,
                        hitPos.Value.z
                    )
                );

            }
            else
            {
                coord = instancedCityModel.GeoReference.Unproject(
                    new PlateauVector3d(0, 0, 0)
                );
            }

            ui.LatitudeValue = (float)coord.Latitude;
            ui.LongitudeValue = (float)coord.Longitude;
            ui.HeightSliderValue = (int)coord.Height; // でいいのか？
        }


        public void OnDisable()
        {
            BIMLoader.Instance.Dispose();

            var meshList = loadIfcDataDict.Values.Select(x => x.combinedMesh).ToList();

            foreach (var mesh in meshList)
            {
                GameObject.DestroyImmediate(mesh);
            }
        }

        public void OnEnable()
        {
        }

        public void Start()
        {
            instancedCityModel = GameObject.FindFirstObjectByType<PLATEAUInstancedCityModel>(FindObjectsInactive.Include);
        }

        public void Update(float deltaTime)
        {
            var currentUIStatus = IsRootUIVisible;
            if (!uiStatus && currentUIStatus)
            {
                // 非表示 -> 表示
                ui.Show(true);
                var hitPos = UpdateCameraRayTargetPosition();
                UpdateUILatLon(hitPos);
            }

            ui.Update(deltaTime);

            if (ui.IsShow())
            {
                // ボタンクリック時
                if (Input.GetMouseButtonDown(0))
                {
                    var cam = Camera.main;
                    var ray = cam.ScreenPointToRay(Input.mousePosition);
                    var result = Physics.RaycastAll(ray.origin, ray.direction, float.MaxValue);
                    if (0 < result.Length)
                    {
                        foreach (var hit in result)
                        {
                            var go = hit.collider.gameObject;
                            if (loadIfcDataDict.ContainsKey(go.GetInstanceID().ToString()))
                            {
                                if (currentLoadIfcObject != go)
                                {
                                    SetupEditMode(go);
                                    break;
                                }
                            }
                        }

                        if (currentLoadIfcObject == null)
                        {
                            if (!EventSystem.current.IsPointerOverGameObject())
                            {
                                UpdateUILatLon(result[0].point);
                            }
                        }

                    }
                }
            }


            if (layoutUI.IsShow)
            {
                var isEditingAssetTRS =
                    editMode.RuntimeTransformHandleScript != null &&
                    editMode.RuntimeTransformHandleScript.isDragging;
                CameraMoveByUserInput.IsCameraMoveActive = !isEditingAssetTRS;

                if (currentLoadIfcObject != null)
                {
                    layoutUI.Update(deltaTime);
                }

            }

            if (uiStatus && !currentUIStatus)
            {
                // 表示 -> 非表示

                if (layoutUI.IsShow)
                {
                    CameraMoveByUserInput.IsCameraMoveActive = true;
                    SetupLoadMode();
                }
                if (ui.IsShow())
                {
                    ui.Show(false);
                }
                // 読み込んだobjectは開放
                currentLoadIfcObject = null;
            }


            uiStatus = currentUIStatus;
        }

        public void LateUpdate(float deltaTime)
        {
        }

        void SetupLoadMode()
        {
            ui.Show(true);
            ChangeEditMode(LayoutMode.None);
            layoutUI.ReleaseTarget();
            layoutUI.Show(false);
        }


        void SetupEditMode(GameObject target)
        {
            currentLoadIfcObject = target;
            ChangeEditMode(LayoutMode.Trans);

            ui.Show(false);
            layoutUI.SetTarget(target);
            layoutUI.Show(true);

        }

        void ChangeEditMode(LayoutMode mode)
        {
            TransformType type = TransformType.Position;
            switch (mode)
            {
                case LayoutMode.Trans:
                    type = TransformType.Position;
                    layoutUI.TRSToggleState = BIMLayoutUI.TRSToggle.Transform;
                    break;
                case LayoutMode.Rot:
                    type = TransformType.Rotation;
                    layoutUI.TRSToggleState = BIMLayoutUI.TRSToggle.Rotate;
                    break;
                case LayoutMode.Scale:
                    type = TransformType.Scale;
                    layoutUI.TRSToggleState = BIMLayoutUI.TRSToggle.Scale;
                    break;
                default:
                    editMode?.OnCancel();
                    return;
            }

            layoutMode = mode;
            if (currentLoadIfcObject != null)
            {
                editMode?.CreateRuntimeHandle(currentLoadIfcObject, type, false);
            }
        }

        private Bounds? GetLargestBounds(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("GameObject が null です。");
                return null;
            }

            // 子オブジェクトを含むすべての Renderer を取得
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            if (renderers.Length <= 0)
            {
                Debug.LogWarning("Renderer が見つかりません。");
                return null;
            }

            // 最初の Renderer で初期 Bounds を設定
            Bounds combinedBounds = renderers[0].bounds;

            // すべての Renderer の Bounds を統合
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }

            return combinedBounds;
        }

        private Mesh CombineIFCMesh(GameObject ifc)
        {
            // 子オブジェクトから MeshFilter を取得
            MeshFilter[] meshFilters = ifc.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length == 0)
            {
                Debug.LogWarning("結合する MeshFilter が見つかりません");
                return null;
            }

            // `parent` の Transform 情報を取得
            Transform parentTransform = ifc.transform;
            Matrix4x4 parentMatrix = parentTransform.worldToLocalMatrix; // ワールドからローカルへの変換

            // CombineInstance 配列を作成
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh == null)
                {
                    continue;
                }

                Transform t = meshFilters[i].transform;

                // 各子オブジェクトの `TRS` を `parent` のローカル空間に変換
                Matrix4x4 localMatrix = parentMatrix * Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);

                combineInstances[i] = new CombineInstance
                {
                    mesh = meshFilters[i].sharedMesh,
                    transform = localMatrix
                };
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combineInstances, true, true);

            return combinedMesh;
        }
    }


}
