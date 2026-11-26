# mini計算機 1.5.1

Windows 11 上的輕量計算機，提供標準與科學兩種模式。本版本提供 x64 與 ARM64 的非 Self-Contained（framework-dependent）壓縮檔，不含安裝程式。

## 下載

| 檔案 | 適用裝置 |
| --- | --- |
| `MiniCalculator-1.5.1-win-x64.zip` | Intel／AMD x64 電腦 |
| `MiniCalculator-1.5.1-win-arm64.zip` | Windows on ARM（ARM64）裝置 |

解壓縮後直接執行 `MiniCalculator.exe`。由於採用非 Self-Contained 發佈，請先安裝 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)。

## 本次更新

- 啟動更快：只建立目前使用的計算機模式，並延後非必要的主題色票與歷史資料讀取。
- 支援隱含乘法：`2π`、`2(3+4)`、`3sin(30)`。
- 連按 `=` 可重複上次運算；記住上次的模式與科學記號顯示。
- 歷史紀錄顯示時間，雙擊即可回填。
- 新增「複製原始值」、複製提示與「恢復預設值」。
- 修正按 `=` 後編輯、`±`、`mod`、`CE` 與記憶體提示的行為。
- 補上鍵盤焦點框、螢幕閱讀器名稱與常用快捷鍵。
- 新增 x64 與 ARM64 的非自包含發佈產物與完整專案文件。

## 這次有什麼

- **標準模式**：日常四則運算，支援括號與運算子優先序、百分比、倒數、平方、根號與正負號。
- **科學模式**：三角／反三角、雙曲、對數、指數、次方與根、階乘、`mod`、常數 `π/e`、隨機數、度分秒與科學記號。
- **看得懂的計算過程**：上方可直接編輯運算式，下方即時顯示結果；按 `=` 後自動存入歷史紀錄。
- **記憶體**：`MC`、`MR`、`M+`、`M−`、`MS`。
- **鍵盤操作**：數字與運算子可直接輸入，`Enter` 計算、`Esc` 清除、`Backspace` 刪除；`Ctrl+H` 歷史、`Ctrl+,` 設定、`Ctrl+1/2` 切換模式、`Ctrl+Shift+C` 複製結果。
- **外觀**：跟隨系統或自選淺色／深色，並提供 12 款主題色。
- **設定整合**：版本、授權與隱私等關於資訊集中於設定頁底部。
- **順手的小功能**：視窗置頂、記住視窗位置與大小、避免重複開啟。

## 快速開始

1. 解壓縮後執行 `MiniCalculator.exe`。
2. 在上方切換「標準」或「科學」。
3. 右上角可開啟歷史與設定；關於資訊位於設定頁底部。

## 資料存放位置

- 設定：`%AppData%\MiniCalculator\settings.json`
- 歷史：`%AppData%\MiniCalculator\history.json`
- 日誌：`%AppData%\MiniCalculator\logs\`（僅警告與錯誤）

程式僅在本機執行，不會收集或傳送任何資料。詳見[隱私說明](docs/PRIVACY.md)。

## 系統需求

- Windows 11（build 22000 以上）
- x64 或 ARM64 架構
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## 授權

MIT License，Copyright © 2026 mark216tw。詳見 [LICENSE](LICENSE)。
