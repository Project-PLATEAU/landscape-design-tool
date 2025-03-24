using UnityEngine;
using PlateauToolkit.Sandbox;
using PlateauToolkit.Sandbox.Runtime;

namespace Landscape2.Runtime
{
    public enum AssetPlacedDirectionType
    {
        InValid,
        GroundPlacementHorizontal, // 地面に水平に配置
        GroundPlacementVertical, // 地面に垂直に配置
    }

    /// <summary>
    /// アセット配置向き制御コンポーネント
    /// </summary>
    public class AssetPlacedDirectionComponent : MonoBehaviour
    {
        private AssetPlacedDirectionType type = AssetPlacedDirectionType.InValid;
        private bool isPlacing = true;

        public Vector3 setPlaceNormal { get; set; } = Vector3.up;// 設置地点の法線


        private static AssetPlacedDirectionType TryCheckRequired(GameObject target)
        {
            // 配置を地面と垂直にするか平行にするか
            target.TryGetComponent(out IPlateauSandboxPlaceableObject asset);
            if (asset.IsGroundPlacementVertical())
            {
                return AssetPlacedDirectionType.GroundPlacementVertical;
            }
            else
            { 
                return AssetPlacedDirectionType.GroundPlacementHorizontal;
            }
        }
        
        public static bool TryAdd(GameObject target)
        {
            var type = TryCheckRequired(target);
            if (type == AssetPlacedDirectionType.InValid)
            {
                return false;
            }
            
            var component = target.GetComponent<AssetPlacedDirectionComponent>();
            if (component == null)
            {
                component = target.AddComponent<AssetPlacedDirectionComponent>();
            }
            component.SetType(type);
            return true;
        }
        
        public void SetPlaced()
        {
            isPlacing = false;
        }
        
        public void SetType(AssetPlacedDirectionType type)
        {
            this.type = type;
        }

        private void Update()
        {
            if (!isPlacing)
            {
                // 配置が完了したら何もしない
                return;
            }

            if (type != AssetPlacedDirectionType.InValid)
            {
                TrySetForward();
            }
        }

        private void TrySetForward()
        {
            // カメラの方向に対して正面か裏かを判定
            Vector3 dirToCamera = Camera.main.transform.position - transform.position;
            float dot = Vector3.Dot(transform.forward, dirToCamera.normalized);
            if (dot < 0)
            {
                // 裏の場合は、Y軸を中心に180度回転させて正面にする。
                transform.Rotate(0, 180, 0);
            }
            
            // 配置地点の法線に合わせて向きを変更
            transform.rotation = GetPlaceRotation();
        }

        private Quaternion GetPlaceRotation()
        {
            var toDirection = setPlaceNormal;

            // 地面に水平に配置の場合は、地面の法線に合わせて向きを変更
            if (type == AssetPlacedDirectionType.GroundPlacementHorizontal)
            {
                Vector3 placePointNormalDirection = GetPlacePointNormalDirection();
                if (placePointNormalDirection == Vector3.up)
                {
                    toDirection = Vector3.back;
                }
                else if (placePointNormalDirection == Vector3.down)
                {
                    toDirection = Vector3.forward;
                }
                else
                {
                    toDirection = Vector3.up;
                }
            }
            Vector3 handleDirectionVector = SetGroundHandleDirection();

            Quaternion toNormal = Quaternion.FromToRotation(Vector3.up, toDirection);
            Vector3 rotatedDirection = toNormal * handleDirectionVector;
            return Quaternion.LookRotation(rotatedDirection, toDirection);      
        }

        private Vector3 GetPlacePointNormalDirection()
        {
            if (Vector3.Dot(setPlaceNormal, Vector3.up) > 0.9f)
            {
                return Vector3.up;
            }
            else if (Vector3.Dot(setPlaceNormal, Vector3.down) > 0.9f)
            {
                return Vector3.down;
            }
            else if (Vector3.Dot(setPlaceNormal, Vector3.right) > 0.9f)
            {
                return Vector3.right;
            }
            else if (Vector3.Dot(setPlaceNormal, Vector3.left) > 0.9f)
            {
                return Vector3.left;
            }
            else if (Vector3.Dot(setPlaceNormal, Vector3.forward) > 0.9f)
            {
                return Vector3.forward;
            }
            else if (Vector3.Dot(setPlaceNormal, Vector3.back) > 0.9f)
            {
                return Vector3.back;
            }
            return setPlaceNormal;
        }

        Vector3 SetGroundHandleDirection()
        {
            bool isGroundVertical = type == AssetPlacedDirectionType.GroundPlacementVertical;
            if (isGroundVertical)// 地面に垂直
            {
                return Vector3.forward;
            }

            Vector3 placePointNormalDirection = GetPlacePointNormalDirection();
            if (placePointNormalDirection == Vector3.up || placePointNormalDirection == Vector3.down)
            {
                return Vector3.forward;
            }
            else
            {
                return placePointNormalDirection;
            }
        }
    }
}