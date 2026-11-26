# 開發紀錄

最新紀錄放最上面。每次開發工作都要更新，不要事後補寫。

---

## 2026-11-26 - v1.5.1（文件與 x64/ARM64 發佈）

### 目標
補齊專案文件（專案說明、使用指南、架構、設計、建置發佈、隱私與授權），建立公開 GitHub 儲存庫，並提供 x64 與 ARM64 的非 Self-Contained 壓縮檔 Release，不提供安裝檔。

### 變更
- 根目錄：改寫 `README.md`，更新 `LICENSE`（著作權人 mark216tw）、`CHANGELOG.md`、`RELEASE_NOTES.md`；新增 `CONTRIBUTING.md`、`SECURITY.md`。
- `docs/`：新增 `README.md`（索引）、`USER_GUIDE.md`、`ARCHITECTURE.md`、`DESIGN.md`、`BUILD_AND_RELEASE.md`、`PRIVACY.md`。
- `src/MiniCalculator/MiniCalculator.csproj`：加入 `RuntimeIdentifiers`，`PlatformTarget` 依 `RuntimeIdentifier` 切換 x64／ARM64。
- 發佈產物：`MiniCalculator-1.5.1-win-x64.zip`、`MiniCalculator-1.5.1-win-arm64.zip`（framework-dependent）。
- 建立公開儲存庫 `mark216tw/win-calculator-01`，推送 `main` 與 `v1.5.1` 標籤，建立 GitHub Release。

### 問題與解法
- ARM64 發佈初次執行出現 `NETSDK1032`：`PlatformTarget=x64` 與 `win-arm64` 不相容。改以條件式 `PlatformTarget`，未指定 RID 時維持 x64，`win-arm64` 時使用 `arm64`。
- Debug 建置曾因既有 `MiniCalculator.exe` 執行中而檔案鎖定（MSB3021／MSB3027）；關閉程序後重建即恢復。
- 非 Self-Contained 產物不含 `hostfxr.dll`／`coreclr.dll`，需在 Release 說明標明 .NET 10 Desktop Runtime 需求。

### 待辦／已知限制
- 尚未建立 MSIX／MSI 安裝封裝（依需求不提供安裝檔）。
- 尚未進行程式碼簽章，SmartScreen 可能顯示提示。
- 12 主題 × 淺／深與 ARM64 實機仍建議持續目視驗證。

---

## 2026-11-26 - v1.5.1

### 目標
縮短開發時 Debug 啟動到視窗可互動的時間，優先處理啟動關鍵路徑上的 UI 建構、Host 初始化與不必要的檔案 I/O。

### 變更
- `Views/MainWindow.xaml`、`Views/MainWindow.xaml.cs`：標準／科學面板改由 `ContentControl` 承載，啟動只建立目前模式，另一個模式第一次切換時才建立並快取；模式切換改由 `ShellViewModel.PropertyChanged` 套用。
- `Views/MainWindow.xaml.cs`：啟動焦點延後到第一次畫面呈現後（`DispatcherPriority.Input`）；保留滑鼠點選模式鈕後自動回到輸入框。
- `App.xaml.cs`：`Host.CreateApplicationBuilder()` 改為 `Host.CreateEmptyApplicationBuilder(...)`，不再載入未使用的預設組態來源。
- `App.xaml`、`Views/SettingsWindow.xaml`：`ThemeSwatches.xaml` 改為只在設定視窗載入。
- `Services/IHistoryService.cs`、`Services/HistoryService.cs`、`Views/HistoryWindow.xaml.cs`：歷史紀錄改為第一次開啟歷史視窗或第一次新增時才讀檔。
- `Services/JsonFileStore.cs`、`Services/SettingsService.cs`、`Services/HistoryService.cs`、`Services/FileLoggerProvider.cs`：資料夾改為實際寫檔時才建立，移除啟動時不必要的 `Directory.CreateDirectory`。
- `Directory.Build.props`：版本 1.5.1。

### 問題與解法
- 拔除模式鈕 `Checked` 事件後，滑鼠點選模式不會自動把焦點放回輸入框；補回 `Checked` 並以延後焦點處理，鍵盤快捷鍵仍走 `SelectMode`。
- `ContentControl` 預設內容對齊不是 Stretch，面板不會填滿可用空間；補上 `HorizontalContentAlignment`／`VerticalContentAlignment="Stretch"`。
- 啟動量測以 `Process.WaitForInputIdle()` 量測 Debug exe，7 次中位數由約 620ms 降至約 500–520ms；數值受系統快取與防毒掃描影響，僅供趨勢參考。

### 待辦／已知限制
- 第一次切換到另一個模式仍需即時建立面板，會比後續切換稍慢。
- 未處理 Release／`PublishReadyToRun` 冷啟動，本次目標為 `dotnet run` / Debug。
- 啟動量測非正式效能測試，尚未建立自動化效能基準。

---

## 2026-11-26 - v1.5.0

### 目標
依架構與使用流程檢視，執行一批「改動小、效益高」的介面與功能優化：修正 `=` 後的編輯狀態、強化複製與歷史、補上隱含乘法與連按 `=`、記住常用狀態，並改善無障礙與鍵盤操作。

### 變更
- `CalculatorViewModel`：
  - 手動編輯或 `⌫` 後重設 `_justEvaluated`，避免下一個數字鍵覆蓋整段運算式。
  - `±` 在 `=` 後直接對結果變號；`mod` 不再被判定為「值開頭」。
  - `CE` 改為移除結尾數字；剛按 `=` 或無結尾數字時退回 `C`。
  - 新增連按 `=` 重複上次運算（取最右側頂層運算子，支援 `+ − × ÷ ^ mod`）。
  - 新增 `CopyRawResultCommand`、`StatusMessage` 與 2 秒提示；`MR` 空改為狀態提示，不再標紅。
  - 記住 `F-E` 狀態並同步 `AppSettings`。
- `CalculatorEngine`：新增隱含乘法（`2π`、`2(3+4)`、`3sin(30)`、`2e`）；排除 `.` 開頭與 `2e3` 這類潛在科學記號誤判。
- `ShellViewModel`：記住上次標準/科學模式，並在設定重設時同步選取狀態。
- `SettingsViewModel` / `SettingsWindow`：新增「恢復預設值」（二次確認）與 `Esc` 關閉。
- `HistoryWindow` / `Controls.xaml`：歷史顯示時間、雙擊回填、`Enter`/`Esc` 操作、主題化清單項目（移除系統藍底）。
- `StandardView` / `ScientificView`：輸入框提示、狀態訊息、複製原始值、結果 ToolTip、固定錯誤字級、符號按鈕的 `AutomationProperties.Name`。
- `MainWindow`：置頂按鈕 tooltip 依狀態切換；新增 `Ctrl+H`、`Ctrl+,`、`Ctrl+1/2`、`Ctrl+Shift+C`。
- `Controls.xaml`：新增共用 `KeyFocusVisual`，讓鍵盤 Tab 有可見焦點框。
- `Strings/Resources.resx`：新增複製、重設與符號按鈕無障礙字串。
- `Directory.Build.props`：版本 1.5.0。

### 問題與解法
- `OnExpressionChanged` 重置 `_justEvaluated` 會影響按鈕插入流程，因此 `Insert` 先讀取旗標再修改 `Expression`，並在流程末端明確清除。
- 連按 `=` 需避免把科學記號的 `+/-` 當成二元運算子；`IsUnaryPosition` 對大寫 `E` 後的符號視為指數符號。
- 重複運算的左運算元若為負數，直接串成 `-4 ^ 2` 會被解析為 `-(4^2)`；改以括號包住左運算元。
- 隱含乘法不能把 `2.5.3` 或 `2e3` 變成有效值，因此只接受數字、`(`、`π`、字母作為開頭，且遇到 `e` 後接數字時不啟用。

### 待辦／已知限制
- `CE` 僅處理結尾數字，科學記號結尾（如 `1E+5`）會直接全部清除。
- 連按 `=` 僅支援最右側頂層運算子的數值運算，函式或後置運算（`5!`、`50%`）不重複。
- 焦點框、placeholder 與各主題對比仍需實際啟動目視確認。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.4.0

### 目標
修正「顯示模式、主題色切換沒有作用」，並新增 2 款主題色（薰衣草、暖棕）。

### 變更
- `Styles/Themes/*.xaml`（共 24 檔）：改為在每份主題字典直接定義 18 個 `SolidColorBrush`，移除 `Color` 間接層。
- 刪除 `Styles/Brushes.xaml`，`App.xaml` 移除其合併項目。
- `Services/ThemeService.cs`：套用主題時先移除舊字典，再 `Add` 到最後（最高優先），不再 `Insert(0)`。
- `Models/ThemeName.cs`：新增 `Lavender`、`WarmBrown`。
- 新增 `Light.Lavender.xaml`、`Dark.Lavender.xaml`、`Light.WarmBrown.xaml`、`Dark.WarmBrown.xaml`。
- `Styles/ThemeSwatches.xaml`、`Strings/Resources.resx`、`Views/SettingsWindow.xaml`：新增 2 色色票、名稱與選項。
- `Directory.Build.props`：版本 1.4.0。

### 問題與解法
- 症狀：切換顯示模式或主題色後畫面不變，只有啟動時第一次套用的主題生效。
- 根因：筆刷的 `Color` 以 `{DynamicResource ...Color}` 指向另一本合併字典；WPF 在姊妹字典被替換時不會重新解析 Freezable 的 DynamicResource。以 STA WPF 記憶體重現驗證：筆刷定義在可替換字典＋元素用 `DynamicResource` 會更新；筆刷以 `DynamicResource` 取色則不會。
- 解法：主題字典直接定義筆刷，元素與樣式一律以 `DynamicResource` 引用，換字典即可更新。

### 待辦／已知限制
- 12 主題 × 淺/深仍需人工目視確認。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.3.0

### 目標
依架構檢視結果，執行 7 項低風險高效益優化與 2 項可選重構：降低 UI 執行緒 I/O、去除主題字典重複、減少輸入時解析負擔、補強可觀測性與視窗還原正確性。

### 變更
- `Services/JsonFileStore.cs`：新增泛型 JSON 儲存器，400ms 延遲合併、背景 `Timer` 寫入、`Semaphore`/lock 序列化與原子寫入（`.tmp` + `Move`），`Dispose`/`Flush` 時落地。
- `Services/AppJsonContext.cs`：啟用 `System.Text.Json` source generation（`JsonStringEnumConverter` 由 `UseStringEnumConverter` 產生）。
- `Services/SettingsService.cs`、`Services/HistoryService.cs`：改用 `JsonFileStore<T>`，不再於 UI 執行緒同步寫檔；兩者實作 `IDisposable`。
- `Styles/Brushes.xaml`：新增共用筆刷對應，筆刷 `Color` 以 `DynamicResource` 指向主題色。
- `Styles/Themes/*.xaml`（20 檔）：改為只定義 18 個 `Color` 資源，不再重複宣告筆刷；`App.xaml` 合併載入 `Brushes.xaml`。
- `ViewModels/CalculatorViewModel.cs`：即時預覽改以 `DispatcherTimer` 節流 100ms；`Evaluate`/清除/記憶體/回填歷史時取消待處理預覽，避免覆蓋結果或錯誤訊息。
- `Services/CalculatorEngine.cs`：數字解析改用 `ReadOnlySpan<char>` 與 `double.TryParse(span)`，減少每次求值的字串配置；未知例外改為記錄後回傳 Syntax。
- `Services/LogMessages.cs`：以 `[LoggerMessage]` 產生結構化日誌方法。
- `Services/FileLoggerProvider.cs`：新增輕量檔案記錄器（寫入 `%AppData%\MiniCalculator\logs\app-yyyyMMdd.log`，僅 Warning 以上）。
- `App.xaml.cs`：單一實例檢查提前至套用主題前；掛載檔案記錄器（Debug 另加 `AddDebug`）；註冊 `Func<HistoryWindow>`／`Func<SettingsWindow>` 工廠。
- `Views/MainWindow.xaml.cs`：改注入視窗工廠，移除 `App.Services` service locator。
- `Helpers/WindowPlacementHelper.cs`：還原位置時以 `SystemParameters.WorkArea` 夾限，避免視窗落在工作列下方；仍保留虛擬螢幕交集檢查。
- `Services/ThemeService.cs`：`UserPreferenceChanged` 改 `Dispatcher.BeginInvoke`，避免阻塞系統事件執行緒。
- `Services/SingleInstanceService.cs`：以具名 `EventWaitHandle` + `RegisterWaitForSingleObject` 取代 Named Pipe 輪詢，保留 Mutex 單一實例。
- `Directory.Build.props`：`EnableNETAnalyzers`、`AnalysisMode=Recommended`、`EnforceCodeStyleInBuild`；版本 1.3.0。
- `tests/MiniCalculator.Tests/MiniCalculator.Tests.csproj`：移除已無用的 `UseWindowsForms`。

### 問題與解法
- `System.Text.Json` source generator 看不到 CommunityToolkit.Mvvm 產生的 `[ObservableProperty]` 屬性，`AppSettings` 一度只序列化出 `Window`；改以純 DTO `SettingsData` 作為存檔邊界，再對應回 `AppSettings`。
- 啟用 Recommended 分析器後 `CA1848`/`CA1873` 要求使用 `LoggerMessage`，改以 `LogMessages` 集中產生委派；`CA1822` 則將無狀態成員改靜態，About 屬性改在建構時取值以保留 WPF 繫結。
- 主題筆刷與顏色分屬不同合併字典時，`DynamicResource` 需靠 `Freezable` 繼承內容解析；筆刷不凍結即可於切換主題時更新。
- 單一實例若在事件尚未建立時收到第二次啟動，`OpenExisting` 會失敗；沿用最多 20 次、每次 100ms 的重試。

### 待辦／已知限制
- 主題切換與共用筆刷僅完成建置與啟動驗證，尚未逐一切換 10 主題 × 淺/深目視確認。
- 視窗還原以主要螢幕工作區夾限，多螢幕不同 DPI 的次要螢幕仍以虛擬螢幕範圍判斷。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.2.2

### 目標
依使用者需求取消「關閉時縮到系統匣」的行為，關閉主視窗即完整結束應用程式。

### 變更
- 刪除 `Services/TrayService.cs`、`Services/ITrayService.cs`。
- `App.xaml.cs`：移除系統匣服務的欄位、DI 註冊、初始化與釋放；第二次啟動改為呼叫 `MainWindow.ShowAndActivate`。
- `Views/MainWindow.xaml.cs`：移除 `CloseToTray` 攔截與 `ExitApplication`，`RestoreFromTray` 改名為 `ShowAndActivate`；關閉時僅保存視窗位置即結束。
- `Models/AppSettings.cs`：移除 `CloseToTray` 設定。
- `Views/SettingsWindow.xaml`：移除「關閉時縮到系統匣」選項。
- `Strings/Resources.resx`：移除 `Tray_Open`、`Tray_Exit`、`Tray_Tooltip`、`Settings_CloseToTray`。
- `MiniCalculator.csproj`：移除 `UseWindowsForms`、相關全域 using 移除與 `WFO0003` NoWarn。
- `Directory.Build.props`：版本提升至 1.2.2（`AssemblyVersion` 依規範維持 1.2.0.0）。
- `README.md`、`CHANGELOG.md`、`RELEASE_NOTES.md`、`AGENTS.md`：同步移除系統匣相關描述。

### 問題與解法
- 系統匣僅為「關閉縮到系統匣」而存在，取消該行為後一併移除服務與 WinForms 相依，避免留下無用途的常駐圖示。
- 單一實例原本透過系統匣還原視窗，改為直接顯示並啟用主視窗，功能不受影響。

### 待辦／已知限制
- 既有 `settings.json` 中的 `closeToTray` 欄位會在下次儲存時自動消失（讀取時忽略未知欄位）。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.2.1

### 目標
修正科學模式在視窗縮到最小（620 DIP）時，鍵盤底部按鈕被裁切、無法完整顯示的問題。

### 變更
- `Styles/Controls.xaml`：`KeyButton`、`AccentKeyButton` 的 `MinHeight` 46→30；`FunctionKeyButton`／`KeyToggleButton` 40→28；`MemoryKeyButton` 36→26。
- `Views/HistoryWindow.xaml`、`Views/SettingsWindow.xaml`：為使用共用樣式的按鈕補上 `MinHeight`（40／46），維持對話框外觀。
- `Directory.Build.props`：版本提升至 1.2.1（`AssemblyVersion` 依規範維持 1.2.0.0）。
- `CHANGELOG.md`、`RELEASE_NOTES.md`：補上本次修正。

### 問題與解法
- 科學模式為 6 欄 × 9 列，`MinHeight` 加上 3px 邊距後每列最少約需 52 DIP，9 列共約 468 DIP；但最小視窗扣除標題列與顯示區後，鍵盤區僅剩約 380 DIP，導致底列溢出被視窗裁切。`UniformGrid` 會依可用高度平均分配列高，故只要按鈕 `MinHeight` 不高於每列可用高度即可完整顯示；將最小值降到 30 以下後，最小視窗每列約 36 DIP 仍可正常顯示與點擊。

### 待辦／已知限制
- 按鈕高度為縮放配置，未逐一於各 DPI 比例目視確認。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.2.0

### 目標
依使用者回饋，將十款主題色全面改為輕鬆活潑的配色，並把設定頁的顯示模式與主題色改為非下拉選單的選擇方式。

### 變更
- `Styles/Themes/`（20 檔）：淺色改為粉彩底＋鮮明主色，深色改為帶色相深底＋明亮主色；主色前景依亮度改用白色或深墨，維持可讀性。
- `Styles/ThemeSwatches.xaml`：新增 10 個主題色票筆刷（`Swatch_*`），於 `App.xaml` 合併載入。
- `Helpers/EnumToBooleanConverter.cs`：新增列舉與 RadioButton 雙向繫結轉換器。
- `App.xaml`：載入色票字典並註冊 `EnumToBool` 轉換器。
- `Styles/Controls.xaml`：新增 `SegmentOption`、`ThemeSwatchItem`、`ThemeSwatchList` 樣式；移除已無使用的 `ComboBox`／`ThemedComboBoxItem` 樣式。
- `Views/SettingsWindow.xaml`：顯示模式改為三段膠囊按鈕；主題色改為 5×2 色票方格；視窗高度調整為 720。
- `Directory.Build.props`：版本提升至 1.2.0。
- `README.md`、`CHANGELOG.md`、`RELEASE_NOTES.md`：同步更新。

### 問題與解法
- RadioButton 共用 `GroupName` 時，未選取會回寫 `false`；轉換器 `ConvertBack` 對 `false` 回傳 `Binding.DoNothing`，避免覆寫目前列舉值。
- 色票需在非目前主題下仍顯示各自主色，故以獨立字典集中定義 `Swatch_*`，不依賴當下套用的主題字典。

### 待辦／已知限制
- 色彩對比仍以設計值與目視確認，尚未加入自動化檢測。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.1.1

### 目標
依使用者回饋，將淺色模式主題色的主色調淺一些。

### 變更
- `Styles/Themes/Light.*.xaml`（10 檔）：`AccentBrush`／`AccentHoverBrush`／`AccentPressedBrush` 以 HSL 亮度 +9 個百分點調亮，色相與飽和度不變，維持 hover > 主色 > pressed 的層次。
- `Light.EyeCareYellow.xaml`、`Light.SunsetOrange.xaml`、`Light.Teal.xaml`：`AccentForegroundBrush` 由白改為 `#1A1A1A`，避免調亮後白字對比不足。
- `Directory.Build.props`：版本提升至 1.1.1（`AssemblyVersion` 依規範維持 1.1.0.0）。

### 問題與解法
- 直接沿用白字時，護眼黃、夕陽橘主色與白字對比降至 2.x：1（低於大字 3:1），故僅這三款改用深色前景；其餘主題維持白字仍達 3:1 以上。

### 待辦／已知限制
- 未新增自動化色彩對比檢測，仍以計算值與目視確認。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.1.0

### 目標
將關於資訊整合至設定頁底部、移除主畫面的關於入口，新增五款主題色，並修正深色模式下下拉選單與 ToolTip 文字不可見的問題。

### 變更
- `Views/MainWindow.xaml`、`MainWindow.xaml.cs`：移除關於圖示與 `OnAboutClick`。
- `ViewModels/SettingsViewModel.cs`：新增，整合 `AppSettings` 與 `AboutViewModel`。
- `Views/SettingsWindow.xaml`：改綁 `Settings.*`，內容改用 `ScrollViewer`，底部加入關於資訊區塊。
- `App.xaml.cs`：註冊 `SettingsViewModel`，移除 `AboutWindow` 註冊。
- 刪除 `Views/AboutWindow.xaml`、`AboutWindow.xaml.cs`。
- `Models/ThemeName.cs`：新增 `RosePink`、`Teal`、`SunsetOrange`、`Indigo`、`CoralRed`。
- `Styles/Themes/`：新增 10 份主題字典（5 色 × 淺/深）。
- `Styles/Controls.xaml`：加入主題化 `ComboBox`／`ComboBoxItem`／`ToolTip`／`CheckBox` 樣式。
- `Strings/Resources.resx`：新增 5 個主題名稱字串，移除 `App_About`。
- `Directory.Build.props`：版本提升至 1.1.0。

### 問題與解法
- 深色模式下 `TextBlock` 隱含樣式把前景設為近白色，但 `ComboBox` 彈出清單與 `ToolTip` 仍為系統淺色背景，造成白字白底 → 為這些控制項補上以主題資源為基礎的樣式與範本。

### 待辦／已知限制
- 主題字典的實際色彩對比僅以目視檢查，尚未做自動化對比度檢測。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-26 - v1.0.0

### 目標
建立 Windows 桌面「mini計算機」，提供標準與科學模式，並具備主題、歷史、記憶體、系統匣等桌面整合功能。

### 變更
- 建立 WPF 方案、Central Package Management、Microsoft.Extensions.Hosting DI 與 MVVM 結構。
- `CalculatorEngine`：遞迴下降剖析，支援優先序、括號、一元負號、函式、常數、`%`、`!`、`mod`、角度單位與 DMS；錯誤分類為語法/除以零/無效/溢位。
- `NumberFormatter`：16 位有效數字、千分位、科學記號與可回填運算式的格式。
- 標準/科學檢視：雙行顯示（可編輯運算式 + 即時結果）、按鍵與實體鍵盤、Enter/Esc、複製結果、2nd/hyp/F-E/角度切換。
- 5 主題色 × 淺/深共 10 組 ResourceDictionary，`ThemeService` 以 pack URI 即時置換；跟隨系統以 Registry 與 `SystemEvents` 實作。
- 設定與歷史以 JSON 存於 `%AppData%\MiniCalculator`，記憶體單槽並持久化。
- 系統匣（NotifyIcon）、單一實例（Mutex + 具名 Pipe）、視窗置頂、視窗狀態保存與螢幕邊界檢查。
- App 圖示（多尺寸 ICO）、設定頁、關於頁、README/CHANGELOG/RELEASE_NOTES。
- xUnit 測試：引擎 36 例、格式化 11 例、ViewModel 10 例，共 57 例。

### 問題與解法
- `net10.0-windows10.0.19041.0` 搭配 `TargetPlatformMinVersion=10.0.22000.0` 會觸發 NETSDK1135（最低 OS 不能高於 TFM 平台版本）→ 將 TFM 提高為 `net10.0-windows10.0.22000.0`，以符合 Windows 11 最低需求。
- `UseWindowsForms` 會自動加入 `System.Windows.Forms` 與 `System.Drawing` 全域 using，與 WPF 型別（`Application`、`Localization`）衝突 → 以 `<Using Remove>` 移除全域 using，僅在 TrayService 以別名使用。
- WinForms 分析器 WFO0003 對自訂 manifest 的 DPI 設定報錯（本專案實際為 WPF）→ 針對 WFO0003 設定 NoWarn。
- `xUnit` 初次測試 `asin(1)` 於 GRAD 應為 100 而非 90，修正測試期望值。
- 以螢幕像素取樣驗證主題字典確實套用（淺色 `#F3F6FA`、深色 `#1B1F24`、Accent 色皆出現）。

### 待辦／已知限制
- 尚未針對多螢幕高 DPI 與極長運算式做完整實測。
- 標準模式可用鍵盤輸入括號，但畫面未提供括號鍵（依規劃）。
- ComboBox、CheckBox 等原生控制項之外觀仍採用系統預設，未完全套用主題。
- 尚未建立 MSIX 安裝封裝。

---

## 2026-11-23 - v1.0.0

### 目標
調整共用開發規範：UI 改為 WinUI 3／WPF 二選一、App 圖示與視窗狀態列為必做、平台提高為 Windows 11 以上。

### 變更
- `AGENTS.md`：技術基線 UI 改為 WinUI 3（建議）／WPF 二選一，平台改 Windows 11（build 22000）以上。
- `AGENTS.md`：專案結構、程式碼規範、共用界面補上 WPF 對照（.resx、DynamicResource、NavigationView 替代、WindowChrome 等）。
- `AGENTS.md`：第 5 章拆為「必要／選用」，App 圖示與視窗狀態改為必做並補兩框架作法。
- `AGENTS.md`：`TargetPlatformMinVersion` 改 `10.0.22000.0`，TFM 維持 `net10.0-windows10.0.19041.0`；發佈指令分 WinUI 3／WPF。
- `AGENTS.md`：專案專屬補充新增 UI 框架與最低平台欄位。

### 問題與解法
- 無。

### 待辦／已知限制
- 尚未建立實際專案驗證兩框架範本。
