# InsightMovie API ドキュメント

InsightMovie の全機能を外部から操作する REST API。
Claude Code、Python スクリプト、その他のプログラムから画像・音声の自動生成を実現します。

## 目次

- [起動方法](#起動方法)
- [認証](#認証)
- [レスポンス形式](#レスポンス形式)
- [エラーハンドリング](#エラーハンドリング)
- [レートリミット](#レートリミット)
- [API リファレンス](#api-リファレンス)
  - [Health / Status](#health--status)
  - [Models](#models)
  - [Images](#images)
  - [Audio](#audio)
  - [Prompts](#prompts)
  - [Jobs](#jobs)
  - [Pipelines](#pipelines)
- [使用例](#使用例)
  - [Python](#python-による自動化)
  - [curl](#curl-コマンド)
  - [Claude Code 連携](#claude-code-連携)
- [設定](#設定)

---

## 起動方法

```bash
cd InsightMediaGenerator.Api
dotnet run
```

サーバーが `http://localhost:5100` で起動します。
Swagger UI: `http://localhost:5100` (開発モード時)

### 前提条件

| サービス | URL | 用途 |
|---------|-----|------|
| Stable Diffusion WebUI | `http://127.0.0.1:7860` | 画像生成 |
| VOICEVOX Engine | `http://127.0.0.1:50021` | 音声合成 |

両方のサービスが起動していなくても API サーバー自体は起動します。
接続状態は `GET /api/status` で確認できます。

---

## 認証

`X-API-Key` ヘッダーで認証します。

```
X-API-Key: your-api-key-here
```

- `appsettings.json` の `ApiSecurity.ApiKey` に値を設定すると認証が有効になります
- 空文字の場合は認証なし（開発モード）
- `/api/health` と Swagger UI は認証不要

### 認証エラー

```json
{
  "success": false,
  "error": "Invalid or missing API key. Set 'X-API-Key' header.",
  "timestamp": "2026-01-30T12:00:00Z"
}
```

HTTP Status: `401 Unauthorized`

---

## レスポンス形式

全エンドポイントが統一エンベロープを返します。

### 成功時

```json
{
  "success": true,
  "data": { ... },
  "timestamp": "2026-01-30T12:00:00Z"
}
```

### 失敗時

```json
{
  "success": false,
  "error": "エラーメッセージ",
  "timestamp": "2026-01-30T12:00:00Z"
}
```

---

## エラーハンドリング

| HTTP Status | 意味 | 対処 |
|-------------|------|------|
| `400` | 入力値不正 | リクエストを修正 |
| `401` | 認証失敗 | `X-API-Key` ヘッダーを確認 |
| `404` | リソース未検出 | ID を確認 |
| `429` | レートリミット超過 | しばらく待ってリトライ |
| `502` | バックエンド(SD/VOICEVOX)エラー | バックエンドの状態を確認 |
| `504` | タイムアウト | `/async` エンドポイントの使用を検討 |

---

## レートリミット

| ポリシー | 制限 | 対象 |
|---------|------|------|
| グローバル | 30リクエスト/分/IP | 全エンドポイント |
| 生成系 | 5件同時/IP | 画像・音声生成 |

リクエストボディ上限: 10MB
リクエストタイムアウト: 300秒（設定変更可能）

---

## API リファレンス

### Health / Status

#### `GET /api/health`

認証不要。サーバーの稼働状態を確認します。

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "status": "ok",
    "version": "2.0.0",
    "uptime_seconds": 3600
  }
}
```

---

#### `GET /api/status`

Stable Diffusion と VOICEVOX の接続状態を返します。

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "stable_diffusion": {
      "connected": true,
      "url": "http://127.0.0.1:7860/sdapi/v1/txt2img"
    },
    "voicevox": {
      "connected": true,
      "url": "http://127.0.0.1:50021"
    }
  }
}
```

---

#### `GET /api/config`

現在のデフォルトパラメータを返します。

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "model": "dreamshaper_8.safetensors",
    "sampler": "DPM++ 2M Karras",
    "steps": 30,
    "cfg_scale": 6,
    "width": 768,
    "height": 768,
    "lora_weight": 0.8,
    "speaker_id": 3
  }
}
```

---

### Models

#### `GET /api/models`

利用可能な Stable Diffusion チェックポイント一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": ["dreamshaper_8.safetensors", "sd_xl_base_1.0.safetensors"]
}
```

---

#### `GET /api/models/loras`

利用可能な LoRA モデル一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": ["character_lora.safetensors", "style_lora.safetensors"]
}
```

---

#### `GET /api/models/samplers`

利用可能なサンプラー一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    "DPM++ 2M Karras", "DPM++ SDE Karras", "DPM++ 2M SDE Karras",
    "Euler a", "Euler", "Heun", "LMS", "DDIM"
  ]
}
```

---

#### `GET /api/models/resolutions`

推奨解像度一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    { "width": 512, "height": 512, "label": "512x512" },
    { "width": 768, "height": 768, "label": "768x768" },
    { "width": 1024, "height": 1024, "label": "1024x1024" },
    { "width": 512, "height": 768, "label": "512x768 (Portrait)" },
    { "width": 768, "height": 512, "label": "768x512 (Landscape)" }
  ]
}
```

---

### Images

#### `POST /api/images/generate`

画像を同期的に生成します。生成完了まで応答をブロックします。

**リクエスト:**
```json
{
  "prompt": "beautiful elf woman with long black hair, fantasy art",
  "negative_prompt": "bad quality, deformed",
  "model": "dreamshaper_8.safetensors",
  "lora": "character_lora.safetensors",
  "lora_weight": 0.8,
  "steps": 30,
  "cfg_scale": 6,
  "width": 768,
  "height": 768,
  "sampler": "DPM++ 2M Karras",
  "char_name": "elf_001",
  "batch_count": 1
}
```

| フィールド | 型 | 必須 | デフォルト | 制約 |
|-----------|-----|------|-----------|------|
| `prompt` | string | **必須** | - | - |
| `negative_prompt` | string | - | `""` | - |
| `model` | string | - | config値 | - |
| `lora` | string | - | null | - |
| `lora_weight` | double | - | config値 | 0〜2.0 |
| `steps` | int | - | config値 | 1〜150 |
| `cfg_scale` | double | - | config値 | 1〜30 |
| `width` | int | - | config値 | 64〜2048, 8の倍数 |
| `height` | int | - | config値 | 64〜2048, 8の倍数 |
| `sampler` | string | - | config値 | - |
| `char_name` | string | - | `"image"` | パス文字禁止 |
| `batch_count` | int | - | `1` | 1〜100 |

**レスポンス:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "file_name": "elf_001_20260130_120000.png",
      "file_path": "C:/output/elf_001_20260130_120000.png",
      "timestamp": "2026-01-30T12:00:00",
      "model": "dreamshaper_8.safetensors",
      "lora": "character_lora.safetensors",
      "lora_weight": 0.8,
      "prompt": "beautiful elf woman...",
      "negative_prompt": "bad quality...",
      "steps": 30,
      "width": 768,
      "height": 768,
      "sampler": "DPM++ 2M Karras",
      "cfg_scale": 6,
      "char_name": "elf_001",
      "batch_index": 0
    }
  ]
}
```

---

#### `POST /api/images/generate/async`

画像を非同期で生成します。ジョブIDを即時返却します。

**リクエスト:** `/generate` と同じ

**レスポンス (202 Accepted):**
```json
{
  "success": true,
  "data": {
    "job_id": "job_abc123...",
    "type": "ImageGeneration",
    "status": "Queued",
    "progress": 0,
    "message": null,
    "created_at": "2026-01-30T12:00:00Z",
    "completed_at": null,
    "result": null
  }
}
```

`GET /api/jobs/{job_id}` でポーリングして完了を確認してください。

---

#### `POST /api/images/batch`

複数キャラクターの画像を一括生成（常に非同期）。

**方法1: キャラクターを直接指定**
```json
{
  "characters": [
    {
      "name": "エルフ",
      "file_name": "elf",
      "prompt": "beautiful elf woman with long black hair",
      "negative_prompt": "bad quality"
    },
    {
      "name": "戦士",
      "file_name": "warrior",
      "prompt": "handsome warrior in golden armor",
      "negative_prompt": "bad quality"
    }
  ],
  "model": "dreamshaper_8.safetensors",
  "steps": 30,
  "batch_count": 2
}
```

**方法2: 登録済みJSONファイルを指定**
```json
{
  "json_file_id": 1,
  "steps": 30,
  "batch_count": 2
}
```

**レスポンス (202 Accepted):** ジョブIDを返却。ポーリングで結果取得。

**バッチ結果の状態:**
- 全成功: `"Batch complete: N image(s) generated"`
- 部分成功: `"Batch partial: N succeeded, M failed"` + `errors` 配列
- 全失敗: ジョブステータスが `Failed`

---

#### `GET /api/images`

生成済み画像の一覧を返します。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "file_name": "elf_001_20260130_120000.png",
      "file_path": "...",
      "timestamp": "2026-01-30T12:00:00",
      "model": "dreamshaper_8.safetensors",
      "prompt": "...",
      "steps": 30,
      "width": 768,
      "height": 768,
      "char_name": "elf_001",
      "batch_index": 0
    }
  ]
}
```

---

### Audio

#### `POST /api/audio/generate`

音声を同期的に生成します。

**リクエスト:**
```json
{
  "text": "こんにちは、世界！",
  "speaker_id": 3,
  "speed": 1.0,
  "pitch": 0.0,
  "intonation": 1.0,
  "volume": 1.0,
  "save_file": true,
  "file_name": "greeting"
}
```

| フィールド | 型 | 必須 | デフォルト | 制約 |
|-----------|-----|------|-----------|------|
| `text` | string | **必須** | - | 10000文字以内 |
| `speaker_id` | int | - | config値 | - |
| `speed` | double | - | `1.0` | 0.5〜2.0 |
| `pitch` | double | - | `0.0` | -0.15〜0.15 |
| `intonation` | double | - | `1.0` | 0〜2.0 |
| `volume` | double | - | `1.0` | 0〜2.0 |
| `save_file` | bool | - | `true` | - |
| `file_name` | string | - | null | パス文字禁止 |

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "file_path": "./data/audio/greeting_20260130_120000.wav",
    "file_size_bytes": 48000
  }
}
```

---

#### `POST /api/audio/generate/async`

音声を非同期で生成。リクエストは同期版と同じ。

**レスポンス (202 Accepted):** ジョブIDを返却。

---

#### `GET /api/audio/speakers`

VOICEVOX の話者一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    {
      "id": 0,
      "name": "四国めたん (ノーマル)",
      "speaker_name": "四国めたん",
      "style_name": "ノーマル"
    },
    {
      "id": 3,
      "name": "ずんだもん (ノーマル)",
      "speaker_name": "ずんだもん",
      "style_name": "ノーマル"
    }
  ]
}
```

---

### Prompts

#### `GET /api/prompts`

登録済みプロンプトファイル一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "file_name": "fantasy_characters.json",
      "file_path": "./data/json_files/fantasy_characters.json",
      "uploaded_at": "2026-01-30T12:00:00",
      "comment": "ファンタジーキャラ定義"
    }
  ]
}
```

---

#### `POST /api/prompts`

キャラクタープロンプトファイルを登録。

**リクエスト:**
```json
{
  "file_name": "fantasy_characters",
  "comment": "ファンタジーキャラ定義",
  "characters": [
    {
      "name": "エルフ",
      "file_name": "elf",
      "prompt": "beautiful elf woman with long black hair",
      "negative_prompt": "bad quality"
    },
    {
      "name": "戦士",
      "file_name": "warrior",
      "prompt": "handsome warrior in golden armor",
      "negative_prompt": "bad quality"
    }
  ]
}
```

**レスポンス (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "file_name": "fantasy_characters.json",
    "file_path": "./data/json_files/fantasy_characters.json",
    "uploaded_at": "2026-01-30T12:00:00Z",
    "comment": "ファンタジーキャラ定義"
  }
}
```

---

#### `GET /api/prompts/{id}/characters`

指定ファイルのキャラクター定義を取得。

**レスポンス:**
```json
{
  "success": true,
  "data": [
    {
      "name": "エルフ",
      "file_name": "elf",
      "prompt": "beautiful elf woman with long black hair",
      "negative_prompt": "bad quality"
    }
  ]
}
```

---

#### `PATCH /api/prompts/{id}`

プロンプトファイルのコメントを更新。

**リクエスト:**
```json
{
  "comment": "更新されたコメント"
}
```

---

#### `DELETE /api/prompts/{id}`

プロンプトファイルをディスクとDBから削除。

---

### Jobs

#### `GET /api/jobs/{jobId}`

非同期ジョブの状態と結果を取得。

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "job_id": "job_abc123...",
    "type": "ImageGeneration",
    "status": "Completed",
    "progress": 100,
    "message": "Generated 3 image(s)",
    "created_at": "2026-01-30T12:00:00Z",
    "completed_at": "2026-01-30T12:02:30Z",
    "result": [
      {
        "id": 1,
        "file_name": "elf_001_20260130_120000.png",
        "file_path": "..."
      }
    ]
  }
}
```

**ジョブステータス:**

| Status | 説明 |
|--------|------|
| `Queued` | キューで待機中 |
| `Running` | 実行中 (`progress` で進捗確認) |
| `Completed` | 正常完了 (`result` に結果) |
| `Failed` | 失敗 (`message` にエラー内容) |
| `Cancelled` | キャンセル済み |

**注意:** 完了ジョブは60分後に自動削除されます。

---

#### `GET /api/jobs`

全ジョブ一覧。

**レスポンス:**
```json
{
  "success": true,
  "data": {
    "jobs": [ ... ],
    "total": 5
  }
}
```

---

#### `POST /api/jobs/{jobId}/cancel`

実行中のジョブをキャンセル。

---

### Pipelines

#### `POST /api/pipelines`

複数ステップを順番に自動実行するパイプラインを起動します。
1回のAPIコールで「画像生成→音声合成」のような連続操作が可能です。

**リクエスト:**
```json
{
  "name": "キャラクターシーン生成",
  "steps": [
    {
      "action": "generate_image",
      "params": {
        "prompt": "beautiful elf in enchanted forest",
        "negative_prompt": "bad quality",
        "char_name": "elf_forest",
        "steps": 30,
        "width": 768,
        "height": 768
      }
    },
    {
      "action": "generate_audio",
      "params": {
        "text": "ようこそ、エルフの森へ。",
        "speaker_id": 3,
        "speed": 0.9
      }
    },
    {
      "action": "generate_image",
      "params": {
        "prompt": "warrior facing dragon in battlefield",
        "char_name": "warrior_battle"
      }
    }
  ]
}
```

**利用可能なアクション:**

| action | 説明 | params |
|--------|------|--------|
| `generate_image` | 画像生成 | prompt, negative_prompt, model, lora, lora_weight, steps, cfg_scale, width, height, sampler, char_name |
| `generate_audio` | 音声合成 | text, speaker_id, speed, pitch, intonation, volume, save_file, file_name |
| `list_models` | モデル一覧取得 | なし |
| `list_speakers` | 話者一覧取得 | なし |
| `check_status` | 接続状態確認 | なし |

**レスポンス (202 Accepted):** ジョブIDを返却。
ポーリングで結果を取得すると、各ステップの成否が得られます：

```json
{
  "success": true,
  "data": {
    "job_id": "job_abc...",
    "status": "Completed",
    "result": {
      "pipeline_id": "job_abc...",
      "name": "キャラクターシーン生成",
      "status": "completed",
      "steps": [
        { "index": 0, "action": "generate_image", "status": "completed", "result": [...] },
        { "index": 1, "action": "generate_audio", "status": "completed", "result": {...} },
        { "index": 2, "action": "generate_image", "status": "completed", "result": [...] }
      ],
      "created_at": "2026-01-30T12:00:00Z"
    }
  }
}
```

一部ステップが失敗した場合、`status` は `"completed_with_errors"` になります。

---

## 使用例

### Python による自動化

#### インストール

```bash
pip install requests
```

#### 基本：画像1枚生成

```python
import requests

API = "http://localhost:5100/api"
HEADERS = {"X-API-Key": "your-key"}  # 認証が有効な場合

# 画像を同期生成
res = requests.post(f"{API}/images/generate", headers=HEADERS, json={
    "prompt": "beautiful elf woman with long black hair, fantasy art",
    "negative_prompt": "bad quality, deformed",
    "steps": 30,
    "width": 768,
    "height": 768
})

data = res.json()
if data["success"]:
    for img in data["data"]:
        print(f"Generated: {img['file_path']}")
else:
    print(f"Error: {data['error']}")
```

#### 非同期：大量生成をポーリング

```python
import requests
import time

API = "http://localhost:5100/api"
HEADERS = {"X-API-Key": "your-key"}

# 非同期で画像生成を開始
res = requests.post(f"{API}/images/generate/async", headers=HEADERS, json={
    "prompt": "cyberpunk city at night, neon lights",
    "char_name": "cyberpunk_city",
    "batch_count": 5
})

job_id = res.json()["data"]["job_id"]
print(f"Job started: {job_id}")

# ポーリングで完了を待つ
while True:
    status = requests.get(f"{API}/jobs/{job_id}", headers=HEADERS).json()
    job = status["data"]

    print(f"  Progress: {job['progress']:.0f}% - {job['message']}")

    if job["status"] in ("Completed", "Failed", "Cancelled"):
        break

    time.sleep(3)

if job["status"] == "Completed":
    print(f"Done! Generated {len(job['result'])} images")
    for img in job["result"]:
        print(f"  {img['file_path']}")
else:
    print(f"Job {job['status']}: {job['message']}")
```

#### パイプライン：画像+音声を一括生成

```python
import requests
import time

API = "http://localhost:5100/api"
HEADERS = {"X-API-Key": "your-key"}

# パイプラインでシーンを一括生成
res = requests.post(f"{API}/pipelines", headers=HEADERS, json={
    "name": "Episode 1 - 森の出会い",
    "steps": [
        {
            "action": "generate_image",
            "params": {
                "prompt": "elf standing in mystical forest, morning light",
                "char_name": "ep1_scene1",
                "steps": 30
            }
        },
        {
            "action": "generate_audio",
            "params": {
                "text": "静かな森の朝。エルフのアリアは木々の間を歩いていた。",
                "speaker_id": 3,
                "speed": 0.9
            }
        },
        {
            "action": "generate_image",
            "params": {
                "prompt": "warrior emerging from shadows, dramatic lighting",
                "char_name": "ep1_scene2"
            }
        },
        {
            "action": "generate_audio",
            "params": {
                "text": "突然、影の中から一人の戦士が現れた。",
                "speaker_id": 3
            }
        }
    ]
})

job_id = res.json()["data"]["job_id"]

# 完了を待つ
while True:
    job = requests.get(f"{API}/jobs/{job_id}", headers=HEADERS).json()["data"]
    print(f"Pipeline: {job['progress']:.0f}% - {job['message']}")
    if job["status"] in ("Completed", "Failed", "Cancelled"):
        break
    time.sleep(5)

# 結果を表示
pipeline = job["result"]
print(f"\nPipeline '{pipeline['name']}': {pipeline['status']}")
for step in pipeline["steps"]:
    print(f"  Step {step['index']}: {step['action']} -> {step['status']}")
```

#### 無限ループ自動生成

```python
import requests
import time
import random

API = "http://localhost:5100/api"
HEADERS = {"X-API-Key": "your-key"}

scenes = [
    {"prompt": "dragon flying over mountains", "char_name": "dragon"},
    {"prompt": "underwater palace with mermaids", "char_name": "ocean"},
    {"prompt": "floating castle in the clouds", "char_name": "sky"},
]

narrations = [
    "ドラゴンが山脈の上を飛んでいる。",
    "海底の宮殿で人魚たちが踊っている。",
    "雲の上に浮かぶ城が見えてきた。",
]

episode = 1
while True:
    print(f"\n=== Episode {episode} ===")

    steps = []
    for scene, narration in zip(scenes, narrations):
        steps.append({
            "action": "generate_image",
            "params": {**scene, "steps": 30, "width": 768, "height": 768}
        })
        steps.append({
            "action": "generate_audio",
            "params": {"text": narration, "speaker_id": 3}
        })

    res = requests.post(f"{API}/pipelines", headers=HEADERS, json={
        "name": f"Episode {episode}",
        "steps": steps
    })

    job_id = res.json()["data"]["job_id"]

    while True:
        job = requests.get(f"{API}/jobs/{job_id}", headers=HEADERS).json()["data"]
        if job["status"] in ("Completed", "Failed", "Cancelled"):
            print(f"Episode {episode}: {job['message']}")
            break
        time.sleep(5)

    episode += 1
    time.sleep(2)  # エピソード間の待機
```

---

### curl コマンド

```bash
# ヘルスチェック
curl http://localhost:5100/api/health

# 接続状態確認
curl -H "X-API-Key: your-key" http://localhost:5100/api/status

# モデル一覧
curl -H "X-API-Key: your-key" http://localhost:5100/api/models

# 話者一覧
curl -H "X-API-Key: your-key" http://localhost:5100/api/audio/speakers

# 画像生成（同期）
curl -X POST http://localhost:5100/api/images/generate \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-key" \
  -d '{"prompt": "beautiful landscape", "steps": 20}'

# 音声生成（同期）
curl -X POST http://localhost:5100/api/audio/generate \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-key" \
  -d '{"text": "こんにちは", "speaker_id": 3}'

# パイプライン実行
curl -X POST http://localhost:5100/api/pipelines \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-key" \
  -d '{
    "name": "test",
    "steps": [
      {"action": "generate_image", "params": {"prompt": "sunset"}},
      {"action": "generate_audio", "params": {"text": "夕日が美しい"}}
    ]
  }'

# ジョブ状態確認
curl -H "X-API-Key: your-key" http://localhost:5100/api/jobs/job_abc123
```

---

### Claude Code 連携

Claude Code から InsightMovie API を使って自動で動画素材を生成する例：

```bash
# Claude Code のプロンプト例：
# 「InsightMovie API (localhost:5100) を使って、
#   ファンタジーRPGのシーン5つ分の画像とナレーション音声を生成して」
```

Claude Code は curl や Python スクリプトを自動生成・実行して、
パイプラインAPIを活用した一括生成を行えます。

---

## 設定

`appsettings.json` の設定項目：

```json
{
  "app": {
    "name": "Insight Media Generator",
    "version": "2.0.0"
  },
  "stable_diffusion": {
    "api_url": "http://127.0.0.1:7860/sdapi/v1/txt2img",
    "models_path": "C:/path/to/models/Stable-diffusion",
    "lora_path": "C:/path/to/models/Lora",
    "output_path": "C:/path/to/outputs"
  },
  "voicevox": {
    "api_url": "http://127.0.0.1:50021",
    "auto_discover": true,
    "output_path": "./data/audio"
  },
  "data": {
    "json_upload_dir": "./data/json_files",
    "database_file": "./data/insight_media.db"
  },
  "defaults": {
    "model": "dreamshaper_8.safetensors",
    "sampler": "DPM++ 2M Karras",
    "steps": 30,
    "cfg_scale": 6,
    "width": 768,
    "height": 768,
    "lora_weight": 0.8,
    "speaker_id": 3
  },
  "ApiSecurity": {
    "ApiKey": "",
    "RequestTimeoutSeconds": 300,
    "AllowedOrigins": []
  }
}
```

| セクション | キー | 説明 |
|-----------|------|------|
| `ApiSecurity.ApiKey` | string | API認証キー。空=認証無効 |
| `ApiSecurity.RequestTimeoutSeconds` | int | リクエストタイムアウト秒数 |
| `ApiSecurity.AllowedOrigins` | string[] | CORS許可オリジン。空=全許可 |
| `voicevox.auto_discover` | bool | VOICEVOX自動検出 (port 50020-50100) |
| `defaults.*` | 各種 | 未指定パラメータのデフォルト値 |
