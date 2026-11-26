# mini計算機（MiniCalculator）

以 .NET 10 / WPF 打造的 Windows 桌面計算機。提供標準與科學兩種模式，以及僅限標準模式的極小視窗；共用同一套運算式求值引擎，支援 12 款主題色 × 淺色／深色、歷史紀錄、單槽記憶體、實體鍵盤操作與視窗狀態記憶。

- 平台：Windows 11（build 22000 以上）
- 架構：x64、ARM64
- 授權：MIT License
- 版本：1.6.0

## 下載與執行

請至 [Releases](https://github.com/mark216tw/win-calculator-01/releases/latest) 下載對應架構的壓縮檔：

| 檔案 | 適用裝置 |
| --- | --- |
| `MiniCalculator-1.5.1-win-x64.zip` | Intel／AMD x64 電腦 |
| `MiniCalculator-1.5.1-win-arm64.zip` | Windows on ARM（ARM64）裝置 |

本專案提供**非 Self-Contained**（framework-dependent）版本，不包含 .NET 執行階段，也不提供安裝檔。使用前請先安裝 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)。

1. 解壓縮到下載資料夾或任何可寫入位置。
2. 執行 `MiniCalculator.exe`。
3. 若 Windows SmartScreen 顯示提示，請確認檔案來源後選擇「仍要執行」。

資料僅存放於本機，程式不會連線或收集任何個人資料。詳見 [隱私說明](docs/PRIVACY.md)。

## 功能

### 標準模式
- 數字、四則運算、括號與運算子優先序
- `%`、`1/x`、`x²`、`√`、`±`、隱含乘法（`2π`、`2(3+4)`）
- `C` / `CE` / `⌫`、即時結果預覽；`CE` 只清除結尾數字
- 單槽記憶體：`MC`、`MR`、`M+`、`M−`、`MS`
- 連按 `=` 重複上次運算
- 歷史紀錄（顯示時間、雙擊回填）

### 科學模式
- 角度單位 `DEG` / `RAD` / `GRAD`
- 三角與反三角、雙曲與反雙曲（`2nd` + `hyp` 組合）
- `ln`、`log`、`log₂`、`eˣ`、`10ˣ`、`xʸ`、`x²`、`x³`、`√`、`∛`
- `n!`、`mod`、`|x|`、`floor`、`ceil`、`round`
- 常數 `π`、`e`、隨機數 `rand()`、度分秒 `DMS`
- `F-E` 科學記號、括號
- 記憶體與歷史紀錄（與標準模式共用）

### 桌面整合
- 顯示模式：跟隨系統／淺色／深色
- 12 款主題色，皆支援淺色與深色
- 視窗置頂、視窗大小與位置記憶
- 極小視窗模式（僅標準模式）：精簡鍵盤、自動置頂、獨立記住視窗位置與大小
- 關閉視窗即完整結束程式、單一實例
- 記住上次的標準／科學模式與 `F-E` 狀態
- 設定頁可恢復預設值
- 複製格式化結果或原始值，並顯示複製提示
- 鍵盤焦點框、螢幕閱讀器名稱與常用快捷鍵

## 快速開始

1. 啟動後預設為標準模式，可直接用滑鼠或鍵盤輸入運算式。
2. 按 `Enter` 或 `=` 計算；結果會自動加入歷史紀錄。
3. 以視窗上方切換「標準／科學」。
4. 右上角可開啟歷史紀錄、設定與視窗置頂。
5. 設定頁底部提供版本、檔案版本、授權與隱私資訊。
6. 標準模式下可按右上角極小模式按鈕或 `Ctrl+M`，縮成精簡視窗並自動置頂。

完整操作說明請見 [使用指南](docs/USER_GUIDE.md)。

## 鍵盤操作

| 按鍵 | 功能 |
| --- | --- |
| `0`–`9`、`.` | 輸入數字 |
| `+` `-` `*` `/` `^` `%` `(` `)` | 運算子與括號 |
| `Enter` | 計算（`=`）；連按可重複上次運算 |
| `Esc` | 全部清除（`C`） |
| `Backspace` | 刪除前一個字元（`⌫`） |
| `Ctrl+H` | 開啟歷史紀錄 |
| `Ctrl+,` | 開啟設定 |
| `Ctrl+1` / `Ctrl+2` | 切換標準／科學模式 |
| `Ctrl+M` | 切換極小／完整視窗（僅標準模式） |
| `Ctrl+Shift+C` | 複製格式化結果 |
| 方向鍵 / `Home` / `End` | 移動輸入游標 |

也可直接輸入函數名稱，例如 `sin(30)`、`ln(2)`、`sqrt(9)`、`10 mod 3`、`5!`、`π`、`e`、`rand()`。

## 運算式語意

- `%` 為「除以 100」：`200*10%` = 20。
- 支援隱含乘法：`2π`、`2(3+4)`、`3sin(30)`、`2e`。
- 運算式若缺少右括號，計算時會自動補齊。
- `dms(x)` 將十進位度轉為度分秒數值（`d.mmss`）；`deg(x)` 為反向轉換。
- `F-E` 切換結果的科學記號顯示，狀態會保存。
- 連按 `=` 會以最右側頂層運算子重複運算（如 `2+3=` 得 5，再按 `=` 得 8）。
- 科學模式的角度單位會影響三角與反三角函式；雙曲函式不受角度單位影響。

## 資料位置

| 資料 | 路徑 |
| --- | --- |
| 設定 | `%AppData%\MiniCalculator\settings.json` |
| 歷史紀錄 | `%AppData%\MiniCalculator\history.json`（最多 100 筆） |
| 日誌 | `%AppData%\MiniCalculator\logs\app-yyyyMMdd.log`（僅警告與錯誤） |

## 專案結構

```text
src/MiniCalculator/
  Models/       設定、歷史、列舉與求值結果模型
  Services/     求值引擎、設定／歷史／主題／單一實例服務
  ViewModels/   MVVM ViewModel
  Views/        MainWindow、標準／科學／極小檢視、歷史／設定視窗
  Helpers/      格式化、在地化、視窗位置工具
  Strings/      zh-Hant 資源（.resx）
  Styles/       Fluent 風格樣式與 12×2 主題字典
  Assets/       App 圖示
tests/MiniCalculator.Tests/   引擎、格式化與 ViewModel 的 xUnit 測試
docs/                         使用、架構、設計、建置與隱私文件
```

## 建置與測試

需求：Windows 11、.NET 10 SDK。

```powershell
dotnet build MiniCalculator.sln -c Debug
dotnet test MiniCalculator.sln -c Debug
dotnet format MiniCalculator.sln --verify-no-changes
dotnet run --project src/MiniCalculator/MiniCalculator.csproj
```

發佈非 Self-Contained（framework-dependent）版本，與 GitHub Release 相同：

```powershell
dotnet publish src/MiniCalculator/MiniCalculator.csproj -c Release -r win-x64 --self-contained false
dotnet publish src/MiniCalculator/MiniCalculator.csproj -c Release -r win-arm64 --self-contained false
```

需要自包含版本時，可將 `--self-contained false` 改為 `true`。完整流程與版本規範請見 [建置與發佈](docs/BUILD_AND_RELEASE.md)。

## 文件

- [文件索引](docs/README.md)
- [使用指南](docs/USER_GUIDE.md)
- [系統架構與技術文件](docs/ARCHITECTURE.md)
- [系統設計文件](docs/DESIGN.md)
- [建置與發佈](docs/BUILD_AND_RELEASE.md)
- [隱私說明](docs/PRIVACY.md)
- [開發紀錄](docs/DEV_LOG.md)
- [貢獻指南](CONTRIBUTING.md)
- [安全性政策](SECURITY.md)
- [版本異動](CHANGELOG.md)
- [發行說明](RELEASE_NOTES.md)

## 授權

本專案採用 [MIT License](LICENSE)，Copyright © 2026 mark216tw。
