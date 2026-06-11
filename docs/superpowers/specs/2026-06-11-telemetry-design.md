# Telemetry 数据统计收集功能设计

## 背景

DocuFiller 是一个 .NET 8 WPF 桌面应用，内部几十个用户使用。需要增加远程使用数据统计收集功能，用于了解功能使用频次、成功率、版本分布等。

现有基础设施：
- Go 语言 update-hub 服务器（SQLite + HTTP，端口 30001）
- 已有 Velopack 自动更新机制
- 已有 Vue 前端（嵌入 Go 二进制）

## 方案选型

选择**自定义方案**：在 update-hub 上新增 telemetry 端点，复用现有 Go 服务器 + SQLite。

理由：
- 30-50 内部用户，每天几百条事件，SQLite 轻松应对
- 零新基础设施，零新外部依赖
- 完全控制数据模型，支持未来其他内网应用接入
- Dashboard 可复用现有 Vue 前端

## 数据模型

### 通用事件结构

所有应用共用一套顶层字段，应用自定义数据放 `properties`：

```json
{
  "app_id": "docufiller",
  "signature": "a3f7b2c...",
  "event": "fill_complete",
  "timestamp": "2026-06-11T14:30:00+08:00",
  "session_id": "guid-per-launch",
  "user": "zhangsan",
  "machine": "PC-001",
  "version": "1.13.0",
  "properties": {
    "file_count": 5,
    "success_count": 4,
    "fail_count": 1,
    "duration_ms": 3200,
    "input_mode": "folder"
  }
}
```

| 字段 | 必选 | 说明 |
|------|------|------|
| `app_id` | 是 | 应用标识，对应 update-hub 中的 appId |
| `signature` | 是 | HMAC-SHA256 签名，防篡改 |
| `event` | 是 | 事件名，如 `fill_complete` |
| `timestamp` | 是 | ISO8601 格式 |
| `session_id` | 是 | 每次启动生成一个 GUID |
| `user` | 是 | `Environment.UserName` |
| `machine` | 是 | `Environment.MachineName` |
| `version` | 是 | 应用版本号 |
| `properties` | 否 | 应用自定义的 KV 数据，自由 JSON |

### DocuFiller 事件定义

#### 应用生命周期

| 事件 | 触发时机 | properties |
|------|---------|------------|
| `app_start` | 应用启动 | `launch_mode`: "gui"/"cli", `is_installed`: bool, `is_portable`: bool |
| `app_exit` | 正常退出 | `session_duration_sec`: int |
| `app_crash` | 未捕获异常 | `exception_type`: string, `exception_message`: string |

#### 文档填充 (fill)

| 事件 | 触发时机 | properties |
|------|---------|------------|
| `fill_complete` | 填充处理完成 | `file_count`: int, `success_count`: int, `fail_count`: int, `duration_ms`: int, `input_mode`: "single"/"folder", `excel_format`: "2col"/"3col" |

#### 文档清理 (cleanup)

| 事件 | 触发时机 | properties |
|------|---------|------------|
| `cleanup_complete` | 清理完成 | `file_count`: int, `comments_removed`: int, `controls_unwrapped`: int, `duration_ms`: int, `input_mode`: "single"/"folder" |

#### 模板检查 (inspect)

| 事件 | 触发时机 | properties |
|------|---------|------------|
| `inspect_complete` | 检查完成 | `control_count`: int, `duration_ms`: int |

#### 更新事件

| 事件 | 触发时机 | properties |
|------|---------|------------|
| `update_check` | 检查更新后 | `has_update`: bool, `current_version`: string, `latest_version`: string (nullable) |
| `update_applied` | 应用更新后 | `old_version`: string, `new_version`: string |

### 明确不收集的数据

- 文档内容（关键词、替换值）
- 文件路径和文件名
- Excel 数据内容
- 任何业务敏感信息

## 数据校验机制

采用 HMAC-SHA256 对称签名，按 `app_id` 隔离密钥。

### 签名流程

```
客户端:
  1. 构造 payload 对象（不含 signature 字段）
  2. 序列化为 JSON 字符串（排序 key，确保确定性）
  3. signature = HMAC-SHA256(app_secret_key, json_bytes) → hex string
  4. 将 signature 加入 payload
  5. POST /api/telemetry

服务端:
  1. 从请求体取 app_id 和 signature
  2. 根据 app_id 查找对应的 app_secret_key
  3. 移除 signature 字段后重新序列化
  4. 重算 HMAC-SHA256(app_secret_key, json_bytes)
  5. 对比签名 → 一致则入库，不一致返回 401
```

### 密钥管理

| 端 | 位置 | 说明 |
|----|------|------|
| 客户端 (DocuFiller) | 编译时内置于代码中 | 32 字节 hex string |
| 客户端 (未来应用) | 各应用各自内置 | 同上 |
| 服务端 (update-hub) | 启动参数 `--telemetry-keys docufiller=abc123,otherapp=def456` | `app_id=key` 逗号分隔 |

- 未配置密钥的 `app_id` 默认拒绝
- 可选：配置 `--telemetry-keys-open` 标志允许未配置密钥的 app_id 跳过验签

## 服务端设计（Go，update-hub）

### 数据库

在现有 SQLite 中新增表：

```sql
CREATE TABLE telemetry_events (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    app_id      TEXT NOT NULL,
    event       TEXT NOT NULL,
    timestamp   TEXT NOT NULL,
    session_id  TEXT NOT NULL,
    user_name   TEXT NOT NULL,
    machine     TEXT NOT NULL,
    version     TEXT NOT NULL,
    properties  TEXT,
    received_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_tel_app_time ON telemetry_events(app_id, timestamp);
CREATE INDEX idx_tel_event ON telemetry_events(app_id, event);
```

### API 端点

| 方法 | 路径 | 鉴权 | 说明 |
|------|------|------|------|
| POST | `/api/telemetry` | HMAC 签名校验 | 接收事件（支持单个或批量数组） |
| GET | `/api/telemetry/stats` | Bearer Token（复用现有鉴权） | 聚合统计数据，供 dashboard 使用 |
| GET | `/api/telemetry/apps` | Bearer Token | 返回所有已接入的 app_id 列表 |

POST `/api/telemetry` 请求体支持两种格式：
- 单个事件：`{ "app_id": ..., "event": ..., ... }`
- 批量事件：`[ { ... }, { ... } ]`

### 数据保留

- 默认保留 90 天
- 可通过启动参数 `--telemetry-retention-days` 配置
- 在每次写入时顺带清理过期数据（低频操作，不影响性能）

### Dashboard

在现有 Vue 前端中新增 `/telemetry` 页面：

- 左侧：`app_id` 下拉选择器 + 时间范围筛选
- 主区域：
  - **概览卡片**：今日活跃用户数、今日事件数、7 日趋势
  - **功能使用频次**：按 event 分组的柱状图（ECharts）
  - **版本分布**：饼图
  - **用户活跃度**：按 user 分组的排行表格
  - **错误率**：`*_complete` 事件中 fail 相关的趋势折线图

## 客户端设计（C#，DocuFiller）

### 新增服务：TelemetryService

注册为 Singleton，接口 `ITelemetryService`。

```
TelemetryService
├── 构造：读取 appsettings.json Telemetry 配置 + 内置密钥
├── TrackEvent(event, properties?)
│   ├── 构造事件对象（填充公共字段）
│   ├── 计算 HMAC-SHA256 签名
│   └── 放入内存队列 (Channel<T>)
├── 后台消费线程
│   ├── 每 30 秒或队列满 20 条时触发
│   ├── 批量序列化 + POST /api/telemetry
│   └── 失败重试：最多 3 次，指数退避 (1s, 2s, 4s)
├── FlushAsync()
│   └── 应用退出时调用，发送剩余事件
└── Dispose()
    └── 取消后台线程，尝试最终 flush
```

### 配置（appsettings.json）

```json
{
  "Telemetry": {
    "EndpointUrl": "http://<update-hub-host>:30001/api/telemetry",
    "Enabled": true,
    "BatchSize": 20,
    "FlushIntervalSeconds": 30
  }
}
```

- `Enabled: false` 或 `EndpointUrl` 为空时，TrackEvent 直接返回，不收集不发送
- 后续可在 update-hub Web UI 中提供开关控制

### 埋点位置

| 文件 | 时机 | 事件 |
|------|------|------|
| `App.xaml.cs` OnStartup | 启动时 | `app_start` |
| `App.xaml.cs` OnExit | 退出时 | `app_exit` |
| `App.xaml.cs` 全局异常 | 未捕获异常 | `app_crash` |
| `DocumentProcessorService` ProcessDocumentsAsync 返回后 | 填充完成 | `fill_complete` |
| `DocumentCleanupService` CleanupAsync 返回后 | 清理完成 | `cleanup_complete` |
| `InspectCommand` ExecuteAsync 返回后 | 检查完成 | `inspect_complete` |
| `UpdateService` CheckForUpdatesAsync 后 | 更新检查 | `update_check` |
| `UpdateService` ApplyUpdatesAndRestart 前 | 应用更新 | `update_applied` |

### 错误处理原则

- telemetry 发送失败**不影响主功能**：所有网络异常静默吞掉
- 后台线程异常不 crash 应用
- 队列满时丢弃最旧事件（优先保证应用稳定性）
- 日志记录发送失败（Debug 级别，不打扰用户）

## 文件变更范围

### update-hub（Go）

| 操作 | 文件 | 说明 |
|------|------|------|
| 新增 | `telemetry/handler.go` | POST /api/telemetry 处理器 |
| 新增 | `telemetry/stats.go` | GET /api/telemetry/stats 处理器 |
| 新增 | `telemetry/verify.go` | HMAC-SHA256 验签逻辑 |
| 新增 | `telemetry/schema.go` | 事件数据模型 |
| 新增 | `telemetry/cleanup.go` | 过期数据清理 |
| 修改 | `main.go` | 注册路由、解析 telemetry 启动参数 |
| 修改 | `database/init.go` | 新增 telemetry_events 表迁移 |
| 新增 | `web/src/views/TelemetryView.vue` | Dashboard 页面 |

### DocuFiller（C#）

| 操作 | 文件 | 说明 |
|------|------|------|
| 新增 | `Services/Interfaces/ITelemetryService.cs` | 接口定义 |
| 新增 | `Services/TelemetryService.cs` | 实现类 |
| 新增 | `Configuration/TelemetrySettings.cs` | 配置类 |
| 修改 | `App.xaml.cs` | 注册服务 + 启动/退出/异常埋点 |
| 修改 | `appsettings.json` | 新增 Telemetry 配置节 |
| 修改 | `Services/DocumentProcessorService.cs` | fill_complete 埋点 |
| 修改 | `DocuFiller/Services/DocumentCleanupService.cs` | cleanup_complete 埋点 |
| 修改 | `Cli/Commands/InspectCommand.cs` | inspect_complete 埋点 |
| 修改 | `Services/UpdateService.cs` | update_check / update_applied 埋点 |
