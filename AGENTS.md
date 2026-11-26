# Windows .NET 10 / C# 桌面應用開發規範（簡化版）

本檔是簡單型 Windows 桌面應用的共用開發筆記。開發新專案時複製到專案根目錄，AI Agent 會自動讀取；專案專屬規則寫在文末。

---

## 1. 技術基線

| 項目 | 版本 |
| --- | --- |
| 語言 | C#（含 nullable reference types、file-scoped namespace） |
| 執行環境 | .NET 10（`net10.0-windows10.0.19041.0`） |
| UI | WinUI 3（建議，Windows App SDK）或 WPF，二選一 + MVVM（`Microsoft.Extensions.Hosting` 啟動） |
| 平台 | Windows 11（build 22000）以上 |
| 架構 | x64（必要時提供 AnyCPU；不支援 ARM 需於補充說明） |
| 建置 | .NET SDK 風格專案（`*.csproj`）+ Central Package Management（`Directory.Packages.props`） |

- UI 框架於每個專案二選一：優先 WinUI 3（`Microsoft.WindowsAppSDK`），需要時可選 WPF；選 WPF 需在「專案專屬補充」記錄理由，且不要在同一專案混用 WinUI 3／WPF／WinForms 控制項。
- MVVM 使用 `CommunityToolkit.Mvvm`（`ObservableObject`、`RelayCommand`）；不要手寫 `INotifyPropertyChanged`。
- 套件版本一律集中寫在 `Directory.Packages.props`，不要在 `*.csproj` 寫死版本。
- 不要任意升級 .NET SDK／套件；升級需在 `docs/DEV_LOG.md` 說明原因。
- 啟用 `Nullable` 與 `TreatWarningsAsErrors`；不關閉警告。

## 2. 專案結構

```
src/<AppName>/                 # 應用程式專案
  Views/                       # XAML 頁面／視窗與使用者控制項
  ViewModels/                  # ViewModel（MVVM）
  Models/                      # 資料模型
  Services/                    # 商業邏輯、I/O、外部整合
  Helpers/                     # 轉換器、擴充方法、共用工具
  Assets/                      # 圖示、圖片、字型
  Strings/                     # 多語系資源（WinUI 3 用 .resw；WPF 用 .resx），預設 zh-Hant
  Styles/                      # 樣式與主題資源字典（.xaml）
  App.xaml / App.xaml.cs
  Package.appxmanifest         # MSIX 封裝設定（僅 WinUI 3 封裝模式需要）
tests/<AppName>.Tests/         # 單元測試（選用，建議 xUnit）
<AppName>.sln
Directory.Build.props          # 共用編譯設定
Directory.Packages.props       # 套件版本集中管理
README.md
CHANGELOG.md
RELEASE_NOTES.md
docs/DEV_LOG.md                # 開發過程紀錄（必要）
```

- 命名空間用反向網域命名（例：`Com.Example.AppName`）。
- 使用者可見文案放 `Strings/**/*.resw`（WPF 用 `*.resx`），透過 `x:Uid` 或 `ResourceLoader` 取得，不要寫死在 XAML／程式碼；預設繁體中文。
- WinUI 3 的頁面／視窗放 `Views/`（`Page`／`Window`）；WPF 的 `Window`／`UserControl` 亦放 `Views/`，入口視窗為 `MainWindow.xaml`。
- 一個檔案一個公開型別，檔名與型別同名。

## 3. 程式碼規範

- 4 空格縮排；類名／方法用 PascalCase、區域變數／參數用 camelCase、常數用 PascalCase、私有欄位用 `_camelCase`。
- 非同步方法以 `Async` 結尾並回傳 `Task`／`Task<T>`；函式庫層加 `ConfigureAwait(false)`，UI 層不加。
- 不在 UI 執行緒做 I/O；長工作放 `Task.Run` 或 `async` 流程並回報進度。
- 依賴注入用 `Microsoft.Extensions.DependencyInjection`，在 `App.xaml.cs` 註冊服務；不要在 View 直接 new 服務。
- 設定用 `IOptions` + `appsettings.json`（或 `Microsoft.Extensions.Configuration`）；使用者設定用 `Properties.Settings`（WPF）或 JSON 檔（`%AppData%/<AppName>`，WinUI 3 建議用 JSON）。
- 資料儲存：結構化資料用 SQLite + EF Core，其餘用檔案／JSON。
- 例外要記錄並處理，不要吞例外；記錄用 `Microsoft.Extensions.Logging`，正式版不輸出到主控台。
- 不要引入未在 `Directory.Packages.props` 宣告的依賴。

## 4. 共用界面

- 以 Fluent 風格控制項搭配共用的 `Style`，樣式放 `Styles/`；WinUI 3 用原生 `NavigationView`、`ListView`、`Card` 風格 `Border`、`ToggleSwitch`，WPF 以對應控制項實作（見下方對照）。
- 間距用 4 的倍數（WinUI 單位為 DIP）；圓角 8。
- 顏色與筆刷定義在 `Styles/Themes/` 的資源字典，不要在 XAML 寫死 `#FF...`；WinUI 3 以 `ThemeResource`（跟隨主題）或 `StaticResource` 引用，WPF 以 `DynamicResource` 引用。
- 字型用共用的 `TextBlock` 樣式與 `FontSize` 資源，不要逐處指定。
- 空狀態、載入中、錯誤都要有畫面；刪除等破壞性操作需二次確認（`ContentDialog`／WPF `MessageBox` 或自訂對話框）。
- 可點擊元素要有 `AutomationProperties.Name`，圖示按鈕需有 `ToolTipService.ToolTip`（WPF 亦支援）。
- 視窗要有合理的 `MinWidth`／`MinHeight`，關閉前若有未儲存變更需提示。
- WinUI 3／WPF 對照：
  - 導覽：`NavigationView` → WPF 無原生對應，改用 `ListBox` + `ContentControl` 或第三方元件（需先在 `Directory.Packages.props` 宣告）。
  - 提示訊息：`InfoBar` → WPF `StatusBar` 或自訂 `Border` + `TextBlock`。
  - 載入中：`ProgressRing` → WPF `ProgressBar`（`IsIndeterminate="True"`）。
  - 主題切換：`ElementTheme` → WPF 無對應，改切換 `ResourceDictionary`。
  - 卡片與開關：`Border`（圓角 8）／`ToggleSwitch` → WPF `Border`（`CornerRadius="8"`）／`ToggleButton`。

## 5. 必要與選用功能

### 必要（每個專案都要實作）

- **App 圖示**：建立完整的應用程式圖示資產，套用於執行檔、視窗、工作列、開始功能表及 MSIX 封裝。WinUI 3 放 `Assets/` 並於 `Package.appxmanifest` 宣告，必要時以 `AppWindow.SetIcon` 設定視窗圖示；WPF 準備 `.ico`，於 `*.csproj` 設 `<ApplicationIcon>` 並設定 `Window.Icon`。
- **視窗狀態**：關閉時保存視窗大小、位置及最大化狀態，啟動時還原；還原前需檢查目前螢幕的有效工作區域，避免視窗出現在螢幕外。WinUI 3 用 `AppWindow` 的位置／大小搭配 `DisplayArea` 檢查；WPF 用 `Window.Left`／`Top`／`Width`／`Height`／`WindowState` 搭配螢幕工作區檢查。

### 選用（需要時再實作）

- **顯示模式**：提供跟隨系統、淺色、深色三種模式並保存使用者選擇；WinUI 3 設定 `Window.Content` 的 `ElementTheme`（`Application.Current.RequestedTheme` 只影響初始值），跟隨系統時需同步系統模式變化；WPF 以切換 `ResourceDictionary` 實作。
- **主題設定**：提供黃色護眼、經典藍、森林綠、暮色紫、中性灰五款主題。主題設定與顯示模式彼此獨立，每款主題都需同時支援淺色與深色模式，並即時套用（WinUI 3 用 `ThemeResource`，WPF 用 `DynamicResource`）。
- **設定頁**：`IOptions` + 設定檔，或 `NavigationView`（WPF 用對應導覽）內的 Settings 頁。
- **About**：顯示 `Version`、`FileVersion`、授權與隱私說明（由組件屬性讀取）。
- **通知**：使用 Windows App SDK 的 `AppNotificationBuilder`／`AppNotificationManager`（WinUI 3／WPF 皆可用）。
- **單一實例**：用 `Mutex` 或具名 Pipe 避免多開，第二次啟動時帶回原視窗。
- **系統匣**：WinUI 3 無原生支援，需要時透過 `System.Windows.Forms.NotifyIcon` 或 `Shell_NotifyIcon` 互通並提供結束選單；WPF 可直接用 `System.Windows.Forms.NotifyIcon`。
- **標題列**：WinUI 3 可用 `AppWindowTitleBar` 自訂標題列與按鈕顏色，或 `ExtendsContentIntoTitleBar` 自繪；WPF 用 `WindowChrome`。

## 6. 建置與版本

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
  <TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

- 平台最低為 Windows 11：`TargetPlatformMinVersion` 設 `10.0.22000.0`；TFM 維持 `net10.0-windows10.0.19041.0`（WinUI 3 與 WPF 皆可），需要 Win11 專屬 API 時才提高 TFM。
- 開發與測試一律使用 `Debug`；不要自動建置或發佈 `Release`，只有使用者明確要求時才執行。
- 每次功能變更完成後，自動執行 `dotnet build` 確認可編譯。
- 版本放在 `Directory.Build.props` 的 `Version`／`AssemblyVersion`／`FileVersion`；`Version` 採 `主.次.修`。
- 每次功能變更遞增版本；`AssemblyVersion` 主次版變更時才調整。
- 指令：`dotnet build`、`dotnet test`、`dotnet format --verify-no-changes`。
- 發佈（僅限明確要求）：WinUI 3 未封裝模式用 `dotnet publish -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None`；WPF 用 `dotnet publish -c Release -r win-x64 --self-contained true`；需要安裝檔時再使用 MSIX 打包。

## 7. 開發過程紀錄（docs/DEV_LOG.md）

每次開發工作都要更新 `docs/DEV_LOG.md`，不要事後補寫。內容包含：

1. **本次目標**：要做什麼、為什麼。
2. **變更內容**：改了哪些檔案／功能。
3. **遇到的問題與解法**：錯誤訊息、嘗試過的方法、最後怎麼解。
4. **待辦／已知限制**：尚未完成或未驗證的部分。
5. **日期與版本**：對應的版本號。

格式建議（最新紀錄放最上面）：

```markdown
## 2026-10-30 - v1.1.0

### 目標
新增深色模式切換。

### 變更
- `Styles/Themes/`：加入深色主題資源字典
- `Views/SettingsPage.xaml`：新增主題切換項目

### 問題與解法
- 主題切換後部分樣式未更新 → 改用 `ThemeResource` 並重設 `ElementTheme`

### 待辦
- 尚未實測 Windows 11
```

- 只記重點，不貼整段程式碼。
- 不要記錄 API Key、密碼等敏感資訊。

## 8. 文件與禁止事項

- `README.md` 寫功能與建置方式；`CHANGELOG.md` 記錄版本異動；`RELEASE_NOTES.md` 寫給使用者看。
- 不要提交 `bin/`、`obj/`、`.vs/`、`*.user`、`*.suo`、發佈輸出與憑證檔。
- 不要留 `TODO` 當完成功能、不要留測試假資料或除錯輸出（`Console.WriteLine`／`Debug.WriteLine` 需移除）。
- 不要更動無關檔案；未經確認不要改組件名稱、簽章或目標框架版本。
- 不要把 API Key、密碼、金鑰寫進原始碼或版本控制；機密用 User Secrets、環境變數或 Windows 認證管理員。

---

## 專案專屬補充

- 專案名稱：mini計算機（MiniCalculator）
- 命名空間／組件名稱：`Com.MiniCalculator` / `MiniCalculator`
- UI 框架：WPF（本機未安裝 WinUI 3 專案範本；WPF 範本現成、建置穩定，選 WPF）
- 最低平台：Windows 11（build 22000）
- 架構：x64、ARM64；Release 提供非 Self-Contained（framework-dependent）zip，不提供安裝檔
- 特殊需求：標準與科學模式共用運算式求值引擎；12 主題色 × 淺/深；關閉視窗即完整結束程式；單一實例
- 已知限制：
  - TFM 採 `net10.0-windows10.0.22000.0`：`19041` 搭配 `TargetPlatformMinVersion=22000` 會觸發 NETSDK1135，故以 TFM 直接表達 Win11 最低需求。
  - ComboBox 已改為主題化樣式；CheckBox 的勾選方塊仍使用系統外觀。
  - `CE` 只處理結尾數字，科學記號結尾會直接全部清除。
  - 連按 `=` 僅支援最右側頂層數值運算，函式與後置運算不重複。
  - 尚未建立 MSIX 封裝。
