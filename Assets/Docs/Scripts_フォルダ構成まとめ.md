### Assets/Scripts フォルダ構成まとめ

最終更新: 2025-08-23

このドキュメントは `Assets/Scripts` のディレクトリ構成と主要スクリプトの役割を俯瞰的に整理したものです。ゲームはウェーブ制の協力防衛FPSで、ネットワークは Photon Fusion2 を前提とした設計です。依存性注入（`Core/DI/ProjectInstaller.cs`）が存在し、VContainer による DI を想定した構成になっています。

---

### 全体構成（ディレクトリ）

```text
Scripts/
├─ Character/                      # 旧構成の残骸と思われる（空に近い）
│  └─ Enemy/
│     └─ Combat/
├─ Core/
│  ├─ DI/                          # 依存性注入設定（VContainer 想定）
│  ├─ GameFlow/                    # ゲーム進行管理
│  ├─ Interfaces/                  # 共通インターフェース
│  └─ Wave/                        # ウェーブ制制御・設定
├─ Gameplay/
│  ├─ Character/
│  │  ├─ Animation/
│  │  ├─ Camera/
│  │  ├─ Enemy/
│  │  │  ├─ AI/
│  │  │  │  └─ States/            # ステートパターンでAI制御
│  │  │  ├─ Core/
│  │  │  ├─ Spawning/
│  │  │  └─ Types/
│  │  ├─ Input/
│  │  └─ Pys/                      # 物理移動系（命名注意）
│  ├─ Combat/
│  ├─ Environment/
│  │  └─ Ship/
│  │     └─ Shield/
│  └─ InteractObjectScripts/
│     ├─ AttackObject/
│     ├─ Base/
│     ├─ Interfaces/
│     ├─ Objects/
│     └─ Tools/
├─ Infrastructure/                 # 共通ユーティリティ
├─ NetWork/                        # 起動・接続・プール・同期の枠組み
├─ SubEditor/                      # エディター拡張・属性
└─ UI/
   └─ Base/
```

---

### 主要スクリプトと役割（要点）

- **Core/DI**
  - `ProjectInstaller.cs`: 依存性の登録ポイント（VContainer）。

- **Core/GameFlow**
  - `GameFlowHandler.cs`: ゲーム進行（フェーズ遷移等）のハンドリング。

- **Core/Interfaces**
  - `IGameStateNotice.cs`: ゲーム状態通知のためのインターフェース。

- **Core/Wave**
  - `EnemyCoordinator.cs`: ウェーブ内の敵出現の調停。
  - `WaveConfiguration.cs` と `.asset`: ウェーブ設定の定義アセット。
  - `WaveHandler.cs`: ウェーブ進行の管理。

- **Gameplay/Character（プレイヤー）**
  - `Animation/AnimationConductor.cs`: アニメーション制御。
  - `Camera/PlayerAvatarView.cs`: 視点・アバター表示の制御。
  - `Input/GameInput.cs`, `InputManager.cs`, `PlayerNetworkInput.cs`: 入力ラッパー、入力管理、ネットワーク入力の橋渡し。
  - `ISetPlayerInformation.cs`: プレイヤー情報設定のための契約。
  - `NetworkedDamageable.cs`: ネットワーク同期下での被ダメージ処理。
  - `Pys/PlayerMove.cs`, `PlayerMovement.cs`, `PlayerPhysicsMove.cs`, `PlayerJump.cs`, `RotationMove.cs`: 物理/移動系。

- **Gameplay/Character/Enemy（敵）**
  - `AI/EnemyAIBrainState.cs` と `AI/States/*`: ステートパターン（Idle/Chase/Attack/Dead）。
  - `Core/BaseEnemy.cs`: 敵の基底実装。
  - `Spawning/WaveSpawner.cs`: ウェーブに応じた敵スポーン。
  - `Types/EnemyShooter.cs`, `EnemyWakame.cs`: 敵タイプ実装。

- **Gameplay/Combat**
  - `BulletMove.cs`: 弾丸の挙動。

- **Gameplay/Environment/Ship/Shield**
  - `ShipShieldSystem.cs`, `ShipShieldDurability.cs`, `ShipShieldVisualizer.cs`: シップシールドの制御・耐久・可視化。

- **Gameplay/InteractObjectScripts**
  - `Base/*`: 取扱可能オブジェクトの基底・共通化。
  - `Interfaces/*`: 取扱I/F（ボタン、ツール、制御）。
  - `Objects/*`: 砲台・スイッチ・修理ステーション等の具体実装。
  - `AttackObject/LaserGun.cs`: 攻撃オブジェクト（銃）。
  - `Tools/ShieldChargePot.cs`: シールドチャージ用ツール。

- **Infrastructure**
  - `GameObjectExtensions.cs`, `TransformCalculation.cs`: 共通拡張・計算ユーティリティ。

- **NetWork**
  - `GameLauncher.cs`: ゲーム起動・セッション開始周り。
  - `PhotonConnectionTest.cs`: Photon 接続検証。
  - `PoolObjectProvider.cs`: オブジェクトプーリング（Fusion Object Pool 連携想定）。

- **SubEditor**
  - `RequiredAttribute.cs`: 必須フィールド警告などのエディター属性。

- **UI**
  - `Base/PhaseEndButtonBase.cs`: フェーズ終了UIの基底。
  - `PreparationEndButton.cs`, `UpgradePhaseEndButton.cs`, `ShipShieldButton.cs`: 各フェーズ/機能の操作UI。

---

### 設計上の特徴

- **層分離**: `Core`（進行・設定）/ `Gameplay`（ロジック）/ `Infrastructure`（共通）/ `UI` / `NetWork` / `SubEditor` による関心分離。
- **ウェーブ制の中心設計**: `Core/Wave` と `Gameplay/Character/Enemy`（`Spawning`/`AI`）が連携。
- **AIはステートパターン**: `IEnemyState` と各 `State` 実装で明確な状態遷移。
- **DI 前提**: `ProjectInstaller.cs` から依存性解決。VContainer の利用を前提とした書き方。
- **ネットワーク考慮**: 入力（`PlayerNetworkInput`）や被ダメ（`NetworkedDamageable`）など、Fusion2 を想起させる責務分割。

---

### 気づき・改善候補（提案）

1. **重複/旧構成の整理**  
   ルート直下の `Scripts/Character/` と `Scripts/GamePlay/Character/` が重複しています。前者は空に近く、後者に集約されているため、残骸であれば削除・統合を検討してください。

2. **ファイル名先頭の空白**  
   `Gameplay/Character/Enemy/Spawning/ WaveSpawner.cs` に先頭空白が含まれています。Unity/OS 間での扱い差や参照不整合の原因となるため、`WaveSpawner.cs` への正規化を推奨します。

3. **命名ゆれ（Pys → Physics/Phys）**  
   `Gameplay/Character/Pys/` は `Physics` または `Phys` が意図と思われます。フォルダ・名前空間・参照を一括で整えると可読性が向上します。

4. **NetWork 配下のプレースホルダ**  
   `Connection/`, `Pooling/`, `Synchronization/` は `.meta` のみが存在します。運用上必要でない場合は削除、今後利用予定なら README を併設して意図を明記してください。

5. **アセット命名の統一**  
   `Core/Wave/WaveConfiguration.asset` と `WaveConfiguration-Miyamoto.asset` が並存します。用途（デフォルト/個別調整）を明示し、命名規約を統一すると管理しやすくなります。

---

### 依存関係の概観（非網羅）

- `Core/Wave` → 敵スポーン（`Gameplay/Character/Enemy/Spawning`）
- `UI/*` → ゲーム進行（`Core/GameFlow`）やシールド（`Gameplay/Environment/Ship/Shield`）を操作
- `NetWork/*` → 入力（`PlayerNetworkInput`）やプール（`PoolObjectProvider`）との連携を想定
- `Infrastructure/*` → 全体の補助機能（拡張・計算）

---

### 付録：検出された代表的な .cs ファイル

- Core: `ProjectInstaller.cs`, `GameFlowHandler.cs`, `IGameStateNotice.cs`, `EnemyCoordinator.cs`, `WaveConfiguration.cs`, `WaveHandler.cs`
- Gameplay/Character: `PlayerAvatar.cs`, `PlayerAvatarView.cs`, `NetworkedDamageable.cs`, `ISetPlayerInformation.cs`
- Gameplay/Character/Input: `GameInput.cs`, `InputManager.cs`, `PlayerNetworkInput.cs`
- Gameplay/Character/Pys: `PlayerMove.cs`, `PlayerMovement.cs`, `PlayerPhysicsMove.cs`, `PlayerJump.cs`, `RotationMove.cs`
- Gameplay/Character/Enemy: `BaseEnemy.cs`, `EnemyAIBrainState.cs`, `States/*`, `Types/*`, `Spawning/WaveSpawner.cs`
- Gameplay/Combat: `BulletMove.cs`
- Gameplay/Environment/Ship/Shield: `ShipShieldSystem.cs`, `ShipShieldDurability.cs`, `ShipShieldVisualizer.cs`
- Gameplay/InteractObjectScripts: `Base/*`, `Interfaces/*`, `Objects/*`, `AttackObject/LaserGun.cs`, `Tools/ShieldChargePot.cs`
- Infrastructure: `GameObjectExtensions.cs`, `TransformCalculation.cs`
- NetWork: `GameLauncher.cs`, `PhotonConnectionTest.cs`, `PoolObjectProvider.cs`
- SubEditor: `RequiredAttribute.cs`
- UI: `Base/PhaseEndButtonBase.cs`, `PreparationEndButton.cs`, `UpgradePhaseEndButton.cs`, `ShipShieldButton.cs`

---

何か追加の観点（例えばテスト戦略、シーンとの紐づけ、Prefab 依存関係など）が必要であればご指示ください。構成の自動抽出は随時再実行可能です。


