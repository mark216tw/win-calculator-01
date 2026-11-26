# 貢獻指南

感謝你願意協助改善 mini計算機。本專案以 .NET 10 / WPF 開發，歡迎提出問題、修正錯誤或新增功能。

## 開發環境

- Windows 11（build 22000 以上）
- .NET 10 SDK
- 建議使用 Visual Studio 2026 或支援 C# 的編輯器

## 取得原始碼與建置

```powershell
git clone https://github.com/mark216tw/win-calculator-01.git
cd win-calculator-01
dotnet build MiniCalculator.sln -c Debug
dotnet test MiniCalculator.sln -c Debug
```

執行應用程式：

```powershell
dotnet run --project src/MiniCalculator/MiniCalculator.csproj
```

## 開發規範

- 使用 4 空格縮排，C# 採檔案範圍命名空間（file-scoped namespace）。
- 類別／方法使用 `PascalCase`，區域變數／參數使用 `camelCase`，私有欄位使用 `_camelCase`。
- 遵循既有 MVVM 結構：UI 放 `Views/`、狀態與命令放 `ViewModels/`、商業邏輯放 `Services/`。
- 使用者可見文字放入 `Strings/Resources.resx`，不要寫死在 XAML 或程式碼。
- 顏色與筆刷使用主題字典資源，不要在 XAML 寫死色碼。
- 非同步方法以 `Async` 結尾；不要封鎖 UI 執行緒。
- 不要新增未在 `Directory.Packages.props` 宣告的套件。
- 不要留下 `TODO`、測試假資料、`Console.WriteLine` 或 `Debug.WriteLine`。

## 測試與格式檢查

```powershell
dotnet test MiniCalculator.sln -c Debug
dotnet format MiniCalculator.sln --verify-no-changes
```

若修改求值引擎、格式化或 ViewModel 行為，請同步補充 xUnit 測試。

## 送出變更

1. 從 `main` 建立功能分支。
2. 完成變更並確認建置、測試與格式檢查通過。
3. 更新 `docs/DEV_LOG.md`，記錄目標、變更、問題與待辦。
4. 若功能有異動，更新 `CHANGELOG.md` 與版本資訊。
5. 建立 Pull Request，說明變更動機、影響範圍與驗證方式。

## 版本與發佈

版本號集中於 `Directory.Build.props`，採 `主.次.修`。發佈流程請見 [建置與發佈](docs/BUILD_AND_RELEASE.md)。

## 授權

送出貢獻即表示同意以本專案的 [MIT License](LICENSE) 授權你的貢獻。
