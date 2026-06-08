using PlateauToolkit.Sandbox.Runtime;
using System;
using UnityEngine;

namespace Landscape2.Runtime.AdRegulation
{
    public interface IAdSizeSettingModule
    {
        // 幅
        public float Width { get; set; }
        // 広告物全体の高さ
        public float Height { get; set; }
        // 奥行
        public float Depth { get; set; }
        // 広告上部高さ
        public float AdHeight { get; set; }
        // 支柱高さ
        public float AdLength { get; set; }

        // 値が変更されたときに呼ばれるイベント
        event System.Action OnValueChanged;

        // 値の変更に失敗した時に呼ばれるイベント
        event System.Action OnFaildValueChange;

        // サイズ変更可能なオブジェクトを設定
        void SetScalableAd(PlateauSandboxAdvertisementScaled advertisementScaled);
    }

    public class AdSizeSettingModule : IAdSizeSettingModule
    {
        PlateauSandboxAdvertisementScaled advertisementScaled;

        // 値の依存関係について
        // グループA(Width, Height, Depth), グループB(AdHeight, AdLength)　次のようにグループ分けする
        // グループAの変更はBに影響を与えることはあるが、Bの変更はAに影響を与えない。
        // 例えば Heightを変更した場合は AdLength, AdHeightに影響を与える。　Adlength, AdHeightを変更した場合は Heightに影響を与えない。
        // 例　Height*2 -> AdHeight*2, AdLength*2,  AdHeight-0.5 -> AdLength+0.5

        public AdSizeSettingModule()
        {
        }

        float IAdSizeSettingModule.Width
        {
            get => advertisementScaled?.BillboardSize.x ?? 0f;
            set
            {
                if (advertisementScaled == null)
                    return;

                if (value < 0)
                {
                    OnFaildValueChange?.Invoke();
                    return; // 負の値は無効
                }

                if (advertisementScaled.BillboardSize.x != value)
                {
                    advertisementScaled.BillboardSize = new Vector3(value, advertisementScaled.BillboardSize.y, advertisementScaled.BillboardSize.z);
                    OnValueChanged?.Invoke();
                }
            }
        }

        float IAdSizeSettingModule.Height
        {
            get => advertisementScaled != null ? advertisementScaled.BillboardSize.y + advertisementScaled.PoleHeight : 0f;
            set
            {
                if (advertisementScaled == null)
                    return;

                if (value < 0)
                {
                    OnFaildValueChange?.Invoke();
                    return;
                }

                var height = advertisementScaled.BillboardSize.y + advertisementScaled.PoleHeight;
                if (height != value)
                {
                    // 高さの変化率を広告上部とポール部に適用する 高さ(a+b)x = ax + bx 上部とポール部
                    float ratio = value / height;
                    var heightAndLen = new Vector2(advertisementScaled.BillboardSize.y, advertisementScaled.PoleHeight);
                    heightAndLen = heightAndLen * ratio; // 高さを基準に正規化してから値を設定

                    // billboardの高さとpoleの高さを更新
                    advertisementScaled.BillboardSize = new Vector3(advertisementScaled.BillboardSize.x, heightAndLen.x, advertisementScaled.BillboardSize.z);
                    advertisementScaled.PoleHeight = heightAndLen.y;
                    OnValueChanged?.Invoke();
                }
            }
        }

        float IAdSizeSettingModule.Depth
        {
            get => advertisementScaled?.BillboardSize.z ?? 0f;
            set
            {
                if (advertisementScaled == null)
                    return;

                if (value < 0)
                {
                    OnFaildValueChange?.Invoke();
                    return; // 負の値は無効
                }

                if (advertisementScaled.BillboardSize.z != value)
                {
                    advertisementScaled.BillboardSize = new Vector3(advertisementScaled.BillboardSize.x, advertisementScaled.BillboardSize.y, value);
                    OnValueChanged?.Invoke();
                }
            }
        }

        float IAdSizeSettingModule.AdHeight
        {
            get => advertisementScaled?.BillboardSize.y ?? 0f;
            set
            {
                if (advertisementScaled == null)
                    return;

                var height = advertisementScaled.BillboardSize.y + advertisementScaled.PoleHeight;
                if (value < 0 || value > height)
                {
                    OnFaildValueChange?.Invoke();
                    return; // 負の値や高さを超える値は無効
                }

                if (advertisementScaled.BillboardSize.y != value)
                {
                    // billboardの高さを更新
                    advertisementScaled.BillboardSize = new Vector3(advertisementScaled.BillboardSize.x, value, advertisementScaled.BillboardSize.z);

                    // poleの高さを更新
                    advertisementScaled.PoleHeight = height - value; // 支柱の高さは高さから広告の高さを引いたもの
                    OnValueChanged?.Invoke();
                }
            }
        }

        float IAdSizeSettingModule.AdLength
        {
            get => advertisementScaled?.PoleHeight ?? 0f;
            set
            {
                if (advertisementScaled == null)
                    return;

                var height = advertisementScaled.BillboardSize.y + advertisementScaled.PoleHeight;
                if (value < 0 || value > height)
                {
                    OnFaildValueChange?.Invoke();
                    return; // 負の値や高さを超える値は無効
                }

                if (advertisementScaled.PoleHeight != value)
                {
                    // poleの高さを更新
                    advertisementScaled.PoleHeight = value;
                    // billboardの高さを更新
                    advertisementScaled.BillboardSize = new Vector3(advertisementScaled.BillboardSize.x, height - value, advertisementScaled.BillboardSize.z);
                    OnValueChanged?.Invoke();
                }
            }
        }

        private event System.Action OnValueChanged;
        event Action IAdSizeSettingModule.OnValueChanged
        {
            add
            {
                OnValueChanged += value;
            }

            remove
            {
                OnValueChanged -= value;
            }
        }

        private event Action OnFaildValueChange;
        event Action IAdSizeSettingModule.OnFaildValueChange
        {
            add
            {
                OnFaildValueChange += value;
            }

            remove
            {
                OnFaildValueChange -= value;
            }
        }

        void IAdSizeSettingModule.SetScalableAd(PlateauSandboxAdvertisementScaled advertisementScaled)
        {
            this.advertisementScaled = advertisementScaled;
            OnValueChanged?.Invoke();
        }
    }
}
