# 建置與發佈

本文件說明 mini計算機的建置、測試、發佈與 GitHub Release 流程。對應版本 1.5.1。

## 1. 環境需求

- Windows 11（build 22000 以上）
- .NET 10 SDK（開發與建置）
- PowerShell 5.1 以上
- 發佈 ARM64 版本不需要 ARM64 硬體，可由 x64 主機交叉發佈

確認環境：

```powershell
dotnet --info
```

## 2. 還原與建置

```powershell
dotnet restore MiniCalculator.sln
dotnet build MiniCalculator.sln -c Debug
```

開發與測試一律使用 `Debug`。不要自動建置或發佈 `Release`，僅在明確要求時執行。

> 若建置出現檔案鎖定錯誤，請先關閉正在執行的 `MiniCalculator.exe`。

## 3. 測試與格式檢查

```powershell
dotnet test MiniCalculator.sln -c Debug
dotnet format MiniCalculator.sln --verify-no-changes
```

測試涵蓋求值引擎、數字格式化、CalculatorViewModel 與 ShellViewModel。

## 4. 本機執行

```powershell
dotnet run --project src/MiniCalculator/MiniCalculator.csproj
```

## 5. 發佈非 Self-Contained 版本

Release 產物為 framework-dependent（非 Self-Contained），使用者需安裝 .NET 10 Desktop Runtime。

### 5.1 x64

```powershell
dotnet publish src/MiniCalculator/MiniCalculator.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o artifacts/publish/win-x64
```

### 5.2 ARM64

```powershell
dotnet publish src/MiniCalculator/MiniCalculator.csproj `
  -c Release `
  -r win-arm64 `
  --self-contained false `
  -o artifacts/publish/win-arm64
```

### 5.3 架構設定

`MiniCalculator.csproj` 會依 `RuntimeIdentifier` 切換 `PlatformTarget`：

```xml
<RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
<PlatformTarget Condition="'$(RuntimeIdentifier)' == '' or '$(RuntimeIdentifier)' == 'win-x64'">x64</PlatformTarget>
<PlatformTarget Condition="'$(RuntimeIdentifier)' == 'win-arm64'">arm64</PlatformTarget>
```

若未設定而固定為 x64，ARM64 發佈會出現：

```text
error NETSDK1032: RuntimeIdentifier 平台 'win-arm64' 必須與 PlatformTarget 'x64' 相容。
```

## 6. 建立壓縮檔

```powershell
$version = "1.5.1"
$assets = "artifacts/assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null

Compress-Archive -Path "artifacts/publish/win-x64/*" `
  -DestinationPath "$assets/MiniCalculator-$version-win-x64.zip" -Force

Compress-Archive -Path "artifacts/publish/win-arm64/*" `
  -DestinationPath "$assets/MiniCalculator-$version-win-arm64.zip" -Force
```

檔名格式：

```text
MiniCalculator-<版本>-win-x64.zip
MiniCalculator-<版本>-win-arm64.zip
```

## 7. 驗證發佈產物

### 7.1 內容檢查

解壓縮後應包含：

```text
MiniCalculator.exe
MiniCalculator.dll
MiniCalculator.runtimeconfig.json
MiniCalculator.deps.json
MiniCalculator.pdb
<相依組件>
```

非 Self-Contained 版本**不會**包含 `hostfxr.dll`、`coreclr.dll` 等執行階段檔案。

### 7.2 執行檢查

在已安裝 .NET 10 Desktop Runtime 的機器上：

1. 解壓縮。
2. 執行 `MiniCalculator.exe`。
3. 確認可計算、切換模式、開啟設定與歷史。
4. 確認版本顯示為 1.5.1。

### 7.3 架構檢查

可用 PowerShell 讀取 PE 標頭確認執行檔架構：

```powershell
function Get-PeMachine($path) {
  $bytes = [System.IO.File]::ReadAllBytes($path)
  $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
  $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
  switch ($machine) {
    0x8664 { "x64" }
    0xAA64 { "ARM64" }
    0x014C { "x86" }
    default { "0x{0:X4}" -f $machine }
  }
}

Get-PeMachine "artifacts/publish/win-x64/MiniCalculator.exe"
Get-PeMachine "artifacts/publish/win-arm64/MiniCalculator.exe"
```

預期輸出：

```text
x64
ARM64
```

## 8. 版本管理

版本集中於 `Directory.Build.props`：

```xml
<Version>1.5.1</Version>
<AssemblyVersion>1.5.0.0</AssemblyVersion>
<FileVersion>1.5.0.0</FileVersion>
```

規則：

- `Version` 採 `主.次.修`。
- 每次功能變更遞增版本。
- `AssemblyVersion` 只在主次版變更時調整。
- 版本變更時同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md` 與 `docs/DEV_LOG.md`。

## 9. GitHub Release 流程

### 9.1 前置檢查

```powershell
git status
dotnet build MiniCalculator.sln -c Debug
dotnet test MiniCalculator.sln -c Debug
```

確認：

- 工作目錄沒有未預期變更。
- 建置與測試通過。
- `artifacts/` 未被加入版本控制。
- Release 產物架構與檔名正確。

### 9.2 建立標籤與 Release

```powershell
git tag v1.5.1
git push origin v1.5.1

gh release create v1.5.1 `
  artifacts/assets/MiniCalculator-1.5.1-win-x64.zip `
  artifacts/assets/MiniCalculator-1.5.1-win-arm64.zip `
  --title "mini計算機 1.5.1" `
  --notes-file RELEASE_NOTES.md
```

若 Release 已存在，可改用：

```powershell
gh release upload v1.5.1 `
  artifacts/assets/MiniCalculator-1.5.1-win-x64.zip `
  artifacts/assets/MiniCalculator-1.5.1-win-arm64.zip `
  --clobber
```

### 9.3 Release 內容檢查

- 標題與版本一致。
- 兩個 zip 皆已上傳。
- Release Notes 包含系統需求、下載表與隱私說明。
- 不包含安裝檔或 Self-Contained 產物。
- 標記為 Latest release。

## 10. 發佈檢查清單

- [ ] `Directory.Build.props` 版本正確
- [ ] `CHANGELOG.md` 已更新
- [ ] `RELEASE_NOTES.md` 已更新
- [ ] `docs/DEV_LOG.md` 已更新
- [ ] `dotnet build` 通過
- [ ] `dotnet test` 通過
- [ ] `dotnet format --verify-no-changes` 通過
- [ ] x64 與 ARM64 發佈成功
- [ ] PE 架構正確
- [ ] 兩個 zip 可解壓並啟動
- [ ] GitHub Release 已上傳且標記 Latest
- [ ] 未上傳 `bin/`、`obj/`、`artifacts/` 或憑證

## 11. 常見問題

### NETSDK1032

`RuntimeIdentifier` 與 `PlatformTarget` 不相容。確認 csproj 的條件設定，或發佈時不要覆寫成錯誤的 `PlatformTarget`。

### 檔案被鎖定

關閉執行中的 `MiniCalculator.exe`，或重新開機後再建置。

### ARM64 版本無法啟動

確認裝置為 Windows on ARM，且安裝的是 ARM64 版 .NET 10 Desktop Runtime。

### x64 版本在 ARM64 裝置上執行

Windows 11 ARM64 可模擬執行 x64 應用程式，但建議使用 ARM64 原生版本以獲得較佳效能。

## 12. 相關文件

- [系統架構與技術文件](ARCHITECTURE.md)
- [系統設計文件](DESIGN.md)
- [使用指南](USER_GUIDE.md)
- [隱私說明](PRIVACY.md)
- [開發紀錄](DEV_LOG.md)
