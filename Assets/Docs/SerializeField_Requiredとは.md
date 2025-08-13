## `[SerializeField, Required]` の意味と使い方

### これは何？
- **`[SerializeField]`**: 非公開フィールドをインスペクターに表示し、シリアライズ（保存）可能にする Unity の標準属性。
- **`[Required]`**: このプロジェクトで定義されたカスタム属性。未割り当て（null）のままにしてはいけない「必須参照」であることを示す印。

両方を並べて付けることで、「インスペクターに出す参照で、かつ未設定なら警告する必須項目」を表します。

### どこで使われている？
- 例: `EnemyWakame` の弾プレハブ参照
  ```csharp
  [SerializeField, Required]
  private BulletMove _bulletPrefab; // 弾丸プレハブ
  ```
  ここが未設定だと、`Runner.Spawn(_bulletPrefab, ...)` に null が渡り、実行時エラーの原因になります。

### 仕組み（エディタでの警告表示）
- `RequiredAttribute` は属性定義のみ（`Assets/Scripts/SubEditor/RequiredAttribute.cs`）。
- 実際のチェックと警告はエディタ拡張（`Assets/Editor/EmptyFieldWarningEditor.cs`）が行います。
  - シーン内の全 `MonoBehaviour` を走査
  - フィールドに `SerializeField` と `RequiredAttribute` の両方が付いているか判定
  - 値が `null`（Unity オブジェクトの「壊れ参照」含む）の場合、SceneView の左下に「Required Fields Not Assigned:」一覧を表示
  - 項目をクリックすると対象オブジェクトへフォーカス

### 何が「未設定」とみなされる？
- 参照が `null` のとき
- Unity 特有の「Missing/壊れた参照」のとき

配列やリスト自体が `null` のケースは検出されますが、要素が空であること（Count 0）は検出対象外です。

### 使い方（推奨パターン）
- 実行時に必須となる参照（Prefab、他コンポーネント、`ScriptableObject` など）に付ける
  ```csharp
  [SerializeField, Required] private GameObject _projectilePrefab;
  [SerializeField, Required] private SomeComponent _dependency;
  ```

### 実行時への影響
- ビルド後の挙動には基本影響しません（警告はエディタ限定）。
- ただし未設定のまま再生すると、参照が必要な箇所で `NullReferenceException` や API 呼び出しエラーが発生します。

### 既知の制限・注意
- プリミティブ型（`int`, `float` など）は対象外です。
- 参照型でも「存在はするが内容が不正」までは検出できません。
- 必要十分な警告にするため、実行時のガード（null チェック）も併用してください。

### 関連ファイル
- `Assets/Scripts/SubEditor/RequiredAttribute.cs`
- `Assets/Editor/EmptyFieldWarningEditor.cs`
- 使用例: `Assets/Scripts/Character/Enemy/Types/EnemyWakame.cs`
