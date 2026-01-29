# InsightImageGen

Stable Diffusion・VOICEVOXを活用したAI画像・音声生成ツール

## 機能

- **Simple Image**: プロンプトからの単一/バッチ画像生成
- **Batch Image**: JSONファイルによるキャラクター一括生成
- **Audio**: VOICEVOXによるテキスト読み上げ

## 必要環境

- Windows 10/11 (64-bit)
- .NET 8 SDK (開発時)
- Stable Diffusion WebUI (AUTOMATIC1111)
- VOICEVOX Engine

## ローカルでのビルドコマンド

### 1. 開発中のビルド
```powershell
cd app-insight-image-gen-C
dotnet build InsightMediaGenerator\InsightMediaGenerator.csproj -c Release
```

### 2. 配布用にpublish（インストーラなし）
```powershell
.\build.ps1 -SkipInstaller
```

### 3. フルビルド + インストーラ作成
```powershell
.\build.ps1
```

### 4. クリーンビルド
```powershell
.\build.ps1 -Clean
```

## 前提条件インストール

```powershell
# .NET 8 SDK確認
dotnet --version
```

### Inno Setup 6（インストーラ作成に必要）
https://jrsoftware.org/isdl.php からダウンロード

## 出力先

| 出力先 | 内容 |
|--------|------|
| `publish\` | アプリケーションファイル一式 |
| `Output\InsightImageGen_Setup_1.0.0.exe` | インストーラ |

## 設定

`appsettings.json` を編集して、Stable DiffusionとVOICEVOXのパスを設定してください。

```json
{
  "stable_diffusion": {
    "api_url": "http://127.0.0.1:7860/sdapi/v1/txt2img",
    "models_path": "C:/path/to/stable-diffusion-webui/models/Stable-diffusion",
    "lora_path": "C:/path/to/stable-diffusion-webui/models/Lora",
    "output_path": "C:/path/to/outputs"
  },
  "voicevox": {
    "api_url": "http://127.0.0.1:50021"
  }
}
```

## ライセンス

Copyright (c) Harmonic Insight. All rights reserved.
