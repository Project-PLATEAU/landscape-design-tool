using UnityEngine;

namespace Landscape2.Runtime
{
    public class AssetColorEditor
    {
        /// <summary>
        /// 編集対象のオブジェクト
        /// </summary>
        private GameObject target;

        /// <summary>
        /// 適用する色
        /// </summary>
        private Color? appliedColor;

        /// <summary>
        /// プレビュー用の色
        /// </summary>
        private Color? previewColor;

        public Color? AppliedColor => appliedColor;
        public Color? PreviewColor => previewColor;

        /// <summary>
        /// 編集対象を設定
        /// </summary>
        /// <param name="obj"></param>
        public void SetTarget(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("AssetColorEditor: SetTarget received null object.");
                return;
            }

            target = obj;
            appliedColor = ExtractColorData(target.transform);
            previewColor = appliedColor;
        }

        public void UpdatePreviewColor(Color? col)
        {
            if (target == null) return;

            previewColor = col;
        }

        /// <summary>
        /// 現在のプレビュー色を確定 (コミット) し、適用色として保存する。
        /// プレビュー色が null の場合は適用色も null (MPB 解除) となる。
        /// </summary>
        public void CommitPreview()
        {
            if (target == null) return;
            // 変更がない場合は何もしない (不要な再適用を避け GC/Draw 呼び出し最小化)
            if (appliedColor.HasValue && previewColor.HasValue && appliedColor.Value == previewColor.Value) return;
            if (!appliedColor.HasValue && !previewColor.HasValue) return;

            appliedColor = previewColor;
            ApplyColorData(target, appliedColor);
        }

        private static readonly string[] s_colorPropertyNames = { "_BaseColor", "_Color" };

        public static Color? ExtractColorData(Transform root)
        {
            if (root == null) return null;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!r.HasPropertyBlock())
                {
                    continue;
                }
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                foreach (var prop in s_colorPropertyNames)
                {
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(prop))
                    {
                        var materialColor = r.sharedMaterial.GetColor(prop);
                        var mpbColor = mpb.GetColor(prop);

                        if (mpbColor != materialColor)
                        {
                            return mpbColor;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 保存された colorData を target および子孫 Renderer に適用する。
        /// </summary>
        /// <param name="rootObject">適用対象ルート GameObject</param>
        /// <param name="col">適用する色。null の場合は MaterialPropertyBlock を解除して元のマテリアル状態に戻す。</param>
        public static void ApplyColorData(GameObject rootObject, Color? col)
        {
            if (rootObject == null) return;
            var renderers = rootObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                string targetProp = null;
                foreach (var prop in s_colorPropertyNames)
                {
                    if (r.sharedMaterial.HasProperty(prop))
                    {
                        targetProp = prop;
                        break;
                    }
                }
                if (targetProp == null) continue;

                // col が null の場合は MPB を解除してマテリアルのデフォルト状態に戻す
                if (col == null)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }

                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(targetProp, col.Value);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}