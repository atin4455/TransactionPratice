# TransactionPratice

這是一個用來練習 **SQL Server Transaction 回朔** 的 ASP.NET Core MVC 專案。  
目前只保留 **Web + Service** 兩層，重點在「回圈批次交易中途失敗，整批回朔」。

## 環境需求

- Windows
- .NET SDK（本專案目標 `net10.0`）
- SQL Server（localhost，可用 Windows 驗證）

## 連線設定

`appsettings.json` 目前使用：

`Server=localhost;Database=TransactionPractice;Trusted_Connection=True;TrustServerCertificate=True;`

如果你的 SQL Server instance 不是預設，可改成：

`Server=localhost\\SQLEXPRESS;Database=TransactionPractice;Trusted_Connection=True;TrustServerCertificate=True;`

## 如何執行

1. 在專案根目錄執行：
   - `dotnet run`
2. 打開瀏覽器進入 `Transfer` 頁面（預設首頁也會導向這邊）。

## 頁面功能（只保留迴圈測試）

`Transfer` 頁面提供「Transaction 回朔壓力測試」表單：

- `轉出帳戶`
- `轉入帳戶`
- `每筆金額`
- `回圈次數`
- `中斷於第幾筆 (0=不中斷)`

按下「執行批次交易測試」後，Service 會：

1. 開啟同一個 SQL Transaction
2. 在回圈中重複扣款/加款
3. 若到達指定中斷點，故意丟出例外
4. 失敗時 rollback，成功才 commit

## EF Transaction 方法說明

這個專案主要使用下列 4 個 EF Core 交易方法：

- `BeginTransactionAsync`
  - 用途：開啟一個資料庫交易（transaction scope）。
  - 何時用：在一組「要嘛全成功、要嘛全失敗」的操作開始前呼叫。
  - 在本專案：批次回圈開始前先開 transaction，讓整批更新綁在同一筆交易中。

- `SaveChangesAsync`
  - 用途：把 `DbContext` 追蹤到的變更實際送到資料庫（執行 SQL）。
  - 何時用：每次改完實體資料後呼叫，將變更寫入目前交易。
  - 在本專案：每圈扣款/加款後呼叫，變更先寫入交易中，但尚未最終提交。

- `CommitAsync`
  - 用途：正式提交交易，讓交易內所有變更永久生效。
  - 何時用：整段流程都成功、沒有例外時呼叫。
  - 在本專案：回圈全部完成且沒中斷才 commit，餘額變動才會真的落地。

- `RollbackAsync`
  - 用途：取消本次交易中所有未提交變更，還原到交易開始前狀態。
  - 何時用：流程中任一步失敗或拋例外時呼叫。
  - 在本專案：中途故意中斷或發生錯誤就 rollback，前面已執行的圈數也會一起回朔。

## 建表與種子資料

Service 會自動確保 `Accounts` 表存在，若是空表會補初始資料：

- `1, Alice, 1000`
- `2, Bob, 500`
- `3, Carol, 300`

## 測試流程（重點）

### 案例 A：驗證 rollback

- 參數建議：
  - 轉出帳戶：`1`
  - 轉入帳戶：`2`
  - 每筆金額：`1`
  - 回圈次數：`100`
  - 中斷於第幾筆：`30`
- 預期：
  - 顯示批次失敗 + 已 rollback
  - 下方帳戶餘額與測試前一致（表示前 29 筆也被回朔）

### 案例 B：驗證 commit

- 參數建議：
  - 轉出帳戶：`1`
  - 轉入帳戶：`2`
  - 每筆金額：`1`
  - 回圈次數：`100`
  - 中斷於第幾筆：`0`
- 預期：
  - 顯示批次成功 + 已 commit
  - 帳戶 `1` 減少 `100`，帳戶 `2` 增加 `100`

## 注意事項

- 若 `fromAccountId == toAccountId`、金額 <= 0、回圈次數 <= 0，會直接回傳錯誤。
- 若中斷點超出範圍（不是 0 或 1~回圈次數），會直接回傳錯誤。
- 這個專案以「交易行為驗證」為主，非完整分層範本。  