using Landscape2.Runtime.AdRegulation;
using PlateauToolkit.Sandbox.Runtime;
using UnityEngine;

namespace Landscape2.Runtime
{
    public class AdAssetSizeEditor
    {
        /// <summary>
        /// 現在アタッチされている広告ターゲットの種別。
        /// None: 未設定 / Single: <see cref="PlateauSandboxAdvertisement"/> / Scaled: <see cref="PlateauSandboxAdvertisementScaled"/>
        /// 破壊的に増える可能性があるので switch で網羅判定するコードは default 分岐を用意してください。
        /// </summary>
        public enum AdAssetKind
        {
            None = 0,
            Single = 1,
            Scaled = 2,
        }

        // ------------------------------------------------------------------
        // プライベートな機能インターフェース (最小共通契約)
        // ------------------------------------------------------------------
        private interface IAdAsset
        {
            Vector3 AdSize { get; set; }
        }

        // ビルボードサイズを扱える場合の追加能力インターフェース
        private interface IBillboardSizeCapable
        {
            float BillboardSize { get; set; }
        }

        // ポール高さを扱える場合の追加能力インターフェース
        private interface IPoleHeightCapable
        {
            float PoleHeight { get; set; }
        }

        // (将来拡張) 機能追加が必要になった場合は IBillboardSizeCapable や
        // IPoleHeightCapable などの追加インターフェースで表現する想定。

        // PlateauSandboxAdvertisement (単一サイズ版) 向けアダプタ
        private sealed class AdAdapter : IAdAsset
        {
            private readonly PlateauSandboxAdvertisement ad;

            public AdAdapter(PlateauSandboxAdvertisement ad)
            {
                this.ad = ad;
            }

            public Vector3 AdSize
            {
                get
                {
                    if (ad.defaultAdSize != Vector3.zero)
                    {
                        return Vector3.Scale(ad.defaultAdSize, ad.transform.lossyScale);
                    }
                    else
                    {
                        // defaultAdSizeが0の場合はtransformのスケールがそのままサイズになる
                        return ad.transform.lossyScale;
                    }
                }
                set
                {
                    if (ad.defaultAdSize.x != 0 && ad.defaultAdSize.y != 0 && ad.defaultAdSize.z != 0)
                    {
                        ad.transform.localScale = new Vector3(
                            value.x / ad.defaultAdSize.x,
                            value.y / ad.defaultAdSize.y,
                            value.z / ad.defaultAdSize.z);
                    }
                    else
                    {
                        ad.transform.localScale = value;
                    }
                }
            }
        }

        // PlateauSandboxAdvertisementScaled (ビルボード + ポール) 向けアダプタ
        // AdSize はビルボード部のサイズとして扱い統一的に編集できるようにする。
        private sealed class AdScaledAdapter : IAdAsset, IBillboardSizeCapable, IPoleHeightCapable
        {
            private readonly IAdSizeSettingModule adSizeSettingModule = new AdSizeSettingModule();

            public AdScaledAdapter(PlateauSandboxAdvertisementScaled scaled)
            {
                adSizeSettingModule.SetScalableAd(scaled);
            }

            public Vector3 AdSize
            {
                get => new(adSizeSettingModule.Width, adSizeSettingModule.Height, adSizeSettingModule.Depth);
                set
                {
                    adSizeSettingModule.Width = value.x;
                    adSizeSettingModule.Height = value.y;
                    adSizeSettingModule.Depth = value.z;
                }
            }

            public float BillboardSize
            {
                get => adSizeSettingModule.AdHeight;
                set => adSizeSettingModule.AdHeight = value;
            }

            public float PoleHeight
            {
                get => adSizeSettingModule.AdLength;
                set => adSizeSettingModule.AdLength = value;
            }
        }

        private IAdAsset target;

        /// <summary>
        /// 現在設定されているターゲット種別を返します。
        /// </summary>
        public AdAssetKind CurrentTargetKind
        {
            get
            {
                if (target == null) return AdAssetKind.None;
                if (target is AdScaledAdapter) return AdAssetKind.Scaled;
                return AdAssetKind.Single; // 現状 IAdAsset 実装はこの2種類のみ
            }
        }

        public void SetTarget(PlateauSandboxAdvertisement ad)
        {
            target = ad != null ? new AdAdapter(ad) : null;
        }

        public void SetTarget(PlateauSandboxAdvertisementScaled adScaled)
        {
            target = adScaled != null ? new AdScaledAdapter(adScaled) : null;
        }

        public void ClearTarget()
        {
            target = null;
        }

        public Vector3? GetAdSize() => target?.AdSize;

        public void SetAdSize(Vector3 size)
        {
            if (target == null) return;
            target.AdSize = size;
        }

        public float? GetBillboardSize()
        {
            if (target is IBillboardSizeCapable b) return b.BillboardSize;
            return null;
        }

        public void SetBillboardSize(float size)
        {
            if (target is IBillboardSizeCapable b) b.BillboardSize = size;
        }

        public float? GetPoleHeight()
        {
            if (target is IPoleHeightCapable p) return p.PoleHeight;
            return null;
        }

        public void SetPoleHeight(float height)
        {
            if (target is IPoleHeightCapable p) p.PoleHeight = height;
        }
    }
}