# PLATEAU 景観まちづくりDX v3.0

![分割壁面の広告物規制](./Documentation~/resources/AdvertisementRegulation/MaxSegmentLines.png)

## 1. 概要
本リポジトリでは、Project PLATEAU の令和7年度「まちづくりDXの推進に向けた3D都市モデルの利用環境向上業務」として実施した UC25-12「景観まちづくりDX v3.0」の成果物である、「景観まちづくり⽀援ツール」のソースコードを公開しています。

「景観まちづくり⽀援ツール」は、PLATEAU の 3D 都市モデルを活用し、都市計画・景観計画の検討や屋外広告物の申請・審査業務を支援する景観シミュレーションツールです。

## 2. 「景観まちづくりDX v3.0」について
「景観まちづくりDX v3.0」は、3D都市モデルを活用し、屋外広告物の壁面面積割合算出、設置規制範囲表示、2点間距離計測、アセット色彩編集などの機能を提供します。これらの機能を通じて、屋外広告物の新規（変更）申請や違反広告物の是正、景観計画等の策定・運用、景観政策業務におけるステークホルダーとの合意形成を支援することを目指しています。本システムの詳細については[技術検証レポート](https://www.mlit.go.jp/plateau/file/libraries/doc/plateau_tech_doc_0137_ver01.pdf)を参照してください。

## 3. 利用手順
本システムの構築手順及び利用手順については[利用チュートリアル](https://project-plateau.github.io/landscape-design-tool/)を参照してください。

## 4. システム概要

### 基本操作
- 視点操作はマウスとキーボードで行い、天候や時間帯の変更も可能です。
- 歩行者視点への切り替えや画面キャプチャー機能を備えています。
- 入力した住所の場所に視点を移動できます。
- 3D空間上で指定した2点間の直線距離、垂直距離、水平距離を計測できます。

### アセット配置機能
- アセットは「樹木」、「広告」、「人」、「乗り物」などに分類され、3D空間上に配置可能です。
- アセットの編集では「位置」、「向き」、「大きさ」、「色」の調整が可能です。
- 屋外広告物アセットは、画像・動画の編集と数値指定で寸法を変更できます。
- CSVやシェープファイルによるアセットの一括配置機能も提供しています。

### BIMデータのインポート
- IFCファイル形式のBIMデータを3D都市モデル上に取り込み、配置位置や角度、高さの調整が可能です。

### 建物編集機能
- 建物の色彩（無彩色を含む）や外観の編集に加え、既存建物の削除が可能です。

### GISデータの読み込み
- ShapeファイルやGeoJSONファイルを読み込み、建物情報とピンを表示できます。

### 景観計画区域の管理
- 景観計画区域の作成、編集が可能です。
- 景観計画区域の色、名前、高さ制限値、表示面（水平面と垂直面／水平面のみ／垂直面のみ）を設定できます。
- 景観計画区域の建築物の高さ変更をオン／オフで切り替えられます。
- Shapefile形式で景観計画区域の書き出し・読み込みが可能です。

### 見通し解析機能
- 視点場から眺望対象までの見通しを解析し、可視範囲を確認できます。
- 眺望対象から全方位への見通し解析も可能です。

### プロジェクト管理
- プロジェクトの新規作成、保存、読み込み、編集が可能です。

### 屋外広告物規制
- 道路境界から一定距離の屋外広告物の設置規制範囲を表示する。
- 建物の各壁面（1壁面／総壁面／分割壁面）について、壁面面積と広告物面積の割合を算出する。
- 屋外広告物の周囲に適用される設置規制範囲を表示する。

### 広域表示機能
- 動的タイル機能によって、広域表示の表示処理を軽量化する。
- 常に高解像度で表示するタイルを選択できます。

## 5. 利用技術

| 種別 | 項目 | バージョン | 内容 |
|------|------|-----------|------|
| ソフトウェア | [Unity](https://unity.com/) | 6000.3.10f1 | ハイグラフィックなゲームエンジン |
| ライブラリ | [PLATEAU SDK for Unity](https://github.com/Project-PLATEAU/PLATEAU-SDK-for-Unity) | 4.1.0 | 3D都市モデルデータをUnityで扱うためのツールキット<br>モデルのインポートや形式変換に対応 |
| ライブラリ | [PLATEAU SDK Rendering Toolkit for Unity](https://github.com/Project-PLATEAU/PLATEAU-SDK-Toolkits-for-Unity/tree/main/PlateauToolkit.Rendering) | 2.2.1 | 3D都市モデルデータをUnityでリアルに表示するためのツールキット<br>建築物の外観や天候、光の再現をサポート |
| ライブラリ | [PLATEAU SDK Maps Toolkit for Unity](https://github.com/Project-PLATEAU/PLATEAU-SDK-Maps-Toolkit-for-Unity) | 1.0.2 | 3D都市モデルデータをUnityで地形モデルに配置するためのツールキット<br>GISデータやBIMモデルの取り扱いに対応 |
| ライブラリ | [Triangulation](https://github.com/iShapeUnity/Triangulation) | 0.0.8 | Unityでポリゴンメッシュ生成を行うためのプラグイン |
| ライブラリ | [Save-System-for-Unity](https://github.com/IntoTheDev/Save-System-for-Unity) | 1.8.0 | Unityでゲームの状態を保存・読み込みするためのプラグイン |
| ライブラリ | [HDRP-Custom-Passes](https://github.com/alelievr/HDRP-Custom-Passes) | masterブランチ | UnityでのFX表現のプリセットを多く含んだプラグイン |
| ライブラリ | [Easy GIS .NET](https://github.com/wfletcher/EasyGIS.NET) | 4.6.2 | .NET環境向けのGISライブラリ<br>Shapefile形式の読込み及び書出しに利用 |
| ライブラリ | [RuntimeTransformHandle](https://github.com/pshtif/RuntimeTransformHandle) | 0.1.4 | アセット移動を処理するためのライブラリ |
| ライブラリ | [Mesh2d](https://github.com/iShapeUnity/Mesh2d) | 0.0.9 | 線分や曲線から2Dのメッシュを作成するためのライブラリ |
| ライブラリ | [Triangle.Net-for-Unity](https://github.com/Nox7atra/Triangle.Net-for-Unity) | 1.0.0 | 立体形状からポリゴンメッシュを作成するためのライブラリ |
| ライブラリ | [UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser/) | 1.2 | Unity環境でファイルダイアログを使用するためのライブラリ |
| ライブラリ | [NormalizeJapaneseAddressesNET](https://github.com/mikihiro-t/NormalizeJapaneseAddressesNET?tab=readme-ov-file) | 2.10 | 住所を正規化するためのライブラリ |
| ライブラリ | [Unicolour](https://github.com/waacton/Unicolour) | 6.2.0 | マンセル値とRGB間の変換のためのライブラリ |

## 6. 動作環境

### 検証済環境

| 項目              | 最小動作環境                | 推奨動作環境              | 
|------------------|--------------------------|--------------------------| 
| CPU             | Intel クロック周波数 2GHz 以上 | Core i7（8コア）以上                     | 
| GPU             | NVIDIA® GeForce RTX™  3060以上| NVIDIA® GeForce RTX™ 4060 Laptop GPU                      | 
| メモリ          | 16GB 以上                 |  32GB 以上                         | 
| ストレージ      | 200GB 以上の空き容量       | 同左                      | 
| OS             | Windows 11 Home 64 ビット | 同左                      |

## 7. 本リポジトリのフォルダ構成
本システムは、Unityのプラグインとして構成されています。

## 8. ライセンス

- ソースコード及び関連ドキュメントの著作権は国土交通省に帰属します。
- 本ドキュメントは[Project PLATEAUのサイトポリシー](https://www.mlit.go.jp/plateau/site-policy/)（CCBY4.0及び政府標準利用規約2.0）に従い提供されています。

## 9. 注意事項

- 本リポジトリは参考資料として提供しているものです。動作保証は行っていません。
- 本リポジトリについては予告なく変更又は削除をする可能性があります。
- 本リポジトリの利用により生じた損失及び損害等について、国土交通省はいかなる責任も負わないものとします。

## 10. 参考資料
- 技術検証レポート: https://www.mlit.go.jp/plateau/file/libraries/doc/plateau_tech_doc_0137_ver01.pdf
- PLATEAU WebサイトのUse caseページ「景観まちづくりDX v3.0　まちづくりDXの推進に向けた3D都市モデルの利用環境向上業務～景観まちづくり支援ツールの活用実証～」: https://www.mlit.go.jp/plateau/use-case/uc25-12/
