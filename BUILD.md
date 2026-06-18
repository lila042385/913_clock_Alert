<!-- v1.01 20260618 23:59 -->
# Portable Alarm Clock ビルド手順書 (BUILD.md)

本ドキュメントは、C# / WPF / .NET Framework 4.8 を用いた「Portable Alarm Clock」のビルド環境要件およびビルド手順について記載しています。

## 1. 開発・ビルド環境要件
本アプリをソースコードからビルドするには、以下の環境が必要です。

- **OS**: Windows 11 / Windows 10
- **SDK**: .NET Framework 4.8 開発環境 (または .NET 8.0 SDK 以降の CLI ツールチェーン)
- **ビルドツール**: CLI (`dotnet` コマンド) または Visual Studio 2022

---

## 2. ディレクトリ構成
プロジェクトフォルダ `PortableAlarmClock` は以下の構成になっています。

```text
c:\Users\lila\.gemini\913_clock_Alert\
 ├ PortableAlarmClock.exe (発行された軽量成果物 - 約 540 KB)
 ├ README.md
 ├ BUILD.md
 └ PortableAlarmClock/ (ソースコードルート)
    ├ PortableAlarmClock.csproj (プロジェクト定義)
    ├ FodyWeavers.xml / FodyWeavers.xsd (Fody/CosturaのDLL埋め込み定義)
    ├ app.manifest (マニフェストファイル)
    ├ App.xaml / App.xaml.cs (エントリポイント・例外制御)
    ├ MainWindow.xaml / MainWindow.xaml.cs (メイン画面・並び替え)
    ...
```

---

## 3. ビルド手順

### 開発用ビルド (Debug)
コード変更時の簡易ビルドと実行確認には、プロジェクトディレクトリで以下を実行します。

```bash
cd PortableAlarmClock
dotnet build
```

### 配布用ビルド (Release) - 単一超軽量EXE (Fody/Costura自動埋め込み)
外部依存のない「ポータブル単一EXE」を極小サイズ（約540KB）でビルドするには、プロジェクトディレクトリで以下のコマンドを実行します。

```bash
cd PortableAlarmClock
dotnet build -c Release
```

Fody/Costura パッケージの働きにより、ビルド時に `System.Text.Json` などのすべての依存DLLが `PortableAlarmClock.exe` の内部リソースとして自動的に埋め込まれます。そのため、発行コマンド (`dotnet publish`) を複雑な引数で叩く必要がなく、通常の Release ビルドを行うだけで単一EXEが完成します。

---

## 4. ビルド成果物の出力先

上記のビルドコマンドを実行すると、以下のパスに単一ファイルEXEが生成されます。

`PortableAlarmClock\bin\Release\net48\PortableAlarmClock.exe`

配布する際は、この `PortableAlarmClock.exe` のみをコピーして利用します。
<!-- v1.01 20260618 23:59 -->
