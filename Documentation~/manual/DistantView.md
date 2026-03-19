# 遠景表示設定

近景と遠景を適切に設定することで、パフォーマンスを最適化しつつ、都市の広がりを自然に表現できます。

遠景として設定されたエリアでは、都市モデルの詳細なデータを使用せず、より軽量な手法で描画することで、負荷を軽減しつつ視認性を向上させます。これにより、都市全体の規模を把握しやすくなり、より自然な景観を実現できます。動的タイル機能とは異なり、ストリーミングにより3D Tiles形式のデータを取得するため、オンライン環境が必要となります。

![遠景なし](../resources/DistantView/Disable.png)

![遠景あり](../resources/DistantView/Enable.png)

## 設定方法

遠景として表示するエリアを確認し、[セットアップ](../manual/Setup.md)時に都市モデルのインポート対象から除外します。

遠景エリアの設定には、[PLATEAU-SDK-Maps-Toolkit-for-Unity](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity) を使用します。

詳細な設定方法については、[利用マニュアル](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#plateau-sdk-maps-toolkit-for-unity-%E5%88%A9%E7%94%A8%E3%83%9E%E3%83%8B%E3%83%A5%E3%82%A2%E3%83%AB) の導入手順「Cesium for Unity のインストール」と利用手順「1. PLATEAUモデル位置合わせ」を参照してください。主な手順は以下の通りです。

- 導入手順
  - [Cesium for Unity のインストール](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#cesium-for-unity-%E3%81%AE%E3%82%A4%E3%83%B3%E3%82%B9%E3%83%88%E3%83%BC%E3%83%AB)
- 利用手順
  - [1. PLATEAUモデル位置合わせ](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-plateau%E3%83%A2%E3%83%87%E3%83%AB%E4%BD%8D%E7%BD%AE%E5%90%88%E3%82%8F%E3%81%9B)
    - [1-1. シーンを用意する](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-1-%E3%82%B7%E3%83%BC%E3%83%B3%E3%82%92%E7%94%A8%E6%84%8F%E3%81%99%E3%82%8B)
    - [1-2. 地形モデルを作成する](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-2-%E5%9C%B0%E5%BD%A2%E3%83%A2%E3%83%87%E3%83%AB%E3%82%92%E4%BD%9C%E6%88%90%E3%81%99%E3%82%8B)
    - [1-3. 地形モデルにPLATEAU Terrainを利用する](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-3-%E5%9C%B0%E5%BD%A2%E3%83%A2%E3%83%87%E3%83%AB%E3%81%ABplateau-terrain%E3%82%92%E5%88%A9%E7%94%A8%E3%81%99%E3%82%8B)
    - [1-4. 地形モデルにラスターをオーバーレイする](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-4-%E5%9C%B0%E5%BD%A2%E3%83%A2%E3%83%87%E3%83%AB%E3%81%AB%E3%83%A9%E3%82%B9%E3%82%BF%E3%83%BC%E3%82%92%E3%82%AA%E3%83%BC%E3%83%90%E3%83%BC%E3%83%AC%E3%82%A4%E3%81%99%E3%82%8B)
    - [1-5. Cesium for Unity上への3D都市モデルの配置](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-5-cesium-for-unity%E4%B8%8A%E3%81%B8%E3%81%AE3d%E9%83%BD%E5%B8%82%E3%83%A2%E3%83%87%E3%83%AB%E3%81%AE%E9%85%8D%E7%BD%AE)
    - [1-6. 3D都市モデルのストリーミング設定](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity?tab=readme-ov-file#1-6-3d%E9%83%BD%E5%B8%82%E3%83%A2%E3%83%87%E3%83%AB%E3%81%AE%E3%82%B9%E3%83%88%E3%83%AA%E3%83%BC%E3%83%9F%E3%83%B3%E3%82%B0%E8%A8%AD%E5%AE%9A)