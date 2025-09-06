# プロジェクトディレクトリ構成のみの記述にして
-------------------------------------
## ディレクトリ構成

### 📁 Animation/
アニメーション関連のアセット
- **Character/**: キャラクターアニメーション
- **Object/**: オブジェクトアニメーション

### 📁 Assets/Scripts/InteractObjectScripts/Shield/
インタラクト可能なシールド関連スクリプト

### 📁 Docs/
技術文書・仕様書
- ゲーム仕様書
- 敵システム仕様書  
- Fusion技術解説
- 開発メモ・改善点

### 📁 Editor/
Unity Editor拡張機能
- **Attribute.cs**: カスタム属性
- **AutoSave.cs**: 自動保存機能
- **EmptyFieldWarningEditor.cs**: 空フィールド警告
- **SubClassGenerator.cs**: サブクラス生成ツール

### 📁 Material/
マテリアル・テクスチャアセット

### 📁 Packages/
外部パッケージ・依存関係

### 📁 Photon/
Photon Fusion2ネットワーキングライブラリ
- **Fusion/**: コアライブラリ
  - **Assemblies/**: DLLファイル
  - **CodeGen/**: コード生成
  - **Editor/**: エディタ拡張
  - **Runtime/**: ランタイム機能
  - **Plugins/**: ネイティブプラグイン
- **FusionDemos/**: サンプルデモ
- **PhotonLibs/**: 基本ライブラリ
- **PhotonUnityNetworking/**: Unity統合

### 📁 Prefab/
プレハブアセット
- **Character/Enemy/**: 敵キャラクタープレハブ
- **Object/Bullet/**: 弾丸オブジェクトプレハブ

### 📁 Scene/
シーンファイル
- **NetWorkTest/**: ネットワークテスト用シーン
- **PhotoReal/**: メインゲームシーン

### 📁 Scripts/
C#スクリプト（メインロジック）

#### 🔫 Bullet/
弾丸システム
- **BulletMove.cs**: 弾丸移動・ラグ補償・寿命管理

#### 👤 Character/
キャラクター関連システム

##### Animation/
- **AnimationConductor.cs**: アニメーション制御

##### Camera/
- **PlayerAvatarView.cs**: プレイヤー視点カメラ

##### Enemy/
敵システム
- **AI/**: 敵AI状態管理
  - **EnemyAIBrainState.cs**: AI脳状態制御
  - **States/**: 各AI状態（攻撃、追跡、死亡、待機）
- **Combat/**: 戦闘システム
- **Core/**: 敵の基盤システム
  - **BaseEnemy.cs**: 敵の基底クラス
- **Spawning/**: 敵生成システム
  - **WaveSpawner.cs**: ウェーブ式敵生成
- **Types/**: 敵タイプ別実装
  - **EnemyShooter.cs**: 射撃型敵
  - **EnemyWakame.cs**: ワカメ型敵（ターゲット検索・自動照準）

##### Input/
入力システム
- **GameInput.cs**: ゲーム入力定義
- **InputManager.cs**: 入力管理
- **PlayerNetworkInput.cs**: ネットワーク入力同期

##### その他
- **ISetPlayerInformation.cs**: プレイヤー情報設定インターフェース
- **NetworkedDamageable.cs**: ネットワーク対応ダメージシステム
- **PlayerAvatar.cs**: プレイヤーアバター管理

##### Pys/
物理・移動システム
- **PlayerJump.cs**: ジャンプ機能
- **PlayerMove.cs**: 基本移動
- **PlayerMovement.cs**: 移動制御
- **PlayerPhysicsMove.cs**: 物理ベース移動
- **RotationMove.cs**: 回転制御

#### 🎮 Core/
コアシステム

##### Container/
- **ProjectInstaller.cs**: VContainer + VitalRouter DI統合

##### ゲームフロー
- **GameFlowHandler.cs**: ゲームフロー管理
- **Inter/IGameStateNotice.cs**: ゲーム状態通知インターフェース

##### Wave/
ウェーブシステム
- **EnemyCoordinator.cs**: 敵全体統括
- **WaveConfiguration.cs**: ウェーブ設定
- **WaveHandler.cs**: ウェーブ管理

#### 🔧 InteractObjectScripts/
インタラクト可能オブジェクト

##### AttackObject/
- **LaserGun.cs**: レーザーガン

##### Base/
- **BaseInteractButtonObject.cs**: ボタン系インタラクト基底
- **BaseInteractObject.cs**: インタラクト基底クラス

##### Interface/
- **IInteractableButton.cs**: ボタンインターフェース
- **IInteractableControllable.cs**: 制御可能インターフェース
- **IInteractableTool.cs**: ツールインターフェース

##### Other/
- **GunEmplacementController.cs**: 砲台制御
- **InteractiveSwitch.cs**: インタラクティブスイッチ

##### Shield/
- **ShieldChargePot.cs**: シールド充電ポッド
- **ShieldRepairStation.cs**: シールド修理ステーション

#### 🌐 NetWork/
ネットワーキング
- **GameLauncher.cs**: ゲーム起動・ネットワーク初期化
- **PhotonConnectionTest.cs**: Photon接続テスト
- **PoolObjectProvider.cs**: オブジェクトプール管理

#### 🚢 Ship/
戦艦システム

##### Shield/
シールドシステム
- **ShipShieldDurability.cs**: シールド耐久値管理
- **ShipShieldSystem.cs**: シールドシステム制御
- **ShipShieldVisualizer.cs**: シールド視覚化

#### 🛠️ SubEditor/
エディタ拡張
- **RequiredAttribute.cs**: 必須フィールド属性

#### 🖥️ UI/
ユーザーインターフェース

##### Base/
- **PhaseEndButtonBase.cs**: フェーズ終了ボタン基底

##### Buttons/
- **PreparationEndButton.cs**: 準備フェーズ終了ボタン
- **ShipShieldButton.cs**: シールド操作ボタン
- **UpgradePhaseEndButton.cs**: アップグレードフェーズ終了ボタン

#### 🔨 Utility/
ユーティリティ
- **GameObjectExtensions.cs**: GameObjectの拡張メソッド
- **TransformCalculation.cs**: Transform計算ヘルパー

### 📁 Settings/
プロジェクト設定
- **HDRPDefaultResources/**: HDRP設定リソース

### 📁 TutorialInfo/
チュートリアル・サンプル関連

### 📁 VisualPackage/
ビジュアル関連パッケージ
