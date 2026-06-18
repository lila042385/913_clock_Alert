<!-- v1.00 20260618 00:40 -->
# Portable Alarm Clock ビルド手順書 (BUILD.md)

本ドキュメントは、C# / WPF / .NET 8 を用いた「Portable Alarm Clock」のビルド環境要件およびビルド手順について記載しています。

## 1. 開発・ビルド環境要件
本アプリをソースコードからビルドするには、以下の環境が必要です。

- **OS**: Windows 11 / Windows 10 (64-bit)
- **SDK**: .NET 8.0 SDK (またはそれ以降)
- **ビルドツール**: CLI (`dotnet` コマンド) または Visual Studio 2022

---

## 2. ディレクトリ構成
プロジェクトフォルダ `PortableAlarmClock` は以下の構成になっています。

```text
c:\Users\lila\.gemini\913_clock_Alert\
 ├ PortableAlarmClock.exe (発行された成果物)
 ├ README.md
 ├ BUILD.md
 └ PortableAlarmClock/ (ソースコードルート)
    ├ PortableAlarmClock.csproj (プロジェクト定義)
    ├ app.manifest (マニフェストファイル)
    ├ App.xaml / App.xaml.cs (エントリポイント・例外制御)
    ├ MainWindow.xaml / MainWindow.xaml.cs (メイン画面・並び替え)
    ├ AlarmEditDialog.xaml / AlarmEditDialog.xaml.cs (編集画面)
    ├ AlarmAlertWindow.xaml / AlarmAlertWindow.xaml.cs (鳴動画面)
    ├ Alarm.cs (アラームデータモデル)
    ├ AlarmManager.cs (データ管理・スケジューラ・スリープ制御)
    ├ AlarmSoundPlayer.cs (独自WAV電子音合成・再生)
    ├ SystemTrayIcon.cs (Win32 P/Invoke タスクトレイ制御)
    ├ ThemeManager.cs (ライト/ダークモード監視と切替)
    └ Logger.cs (スレッドセーフログ出力)
```

---

## 3. ビルド手順

### 開発用ビルド (Debug)
コード変更時の簡易ビルドと実行確認には、プロジェクトディレクトリで以下を実行します。

```bash
cd PortableAlarmClock
dotnet build
```

### 配布用ビルド (Release) - 自己完結型単一EXE
外部依存のない「ポータブル単一EXE」を発行するには、プロジェクトディレクトリで以下のコマンドを実行します。

```bash
cd PortableAlarmClock
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false /p:DebugType=None /p:DebugSymbols=false
```

#### 各パラメーターの意味：
- `-c Release`: リリースビルド
- `-r win-x64`: Windows 64-bit 向けランタイム指定
- `--self-contained true`: .NETランタイムをEXEに同梱（自己完結型）
- `/p:PublishSingleFile=true`: すべての依存関係を1つの単一EXEに統合
- `/p:IncludeNativeLibrariesForSelfExtract=true`: ネイティブDLLもEXEにパック
- `/p:PublishTrimmed=false`: WPFアセンブリのトリミングによるバグを防ぐため無効化
- `/p:DebugType=None / /p:DebugSymbols=false`: デバッグシンボルを除外してサイズ削減

---

## 4. ビルド成果物の出力先

上記の発行コマンドを実行すると、以下のパスに単一ファイルEXEが生成されます。

`PortableAlarmClock\bin\Release\net8.0-windows\win-x64\publish\PortableAlarmClock.exe`

配布する際は、この `PortableAlarmClock.exe` のみをコピーして利用します。
<!-- v1.00 20260618 00:40 -->
