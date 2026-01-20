# Update Server 简化重构实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 将 update-server 从手动配置转变为开箱即用的自动更新服务，通过 Web 管理界面简化服务器管理和客户端工具配置。

**架构:** 使用 Go + Gin 后端 + 嵌入式 Web 前端（纯 JavaScript），实现初始化向导、程序管理、Token 管理和客户端工具动态打包下载。采用端到端加密，每个程序独立的 AES-256-GCM 密钥。

**技术栈:**
- 后端: Go 1.23+ / Gin / GORM / SQLite
- 前端: 纯 JavaScript / CSS / embedded FS
- 加密: AES-256-GCM / HKDF

---

## Task 1: 数据库模型扩展

**Files:**
- Create: `internal/models/admin_user.go`
- Modify: `internal/models/program.go`
- Create: `internal/models/encryption_key.go`
- Modify: `internal/models/token.go`

**Step 1: 添加管理员用户模型**

创建 `internal/models/admin_user.go`:
```go
package models

import (
	"time"
	"golang.org/x/crypto/bcrypt"
	"gorm.io/gorm"
)

type AdminUser struct {
	ID           uint           `gorm:"primaryKey"`
	Username     string         `gorm:"uniqueIndex;size:50;not null"`
	PasswordHash string         `gorm:"size:255;not null"`
	CreatedAt    time.Time      `json:"createdAt"`
	UpdatedAt    time.Time      `json:"updatedAt"`
	DeletedAt    gorm.DeletedAt `gorm:"index" json:"-"`
}

// SetPassword 设置密码（哈希）
func (u *AdminUser) SetPassword(password string) error {
	hash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return err
	}
	u.PasswordHash = string(hash)
	return nil
}

// CheckPassword 验证密码
func (u *AdminUser) CheckPassword(password string) bool {
	err := bcrypt.CompareHashAndPassword([]byte(u.PasswordHash), []byte(password))
	return err == nil
}
```

**Step 2: 扩展 Program 模型**

修改 `internal/models/program.go`，添加 ServerURL 字段:
```go
type Program struct {
	ID          uint           `gorm:"primaryKey"`
	ProgramID   string         `gorm:"uniqueIndex;size:50;not null" json:"programId"`
	Name        string         `gorm:"size:100;not null" json:"name"`
	Description string         `gorm:"size:500" json:"description"`
	IconURL     string         `gorm:"size:255" json:"iconUrl"`
	IsActive    bool           `gorm:"default:true" json:"isActive"`
	CreatedAt   time.Time      `json:"createdAt"`
	UpdatedAt   time.Time      `json:"updatedAt"`
	DeletedAt   gorm.DeletedAt `gorm:"index" json:"-"`
}
```

**Step 3: 添加加密密钥模型**

创建 `internal/models/encryption_key.go`:
```go
package models

import (
	"time"
	"gorm.io/gorm"
)

type EncryptionKey struct {
	ID        uint           `gorm:"primaryKey"`
	ProgramID string         `gorm:"uniqueIndex;size:50;not null"`
	KeyData   string         `gorm:"size:255;not null"` // Base64 编码的密钥
	CreatedAt time.Time      `json:"createdAt"`
	UpdatedAt time.Time      `json:"updatedAt"`
	DeletedAt gorm.DeletedAt `gorm:"index" json:"-"`
}
```

**Step 4: 修改 Token 模型支持程序关联**

修改 `internal/models/token.go`，确保 ProgramID 字段存在:
```go
type Token struct {
	ID         uint           `gorm:"primaryKey"`
	TokenID    string         `gorm:"uniqueIndex;size:64;not null"`
	ProgramID  string         `gorm:"index;size:50;not null"`
	TokenType  string         `gorm:"size:20;not null"` // upload, download
	CreatedBy  string         `gorm:"size:100"`
	ExpiresAt  *time.Time     `json:"expiresAt"`
	IsActive   bool           `gorm:"default:true"`
	CreatedAt  time.Time      `json:"createdAt"`
	LastUsedAt *time.Time     `json:"lastUsedAt"`
	DeletedAt  gorm.DeletedAt `gorm:"index" json:"-"`
}
```

**Step 5: 更新数据库迁移**

修改 `internal/database/gorm.go`:
```go
func AutoMigrate(db *gorm.DB) error {
	return db.AutoMigrate(
		&models.Program{},
		&models.Version{},
		&models.Token{},
		&models.AdminUser{},
		&models.EncryptionKey{},
	)
}
```

**Step 6: 提交**

```bash
git add internal/models/
git commit -m "feat: 添加管理员用户和加密密钥模型"
```

---

## Task 2: 系统配置扩展

**Files:**
- Modify: `internal/config/config.go`

**Step 1: 扩展配置结构**

修改 `internal/config/config.go`，添加 ServerURL 和 AdminInitialized:
```go
type Config struct {
	Server             ServerConfig   `yaml:"server"`
	Database           DatabaseConfig `yaml:"database"`
	Storage            StorageConfig  `yaml:"storage"`
	API                APIConfig      `yaml:"api"`
	Logger             LoggerConfig   `yaml:"logger"`
	Crypto             CryptoConfig   `yaml:"crypto"`
	ServerURL          string         `yaml:"serverUrl"`          // 客户端连接的服务器地址
	AdminInitialized   bool           `yaml:"adminInitialized"`   // 管理员是否已初始化
	ClientsDirectory   string         `yaml:"clientsDirectory"`   // 客户端工具目录
}

type ServerConfig struct {
	Port int    `yaml:"port"`
	Host string `yaml:"host"`
}
```

**Step 2: 添加默认配置值**

修改 `LoadConfig` 函数:
```go
func LoadConfig(path string) (*Config, error) {
	config := &Config{
		Server: ServerConfig{
			Port: 8080,
			Host: "0.0.0.0",
		},
		Database: DatabaseConfig{
			Path: "./data/versions.db",
		},
		Storage: StorageConfig{
			BasePath:     "./data/packages",
			MaxFileSize:  536870912, // 512MB
		},
		Logger: LoggerConfig{
			Level:  "info",
			Output: "both",
		},
		ClientsDirectory: "./clients",
		// 其他默认值...
	}

	// 加载配置文件（如果存在）
	if _, err := os.Stat(path); err == nil {
		data, err := os.ReadFile(path)
		if err != nil {
			return nil, err
		}
		if err := yaml.Unmarshal(data, config); err != nil {
			return nil, err
		}
	}

	return config, nil
}
```

**Step 3: 提交**

```bash
git add internal/config/config.go
git commit -m "feat: 扩展配置结构支持服务器URL和客户端目录"
```

---

## Task 3: 初始化流程服务

**Files:**
- Create: `internal/service/setup.go`

**Step 1: 创建初始化服务**

创建 `internal/service/setup.go`:
```go
package service

import (
	"crypto/rand"
	"encoding/base64"
	"errors"
	"time"
	"update-server/internal/models"
)

type SetupService struct {
	db *gorm.DB
}

func NewSetupService(db *gorm.DB) *SetupService {
	return &SetupService{db: db}
}

// IsInitialized 检查是否已初始化
func (s *SetupService) IsInitialized() (bool, error) {
	var count int64
	err := s.db.Model(&models.AdminUser{}).Count(&count).Error
	return count > 0, err
}

// CreateAdminUser 创建管理员用户
func (s *SetupService) CreateAdminUser(username, password string) (*models.AdminUser, error) {
	admin := &models.AdminUser{
		Username: username,
	}
	if err := admin.SetPassword(password); err != nil {
		return nil, err
	}

	if err := s.db.Create(admin).Error; err != nil {
		return nil, err
	}

	return admin, nil
}

// GenerateEncryptionKey 生成32字节随机密钥
func (s *SetupService) GenerateEncryptionKey() (string, error) {
	key := make([]byte, 32)
	if _, err := rand.Read(key); err != nil {
		return "", err
	}
	return base64.StdEncoding.EncodeToString(key), nil
}

// InitializeServer 初始化服务器配置
func (s *SetupService) InitializeServer(req InitializeRequest) error {
	// 检查是否已初始化
	initialized, err := s.IsInitialized()
	if err != nil {
		return err
	}
	if initialized {
		return errors.New("server already initialized")
	}

	// 创建管理员
	_, err = s.CreateAdminUser(req.Username, req.Password)
	if err != nil {
		return err
	}

	return nil
}

type InitializeRequest struct {
	Username string
	Password string
	ServerURL string
}
```

**Step 2: 提交**

```bash
git add internal/service/setup.go
git commit -m "feat: 添加初始化服务"
```

---

## Task 4: 程序服务扩展

**Files:**
- Modify: `internal/service/program.go`

**Step 1: 添加创建程序方法（含密钥和Token生成）**

修改 `internal/service/program.go`，添加:
```go
// CreateProgramWithOptions 创建程序并生成密钥和Token
func (s *ProgramService) CreateProgramWithOptions(req CreateProgramRequest) (*CreateProgramResponse, error) {
	// 创建程序
	program := &models.Program{
		ProgramID:   req.ProgramID,
		Name:        req.Name,
		Description: req.Description,
		IsActive:    true,
	}
	if err := s.db.Create(program).Error; err != nil {
		return nil, err
	}

	// 生成加密密钥
	encryptionKey, err := s.GenerateEncryptionKey()
	if err != nil {
		return nil, err
	}
	keyRecord := &models.EncryptionKey{
		ProgramID: program.ProgramID,
		KeyData:   encryptionKey,
	}
	if err := s.db.Create(keyRecord).Error; err != nil {
		return nil, err
	}

	// 生成上传Token
	uploadToken, err := s.tokenService.GenerateToken(program.ProgramID, "upload")
	if err != nil {
		return nil, err
	}

	// 生成下载Token
	downloadToken, err := s.tokenService.GenerateToken(program.ProgramID, "download")
	if err != nil {
		return nil, err
	}

	return &CreateProgramResponse{
		Program:        program,
		EncryptionKey:  encryptionKey,
		UploadToken:    uploadToken,
		DownloadToken:  downloadToken,
	}, nil
}

// GenerateEncryptionKey 生成32字节随机密钥
func (s *ProgramService) GenerateEncryptionKey() (string, error) {
	key := make([]byte, 32)
	if _, err := rand.Read(key); err != nil {
		return "", err
	}
	return base64.StdEncoding.EncodeToString(key), nil
}

// GetProgramEncryptionKey 获取程序的加密密钥
func (s *ProgramService) GetProgramEncryptionKey(programID string) (string, error) {
	var key models.EncryptionKey
	err := s.db.Where("program_id = ?", programID).First(&key).Error
	if err != nil {
		return "", err
	}
	return key.KeyData, nil
}

// RegenerateEncryptionKey 重新生成加密密钥
func (s *ProgramService) RegenerateEncryptionKey(programID string) (string, error) {
	newKey, err := s.GenerateEncryptionKey()
	if err != nil {
		return "", err
	}

	err = s.db.Model(&models.EncryptionKey{}).
		Where("program_id = ?", programID).
		Update("key_data", newKey).Error
	if err != nil {
		return "", err
	}

	return newKey, nil
}

type CreateProgramRequest struct {
	ProgramID   string
	Name        string
	Description string
}

type CreateProgramResponse struct {
	Program       *models.Program
	EncryptionKey string
	UploadToken   string
	DownloadToken string
}
```

**Step 2: 提交**

```bash
git add internal/service/program.go
git commit -m "feat: 扩展程序服务支持密钥和Token生成"
```

---

## Task 5: Web 前端资源

**Files:**
- Create: `web/embed.go`
- Create: `web/setup.html`
- Create: `web/admin.html`
- Create: `web/style.css`
- Create: `web/app.js`

**Step 1: 创建嵌入式文件系统**

创建 `web/embed.go`:
```go
package web

import "embed"

//go:embed *.html *.css *.js
var Files embed.FS
```

**Step 2: 创建初始化向导页面**

创建 `web/setup.html`:
```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Update Server 初始化</title>
    <link rel="stylesheet" href="/static/style.css">
</head>
<body>
    <div class="container">
        <div class="card setup-card">
            <h1>Update Server 初始化设置</h1>

            <!-- 步骤 1: 创建管理员 -->
            <div id="step1" class="step">
                <h2>步骤 1/3: 创建管理员账号</h2>
                <form id="adminForm">
                    <div class="form-group">
                        <label>管理员用户名</label>
                        <input type="text" id="username" name="username" required>
                    </div>
                    <div class="form-group">
                        <label>管理员密码</label>
                        <input type="password" id="password" name="password" required>
                    </div>
                    <div class="form-group">
                        <label>确认密码</label>
                        <input type="password" id="confirmPassword" name="confirmPassword" required>
                    </div>
                    <button type="submit">下一步</button>
                </form>
            </div>

            <!-- 步骤 2: 服务器配置 -->
            <div id="step2" class="step" style="display:none;">
                <h2>步骤 2/3: 服务器配置</h2>
                <form id="serverForm">
                    <div class="form-group">
                        <label>服务端口</label>
                        <input type="number" id="port" name="port" value="8080" required>
                    </div>
                    <div class="form-group">
                        <label>监听地址</label>
                        <input type="text" id="host" name="host" value="0.0.0.0" required>
                    </div>
                    <div class="form-group">
                        <label>服务器 URL (客户端连接地址)</label>
                        <input type="text" id="serverUrl" name="serverUrl"
                               placeholder="https://update.example.com" required>
                        <small>客户端将使用此地址连接服务器</small>
                    </div>
                    <div class="buttons">
                        <button type="button" onclick="showStep(1)">上一步</button>
                        <button type="submit">下一步</button>
                    </div>
                </form>
            </div>

            <!-- 步骤 3: 完成设置 -->
            <div id="step3" class="step" style="display:none;">
                <h2>步骤 3/3: 完成设置</h2>
                <div class="success">
                    <p>✓ 管理员账号已创建</p>
                    <p>✓ 数据库已初始化</p>
                    <p>✓ 服务器配置已保存</p>
                </div>
                <h3>服务器现在已就绪！</h3>
                <button onclick="window.location.href='/admin'">进入管理后台</button>
            </div>
        </div>
    </div>
    <script src="/static/app.js"></script>
</body>
</html>
```

**Step 3: 创建管理后台页面**

创建 `web/admin.html`:
```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <title>Update Server 管理后台</title>
    <link rel="stylesheet" href="/static/style.css">
</head>
<body>
    <div class="admin-layout">
        <nav class="sidebar">
            <h2>Update Server</h2>
            <ul>
                <li><a href="#" class="active">仪表盘</a></li>
                <li><a href="#programs">程序管理</a></li>
                <li><a href="#settings">系统设置</a></li>
                <li><a href="#" onclick="logout()">退出登录</a></li>
            </ul>
        </nav>

        <main class="content">
            <header>
                <h1>管理后台</h1>
                <div class="user-info">管理员</div>
            </header>

            <!-- 仪表盘 -->
            <div id="dashboard" class="view">
                <div class="stats">
                    <div class="stat-card">
                        <h3>总程序数</h3>
                        <div class="stat-value" id="totalPrograms">0</div>
                    </div>
                    <div class="stat-card">
                        <h3>总版本数</h3>
                        <div class="stat-value" id="totalVersions">0</div>
                    </div>
                    <div class="stat-card">
                        <h3>总下载数</h3>
                        <div class="stat-value" id="totalDownloads">0</div>
                    </div>
                </div>

                <div class="programs-section">
                    <h2>程序列表</h2>
                    <button onclick="showCreateProgram()">+ 创建新程序</button>
                    <div id="programList" class="program-list"></div>
                </div>
            </div>

            <!-- 程序详情 -->
            <div id="programDetail" class="view" style="display:none;">
                <button onclick="showDashboard()">← 返回程序列表</button>
                <h2 id="programTitle">程序详情</h2>

                <div class="program-info">
                    <h3>基本信息</h3>
                    <p>Program ID: <span id="detailProgramId"></span></p>
                    <p>创建时间: <span id="detailCreatedAt"></span></p>
                    <p>总下载: <span id="detailDownloads"></span></p>
                </div>

                <div class="client-download">
                    <h3>下载客户端工具</h3>
                    <button onclick="downloadPublishClient()">下载发布端</button>
                    <button onclick="downloadUpdateClient()">下载更新端</button>
                </div>

                <div class="token-management">
                    <h3>Token 管理</h3>
                    <div class="token-item">
                        <label>Upload Token:</label>
                        <code id="uploadToken"></code>
                        <button onclick="regenerateToken('upload')">重新生成</button>
                    </div>
                    <div class="token-item">
                        <label>Download Token:</label>
                        <code id="downloadToken"></code>
                        <button onclick="regenerateToken('download')">重新生成</button>
                    </div>
                    <div class="token-item">
                        <label>Encryption Key:</label>
                        <code id="encryptionKey"></code>
                        <button onclick="regenerateKey()">重新生成</button>
                    </div>
                </div>

                <div class="versions-section">
                    <h3>版本列表</h3>
                    <div id="versionList"></div>
                </div>
            </div>
        </main>
    </div>
    <script src="/static/app.js"></script>
</body>
</html>
```

**Step 4: 创建样式文件**

创建 `web/style.css`:
```css
* { margin: 0; padding: 0; box-sizing: border-box; }

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
    background: #f5f5f5;
}

.container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 20px;
}

.setup-card {
    background: white;
    padding: 40px;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    width: 100%;
    max-width: 500px;
}

.setup-card h1 {
    margin-bottom: 30px;
    text-align: center;
    color: #333;
}

.step h2 {
    margin-bottom: 20px;
    color: #555;
}

.form-group {
    margin-bottom: 20px;
}

.form-group label {
    display: block;
    margin-bottom: 5px;
    color: #666;
}

.form-group input {
    width: 100%;
    padding: 10px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
}

.form-group small {
    display: block;
    margin-top: 5px;
    color: #999;
    font-size: 12px;
}

button {
    background: #007bff;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

button:hover {
    background: #0056b3;
}

.buttons {
    display: flex;
    gap: 10px;
}

.success {
    background: #d4edda;
    border: 1px solid #c3e6cb;
    border-radius: 4px;
    padding: 15px;
    margin-bottom: 20px;
}

.success p {
    color: #155724;
    margin: 5px 0;
}

/* Admin Layout */
.admin-layout {
    display: flex;
    min-height: 100vh;
}

.sidebar {
    width: 250px;
    background: #2c3e50;
    color: white;
    padding: 20px 0;
}

.sidebar h2 {
    padding: 0 20px;
    margin-bottom: 30px;
}

.sidebar ul {
    list-style: none;
}

.sidebar li {
    padding: 0 20px;
    margin-bottom: 5px;
}

.sidebar a {
    display: block;
    color: #ecf0f1;
    text-decoration: none;
    padding: 10px;
    border-radius: 4px;
}

.sidebar a:hover,
.sidebar a.active {
    background: #34495e;
}

.content {
    flex: 1;
    padding: 30px;
}

.content header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 30px;
}

.stats {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 20px;
    margin-bottom: 30px;
}

.stat-card {
    background: white;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.stat-card h3 {
    color: #666;
    font-size: 14px;
    margin-bottom: 10px;
}

.stat-value {
    font-size: 32px;
    font-weight: bold;
    color: #333;
}

.program-list {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.program-item {
    padding: 20px;
    border-bottom: 1px solid #eee;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.program-item:last-child {
    border-bottom: none;
}

.token-item {
    background: #f8f9fa;
    padding: 15px;
    border-radius: 4px;
    margin-bottom: 10px;
}

.token-item code {
    background: white;
    padding: 5px 10px;
    border-radius: 4px;
    font-family: monospace;
    display: inline-block;
    margin: 0 10px;
}
```

**Step 5: 创建前端逻辑**

创建 `web/app.js`:
```javascript
// API 基础路径
const API_BASE = '/api';

// 初始化向导
function showStep(step) {
    document.querySelectorAll('.step').forEach(s => s.style.display = 'none');
    document.getElementById('step' + step).style.display = 'block';
}

document.getElementById('adminForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (password !== confirmPassword) {
        alert('两次输入的密码不一致');
        return;
    }

    // 暂存数据，最后一步一起提交
    window.setupData = { username, password };
    showStep(2);
});

document.getElementById('serverForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const port = document.getElementById('port').value;
    const host = document.getElementById('host').value;
    const serverUrl = document.getElementById('serverUrl').value;

    try {
        const response = await fetch(`${API_BASE}/setup/initialize`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: window.setupData.username,
                password: window.setupData.password,
                serverUrl: serverUrl,
                port: parseInt(port),
                host: host
            })
        });

        if (!response.ok) throw new Error('初始化失败');

        showStep(3);
    } catch (error) {
        alert('初始化失败: ' + error.message);
    }
});

// 管理后台
async function loadDashboard() {
    const response = await fetch(`${API_BASE}/admin/stats`);
    const stats = await response.json();

    document.getElementById('totalPrograms').textContent = stats.totalPrograms;
    document.getElementById('totalVersions').textContent = stats.totalVersions;
    document.getElementById('totalDownloads').textContent = stats.totalDownloads;

    loadPrograms();
}

async function loadPrograms() {
    const response = await fetch(`${API_BASE}/admin/programs`);
    const programs = await response.json();

    const list = document.getElementById('programList');
    list.innerHTML = programs.map(p => `
        <div class="program-item">
            <div>
                <strong>${p.name}</strong>
                <br>programId: ${p.programId}
                <br>版本: ${p.versionCount} | 下载: ${p.downloadCount}
            </div>
            <div>
                <button onclick="downloadPublishClient('${p.programId}')">下载发布端</button>
                <button onclick="downloadUpdateClient('${p.programId}')">下载更新端</button>
                <button onclick="showProgramDetail('${p.programId}')">管理</button>
                <button onclick="deleteProgram('${p.programId}')">删除</button>
            </div>
        </div>
    `).join('');
}

async function showProgramDetail(programId) {
    const response = await fetch(`${API_BASE}/admin/programs/${programId}`);
    const program = await response.json();

    document.getElementById('dashboard').style.display = 'none';
    document.getElementById('programDetail').style.display = 'block';
    document.getElementById('programTitle').textContent = program.name;
    document.getElementById('detailProgramId').textContent = program.programId;
    document.getElementById('detailCreatedAt').textContent = new Date(program.createdAt).toLocaleString();
    document.getElementById('detailDownloads').textContent = program.downloadCount;
    document.getElementById('uploadToken').textContent = program.uploadToken;
    document.getElementById('downloadToken').textContent = program.downloadToken;
    document.getElementById('encryptionKey').textContent = program.encryptionKey;

    loadVersions(programId);
}

function showDashboard() {
    document.getElementById('dashboard').style.display = 'block';
    document.getElementById('programDetail').style.display = 'none';
}

async function downloadPublishClient(programId) {
    window.location.href = `${API_BASE}/admin/programs/${programId}/download/publish-client`;
}

async function downloadUpdateClient(programId) {
    window.location.href = `${API_BASE}/admin/programs/${programId}/download/update-client`;
}

async function regenerateToken(type) {
    if (!confirm('确定要重新生成 Token 吗？')) return;
    // 实现重新生成逻辑
}

async function regenerateKey() {
    if (!confirm('确定要重新生成加密密钥吗？')) return;
    // 实现重新生成密钥逻辑
}

function logout() {
    window.location.href = '/admin/logout';
}

// 页面加载时初始化
if (document.getElementById('dashboard')) {
    loadDashboard();
}
```

**Step 6: 提交**

```bash
git add web/
git commit -m "feat: 添加 Web 前端资源"
```

---

## Task 6: API 处理器 - 初始化和认证

**Files:**
- Create: `internal/handler/setup.go`
- Create: `internal/handler/auth.go`

**Step 1: 创建初始化处理器**

创建 `internal/handler/setup.go`:
```go
package handler

import (
	"net/http"
	"update-server/internal/service"
	"github.com/gin-gonic/gin"
)

type SetupHandler struct {
	setupService *service.SetupService
}

func NewSetupHandler(setupService *service.SetupService) *SetupHandler {
	return &SetupHandler{setupService: setupService}
}

// CheckInitStatus 检查初始化状态
func (h *SetupHandler) CheckInitStatus(c *gin.Context) {
	initialized, err := h.setupService.IsInitialized()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, gin.H{"initialized": initialized})
}

// Initialize 初始化服务器
func (h *SetupHandler) Initialize(c *gin.Context) {
	var req service.InitializeRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	if err := h.setupService.InitializeServer(req); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"success": true})
}
```

**Step 2: 创建认证处理器**

创建 `internal/handler/auth.go`:
```go
package handler

import (
	"net/http"
	"update-server/internal/models"
	"update-server/internal/service"
	"github.com/gin-gonic/gin"
)

type AuthHandler struct {
	db *gorm.DB
}

func NewAuthHandler(db *gorm.DB) *AuthHandler {
	return &AuthHandler{db: db}
}

type LoginRequest struct {
	Username string `json:"username" binding:"required"`
	Password string `json:"password" binding:"required"`
}

// Login 管理员登录
func (h *AuthHandler) Login(c *gin.Context) {
	var req LoginRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	var admin models.AdminUser
	if err := h.db.Where("username = ?", req.Username).First(&admin).Error; err != nil {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "用户名或密码错误"})
		return
	}

	if !admin.CheckPassword(req.Password) {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "用户名或密码错误"})
		return
	}

	// 设置 session
	c.SetCookie("admin_session", admin.Username, 3600*24, "/", "", false, true)
	c.JSON(http.StatusOK, gin.H{"success": true})
}

// Logout 退出登录
func (h *AuthHandler) Logout(c *gin.Context) {
	c.SetCookie("admin_session", "", -1, "/", "", false, true)
	c.Redirect(http.StatusFound, "/")
}

// AuthMiddleware 管理员认证中间件
func AuthMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		session, err := c.Cookie("admin_session")
		if err != nil || session == "" {
			if c.Request.URL.Path != "/login" {
				c.Redirect(http.StatusFound, "/login")
				c.Abort()
				return
			}
		}
		c.Next()
	}
}
```

**Step 3: 提交**

```bash
git add internal/handler/setup.go internal/handler/auth.go
git commit -m "feat: 添加初始化和认证处理器"
```

---

## Task 7: API 处理器 - 程序管理

**Files:**
- Create: `internal/handler/admin.go`

**Step 1: 创建管理员 API 处理器**

创建 `internal/handler/admin.go`:
```go
package handler

import (
	"net/http"
	"update-server/internal/service"
	"github.com/gin-gonic/gin"
)

type AdminHandler struct {
	programService *service.ProgramService
	versionService *service.VersionService
	tokenService   *service.TokenService
	setupService   *service.SetupService
}

func NewAdminHandler(
	programService *service.ProgramService,
	versionService *service.VersionService,
	tokenService *service.TokenService,
	setupService *service.SetupService,
) *AdminHandler {
	return &AdminHandler{
		programService: programService,
		versionService: versionService,
		tokenService:   tokenService,
		setupService:   setupService,
	}
}

// GetStats 获取统计数据
func (h *AdminHandler) GetStats(c *gin.Context) {
	stats, err := h.programService.GetStats()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, stats)
}

// ListPrograms 列出所有程序
func (h *AdminHandler) ListPrograms(c *gin.Context) {
	programs, err := h.programService.ListAll()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, programs)
}

// CreateProgram 创建新程序
func (h *AdminHandler) CreateProgram(c *gin.Context) {
	var req service.CreateProgramRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	result, err := h.programService.CreateProgramWithOptions(req)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusCreated, result)
}

// GetProgramDetail 获取程序详情
func (h *AdminHandler) GetProgramDetail(c *gin.Context) {
	programID := c.Param("programId")

	program, err := h.programService.GetByProgramID(programID)
	if err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": "程序不存在"})
		return
	}

	// 获取加密密钥
	encryptionKey, _ := h.programService.GetProgramEncryptionKey(programID)

	// 获取 Token
	uploadToken, _ := h.tokenService.GetToken(programID, "upload")
	downloadToken, _ := h.tokenService.GetToken(programID, "download")

	c.JSON(http.StatusOK, gin.H{
		"program":       program,
		"encryptionKey": encryptionKey,
		"uploadToken":   uploadToken,
		"downloadToken": downloadToken,
	})
}

// DeleteProgram 删除程序
func (h *AdminHandler) DeleteProgram(c *gin.Context) {
	programID := c.Param("programId")

	if err := h.programService.Delete(programID); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"success": true})
}

// ListVersions 列出版本
func (h *AdminHandler) ListVersions(c *gin.Context) {
	programID := c.Param("programId")

	versions, err := h.versionService.ListByProgramID(programID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, versions)
}

// DeleteVersion 删除版本
func (h *AdminHandler) DeleteVersion(c *gin.Context) {
	programID := c.Param("programId")
	version := c.Param("version")

	if err := h.versionService.Delete(programID, version); err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"success": true})
}
```

**Step 2: 提交**

```bash
git add internal/handler/admin.go
git commit -m "feat: 添加管理员 API 处理器"
```

---

## Task 8: 客户端工具打包服务

**Files:**
- Create: `internal/service/client_packager.go`

**Step 1: 创建客户端打包服务**

创建 `internal/service/client_packager.go`:
```go
package service

import (
	"archive/zip"
	"bytes"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"
	"update-server/internal/config"
)

type ClientPackagerService struct {
	config        *config.Config
	programService *ProgramService
	tokenService   *TokenService
}

func NewClientPackagerService(
	cfg *config.Config,
	programService *ProgramService,
	tokenService *TokenService,
) *ClientPackagerService {
	return &ClientPackagerService{
		config:         cfg,
		programService: programService,
		tokenService:   tokenService,
	}
}

// GeneratePublishClient 生成发布客户端包
func (s *ClientPackagerService) GeneratePublishClient(programID string) ([]byte, error) {
	// 获取程序信息
	program, err := s.programService.GetByProgramID(programID)
	if err != nil {
		return nil, err
	}

	// 获取加密密钥和 Token
	encryptionKey, err := s.programService.GetProgramEncryptionKey(programID)
	if err != nil {
		return nil, err
	}

	uploadToken, err := s.tokenService.GetToken(programID, "upload")
	if err != nil {
		return nil, err
	}

	// 生成配置文件内容
	configContent := fmt.Sprintf(`# Update Server 发布客户端配置
server: "%s"
programId: "%s"

# 认证配置
uploadToken: "%s"

# 加密配置
encryption:
  enabled: true
  key: "%s"

# 文件配置
file: "./app-v1.0.0.zip"
version: "1.0.0"
channel: "stable"
changelog: "更新说明"
`, s.config.ServerURL, programID, uploadToken, encryptionKey)

	// 创建 ZIP
	var buf bytes.Buffer
	zipWriter := zip.NewWriter(&buf)

	// 添加 publish-client.exe
	exePath := filepath.Join(s.config.ClientsDirectory, "publish-client.exe")
	if err := s.addFileToZip(zipWriter, exePath, "publish-client.exe"); err != nil {
		return nil, err
	}

	// 添加配置文件
	if err := s.addContentToZip(zipWriter, []byte(configContent), "publish-config.yaml"); err != nil {
		return nil, err
	}

	// 添加 README
	readme := []byte(`# Update Server 发布客户端

## 使用方法

1. 编辑 publish-config.yaml:
   - file: 设置要上传的文件路径
   - version: 设置版本号
   - channel: 设置发布渠道 (stable/beta)
   - changelog: 设置更新说明

2. 运行发布客户端:
   publish-client.exe

3. 文件将被加密后上传到服务器
`)
	if err := s.addContentToZip(zipWriter, readme, "README.md"); err != nil {
		return nil, err
	}

	if err := zipWriter.Close(); err != nil {
		return nil, err
	}

	return buf.Bytes(), nil
}

// GenerateUpdateClient 生成更新客户端包
func (s *ClientPackagerService) GenerateUpdateClient(programID string) ([]byte, error) {
	// 获取程序信息
	_, err := s.programService.GetByProgramID(programID)
	if err != nil {
		return nil, err
	}

	// 获取加密密钥和 Token
	encryptionKey, err := s.programService.GetProgramEncryptionKey(programID)
	if err != nil {
		return nil, err
	}

	downloadToken, err := s.tokenService.GetToken(programID, "download")
	if err != nil {
		return nil, err
	}

	// 生成配置文件内容
	configContent := fmt.Sprintf(`# Update Server 更新客户端配置
server: "%s"
programId: "%s"

# 认证配置
downloadToken: "%s"

# 加密配置
encryption:
  enabled: true
  key: "%s"

# 更新检查配置
check:
  channel: "stable"
  autoDownload: true
  interval: 86400

# 下载配置
download:
  outputPath: "./updates"
  verifyChecksum: true
`, s.config.ServerURL, programID, downloadToken, encryptionKey)

	// 创建 ZIP
	var buf bytes.Buffer
	zipWriter := zip.NewWriter(&buf)

	// 添加 update-client.exe
	exePath := filepath.Join(s.config.ClientsDirectory, "update-client.exe")
	if err := s.addFileToZip(zipWriter, exePath, "update-client.exe"); err != nil {
		return nil, err
	}

	// 添加配置文件
	if err := s.addContentToZip(zipWriter, []byte(configContent), "update-config.yaml"); err != nil {
		return nil, err
	}

	// 添加 README
	readme := []byte(`# Update Server 更新客户端

## 使用方法

1. 检查更新:
   update-client.exe --check

2. 下载并安装更新:
   update-client.exe --update

3. 集成到您的程序:
   - 在程序启动时运行 update-client.exe --check
   - 根据返回值决定是否提示用户更新
`)
	if err := s.addContentToZip(zipWriter, readme, "README.md"); err != nil {
		return nil, err
	}

	if err := zipWriter.Close(); err != nil {
		return nil, err
	}

	return buf.Bytes(), nil
}

func (s *ClientPackagerService) addFileToZip(zipWriter *zip.Writer, filePath, filename string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	info, err := file.Stat()
	if err != nil {
		return err
	}

	header, err := zip.FileInfoHeader(info)
	if err != nil {
		return err
	}
	header.Name = filename
	header.Method = zip.Deflate

	writer, err := zipWriter.CreateHeader(header)
	if err != nil {
		return err
	}

	_, err = io.Copy(writer, file)
	return err
}

func (s *ClientPackagerService) addContentToZip(zipWriter *zip.Writer, content []byte, filename string) error {
	writer, err := zipWriter.Create(filename)
	if err != nil {
		return err
	}
	_, err = writer.Write(content)
	return err
}
```

**Step 2: 提交**

```bash
git add internal/service/client_packager.go
git commit -m "feat: 添加客户端打包服务"
```

---

## Task 9: 客户端下载 API

**Files:**
- Modify: `internal/handler/admin.go`

**Step 1: 添加客户端下载处理器**

在 `internal/handler/admin.go` 中添加:
```go
type AdminHandler struct {
	programService       *service.ProgramService
	versionService       *service.VersionService
	tokenService         *service.TokenService
	setupService         *service.SetupService
	clientPackagerService *service.ClientPackagerService
}

func NewAdminHandler(
	programService *service.ProgramService,
	versionService *service.VersionService,
	tokenService *service.TokenService,
	setupService *service.SetupService,
	clientPackagerService *service.ClientPackagerService,
) *AdminHandler {
	return &AdminHandler{
		programService:        programService,
		versionService:        versionService,
		tokenService:          tokenService,
		setupService:          setupService,
		clientPackagerService: clientPackagerService,
	}
}

// DownloadPublishClient 下载发布客户端
func (h *AdminHandler) DownloadPublishClient(c *gin.Context) {
	programID := c.Param("programId")

	data, err := h.clientPackagerService.GeneratePublishClient(programID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	filename := fmt.Sprintf("%s-publish-client.zip", programID)
	c.Header("Content-Disposition", fmt.Sprintf("attachment; filename=\"%s\"", filename))
	c.Data(http.StatusOK, "application/zip", data)
}

// DownloadUpdateClient 下载更新客户端
func (h *AdminHandler) DownloadUpdateClient(c *gin.Context) {
	programID := c.Param("programId")

	data, err := h.clientPackagerService.GenerateUpdateClient(programID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	filename := fmt.Sprintf("%s-update-client.zip", programID)
	c.Header("Content-Disposition", fmt.Sprintf("attachment; filename=\"%s\"", filename))
	c.Data(http.StatusOK, "application/zip", data)
}

// RegenerateToken 重新生成 Token
func (h *AdminHandler) RegenerateToken(c *gin.Context) {
	programID := c.Param("programId")
	tokenType := c.Query("type") // upload or download

	newToken, err := h.tokenService.RegenerateToken(programID, tokenType)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"token": newToken})
}

// RegenerateEncryptionKey 重新生成加密密钥
func (h *AdminHandler) RegenerateEncryptionKey(c *gin.Context) {
	programID := c.Param("programId")

	newKey, err := h.programService.RegenerateEncryptionKey(programID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"encryptionKey": newKey})
}
```

**Step 2: 提交**

```bash
git add internal/handler/admin.go
git commit -m "feat: 添加客户端下载和 Token/密钥重新生成 API"
```

---

## Task 10: 路由配置

**Files:**
- Modify: `main.go`

**Step 1: 更新主路由配置**

修改 `main.go`:
```go
package main

import (
	"embed"
	"fmt"
	"update-server/internal/config"
	"update-server/internal/database"
	"update-server/internal/handler"
	"update-server/internal/logger"
	"update-server/internal/middleware"
	"update-server/internal/service"
	"update-server/web"
	"github.com/gin-gonic/gin"
)

//go:embed config.yaml
var defaultConfig embed.FS

func main() {
	// 加载配置
	cfg, err := config.LoadConfig("config.yaml")
	if err != nil {
		panic(err)
	}

	// 初始化日志
	log := logger.New(cfg.Logger)

	// 连接数据库
	db, err := database.Connect(cfg.Database)
	if err != nil {
		log.Fatal("数据库连接失败: " + err.Error())
	}

	// 自动迁移
	if err := database.AutoMigrate(db); err != nil {
		log.Fatal("数据库迁移失败: " + err.Error())
	}

	// 初始化服务
	setupService := service.NewSetupService(db)
	tokenService := service.NewTokenService(db)
	programService := service.NewProgramService(db, tokenService)
	versionService := service.NewVersionService(db)
	clientPackagerService := service.NewClientPackagerService(cfg, programService, tokenService)

	// 初始化处理器
	setupHandler := handler.NewSetupHandler(setupService)
	authHandler := handler.NewAuthHandler(db)
	adminHandler := handler.NewAdminHandler(programService, versionService, tokenService, setupService, clientPackagerService)

	// 检查是否已初始化
	initialized, _ := setupService.IsInitialized()

	// 创建 Gin 路由
	r := gin.Default()

	// 静态文件
	r.Static("/static", "web/dist")
	r.LoadHTMLGlob("web/dist/*.html")

	// 健康检查
	r.GET("/api/health", func(c *gin.Context) {
		c.JSON(200, gin.H{"status": "ok"})
	})

	// 初始化相关路由（未初始化时才可用）
	if !initialized {
		r.GET("/api/setup/status", setupHandler.CheckInitStatus)
		r.POST("/api/setup/initialize", setupHandler.Initialize)
		r.GET("/setup", func(c *gin.Context) {
			c.HTML(200, "setup.html", nil)
		})
	}

	// 登录
	r.GET("/login", func(c *gin.Context) {
		c.HTML(200, "login.html", nil)
	})
	r.POST("/api/auth/login", authHandler.Login)
	r.GET("/admin/logout", authHandler.Logout)

	// 管理后台路由（需要认证）
	adminGroup := r.Group("/admin")
	adminGroup.Use(handler.AuthMiddleware())
	{
		adminGroup.GET("", func(c *gin.Context) {
			c.HTML(200, "admin.html", nil)
		})

		// API 路由
		api := adminGroup.Group("/api/admin")
		{
			api.GET("/stats", adminHandler.GetStats)
			api.GET("/programs", adminHandler.ListPrograms)
			api.POST("/programs", adminHandler.CreateProgram)
			api.GET("/programs/:programId", adminHandler.GetProgramDetail)
			api.DELETE("/programs/:programId", adminHandler.DeleteProgram)
			api.GET("/programs/:programId/versions", adminHandler.ListVersions)
			api.DELETE("/programs/:programId/versions/:version", adminHandler.DeleteVersion)
			api.GET("/programs/:programId/download/publish-client", adminHandler.DownloadPublishClient)
			api.GET("/programs/:programId/download/update-client", adminHandler.DownloadUpdateClient)
			api.POST("/programs/:programId/tokens/regenerate", adminHandler.RegenerateToken)
			api.POST("/programs/:programId/keys/regenerate", adminHandler.RegenerateEncryptionKey)
		}
	}

	// 现有的公开 API 路由...
	// （保留原有的 version、program handler 路由）

	// 启动服务器
	addr := fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port)
	log.Info("服务器启动在 " + addr)

	if err := r.Run(addr); err != nil {
		log.Fatal("服务器启动失败: " + err.Error())
	}
}
```

**Step 2: 提交**

```bash
git add main.go
git commit -m "feat: 更新路由配置支持初始化和管理功能"
```

---

## Task 11: 编写文档

**Files:**
- Create: `docs/README.md`
- Create: `docs/ARCHITECTURE.md`

**Step 1: 创建快速指南文档**

创建 `docs/README.md`:
```markdown
# Update Server 快速指南

## 一、部署服务器

1. 解压 `update-server.zip` 到服务器目录
2. 运行 `update-server.exe`
3. 浏览器自动打开初始化向导，按提示完成配置：
   - 创建管理员账号
   - 设置服务端口和服务器 URL
4. 完成后自动进入管理后台

## 二、创建程序

1. 登录管理后台 → 程序管理 → 创建新程序
2. 填写 Program ID（如：docufiller）
3. 系统自动生成密钥和 Token
4. 记录显示的信息（或稍后在程序详情页查看）

## 三、发布更新

1. 在程序详情页下载 `docufiller-publish-client.zip`
2. 解压并编辑 `publish-config.yaml`：
   ```yaml
   file: "./docufiller-v1.2.0.zip"
   version: "1.2.0"
   channel: "stable"
   changelog: "更新说明"
   ```
3. 运行 `publish-client.exe`

## 四、集成更新功能

1. 在程序详情页下载 `docufiller-update-client.zip`
2. 解压到您的项目
3. 程序启动时执行 `update-client.exe --check`
4. 根据返回结果处理更新

## 常见问题

**Q: 修改服务器地址？**
A: 管理后台 → 系统设置 → 修改服务器 URL

**Q: 重新生成 Token？**
A: 程序详情页 → Token 管理 → 重新生成
```

**Step 2: 创建架构文档**

创建 `docs/ARCHITECTURE.md`:
```markdown
# Update Server 架构说明

## 系统架构

三层架构：
- **update-server**：中央更新服务器（Go + Gin）
- **publish-client**：发布端（Go CLI）
- **update-client**：更新端（Go CLI）

## 认证机制

- Token 认证：Upload Token / Download Token
- Token 在 Web 界面生成和管理
- 通过 HTTP Header 传递

## 加密机制

端到端加密：
- 每个程序独立的 32 字节密钥
- AES-256-GCM 加密算法
- publish-client 上传前加密
- update-client 下载后解密
- 服务器只存储加密文件

## 数据模型

- programs：程序元数据
- versions：版本信息
- tokens：认证令牌
- encryption_keys：加密密钥
- admin_users：管理员账号

## 技术栈

- 后端：Go 1.23+ / Gin / GORM / SQLite
- 前端：嵌入式单页应用
- 加密：AES-256-GCM / HKDF
```

**Step 3: 提交**

```bash
git add docs/README.md docs/ARCHITECTURE.md
git commit -m "docs: 添加使用指南和架构文档"
```

---

## Task 12: 测试和验证

**Files:**
- None (运行测试)

**Step 1: 运行集成测试**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server
go test ./tests/...
```

**Step 2: 手动测试初始化流程**

```bash
# 删除数据库重新初始化
rm data/versions.db
./update-server.exe
# 浏览器应自动打开初始化向导
```

**Step 3: 测试程序创建和客户端下载**

```bash
# 登录管理后台
# 创建新程序
# 下载发布端和更新端
# 验证配置文件内容正确
```

**Step 4: 测试发布流程**

```bash
# 解压下载的发布端
# 编辑配置上传测试文件
# 验证文件被加密存储
```

**Step 5: 提交测试结果**

```bash
# 如果测试通过，添加标签
git tag -a v2.0.0 -m "简化重构版本"
git push origin v2.0.0
```

---

## 实施顺序建议

1. **阶段一（核心）**：Task 1-3
   - 数据库模型
   - 配置扩展
   - 初始化服务

2. **阶段二（服务）**：Task 4-5
   - 程序服务扩展
   - 客户端打包服务

3. **阶段三（Web）**：Task 6-7
   - Web 前端资源
   - API 处理器

4. **阶段四（集成）**：Task 8-10
   - 客户端下载 API
   - 路由配置

5. **阶段五（文档）**：Task 11-12
   - 编写文档
   - 测试验证

---

## 注意事项

1. **保持向后兼容**：现有的 API 路由继续工作
2. **安全性**：密码使用 bcrypt 哈希，Token 只存哈希值
3. **错误处理**：所有 API 返回统一的错误格式
4. **日志记录**：关键操作记录日志
5. **数据迁移**：为现有系统提供数据迁移脚本
