# DocuFiller 自动更新系统实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-step.

**Goal:** 构建 DocuFiller WPF 应用的自动更新系统，包括 Go 更新服务器、发布脚本和客户端更新组件。

**架构:**
- Go 服务器提供 REST API 管理版本信息和文件分发，使用 GORM + SQLite 存储
- BAT 脚本实现编译和发布自动化
- WPF 客户端集成更新服务，支持启动检查、后台下载和用户确认安装

**Tech Stack:**
- Go 1.21+, Gin, GORM, SQLite, github.com/WQGroup/logger
- C# .NET 8, WPF, HttpClient
- BAT 脚本, curl

---

## Task 1: 创建 Go 服务器项目结构

**Files:**
- Create: `docufiller-update-server/go.mod`
- Create: `docufiller-update-server/main.go`
- Create: `docufiller-update-server/config.yaml`
- Create: `docufiller-update-server/Makefile`

**Step 1: 初始化 Go 模块**

创建 `docufiller-update-server/go.mod`:

```go
module docufiller-update-server

go 1.21

require (
    github.com/WQGroup/logger v0.0.0
    github.com/gin-gonic/gin v1.9.1
    gorm.io/gorm v1.25.5
    gorm.io/driver/sqlite v1.25.4
    gopkg.in/yaml.v3 v3.0.1
)
```

**Step 2: 创建主程序入口**

创建 `docufiller-update-server/main.go`:

```go
package main

import (
    "fmt"
    "log"

    "github.com/gin-gonic/gin"
    "docufiller-update-server/internal/config"
    "docufiller-update-server/internal/database"
    "docufiller-update-server/internal/handler"
    "docufiller-update-server/internal/logger"
    "docufiller-update-server/internal/middleware"
    "docufiller-update-server/internal/models"
)

func main() {
    // 加载配置
    cfg, err := config.Load("config.yaml")
    if err != nil {
        log.Fatalf("Failed to load config: %v", err)
    }

    // 初始化日志
    if err := logger.Init(cfg.Logger); err != nil {
        log.Fatalf("Failed to init logger: %v", err)
    }
    logger.Info("Starting DocuFiller Update Server...")

    // 初始化数据库
    db, err := database.NewGORM(cfg.Database.Path)
    if err != nil {
        logger.Fatalf("Failed to connect to database: %v", err)
    }

    // 自动迁移
    if err := db.AutoMigrate(&models.Version{}); err != nil {
        logger.Fatalf("Failed to migrate database: %v", err)
    }

    // 初始化认证中间件
    middleware.InitAuth(cfg.API.UploadToken)

    // 设置 Gin
    if cfg.Logger.Level != "debug" {
        gin.SetMode(gin.ReleaseMode)
    }
    r := gin.Default()

    // 注册路由
    setupRoutes(r, db)

    // 启动服务器
    addr := fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port)
    logger.Infof("Server listening on %s", addr)
    if err := r.Run(addr); err != nil {
        logger.Fatalf("Failed to start server: %v", err)
    }
}

func setupRoutes(r *gin.Engine, db *gorm.DB) {
    // 健康检查
    r.GET("/api/health", func(c *gin.Context) {
        c.JSON(200, gin.H{"status": "ok"})
    })

    // 版本相关路由
    versionHandler := handler.NewVersionHandler(db)
    api := r.Group("/api")
    {
        api.GET("/version/latest", versionHandler.GetLatestVersion)
        api.GET("/version/list", versionHandler.GetVersionList)
        api.GET("/version/:channel/:version", versionHandler.GetVersionDetail)
        api.POST("/version/upload", middleware.AuthMiddleware(), versionHandler.UploadVersion)
        api.DELETE("/version/:channel/:version", middleware.AuthMiddleware(), versionHandler.DeleteVersion)
        api.GET("/download/:channel/:version", versionHandler.DownloadFile)
    }
}
```

**Step 3: 创建配置文件**

创建 `docufiller-update-server/config.yaml`:

```yaml
server:
  port: 8080
  host: "0.0.0.0"

database:
  path: "./data/versions.db"

storage:
  basePath: "./data/packages"
  maxFileSize: 536870912  # 512MB

api:
  uploadToken: "change-this-token-in-production"
  corsEnable: true

logger:
  level: "info"
  output: "both"
  filePath: "./logs/server.log"
  maxSize: 10485760
  maxBackups: 5
  maxAge: 30
  compress: true
```

**Step 4: 创建 Makefile**

创建 `docufiller-update-server/Makefile`:

```makefile
.PHONY: build run clean

build:
    go build -o bin/docufiller-update-server.exe .

run:
    go run main.go

clean:
    rm -rf bin/ data/ logs/

install-deps:
    go mod tidy
    go mod download
```

**Step 5: 提交**

```bash
cd docufiller-update-server
git init
git add go.mod main.go config.yaml Makefile
git commit -m "feat: initialize Go server project structure"
```

---

## Task 2: 实现配置加载模块

**Files:**
- Create: `docufiller-update-server/internal/config/config.go`

**Step 1: 创建配置结构体和加载函数**

创建 `docufiller-update-server/internal/config/config.go`:

```go
package config

import (
    "os"
    "path/filepath"

    "gopkg.in/yaml.v3"
)

type Config struct {
    Server   ServerConfig   `yaml:"server"`
    Database DatabaseConfig `yaml:"database"`
    Storage  StorageConfig  `yaml:"storage"`
    API      APIConfig      `yaml:"api"`
    Logger   LoggerConfig   `yaml:"logger"`
}

type ServerConfig struct {
    Port int    `yaml:"port"`
    Host string `yaml:"host"`
}

type DatabaseConfig struct {
    Path string `yaml:"path"`
}

type StorageConfig struct {
    BasePath    string `yaml:"basePath"`
    MaxFileSize int64  `yaml:"maxFileSize"`
}

type APIConfig struct {
    UploadToken string `yaml:"uploadToken"`
    CorsEnable  bool   `yaml:"corsEnable"`
}

type LoggerConfig struct {
    Level      string `yaml:"level"`
    Output     string `yaml:"output"`
    FilePath   string `yaml:"filePath"`
    MaxSize    int64  `yaml:"maxSize"`
    MaxBackups int    `yaml:"maxBackups"`
    MaxAge     int    `yaml:"maxAge"`
    Compress   bool   `yaml:"compress"`
}

func Load(path string) (*Config, error) {
    data, err := os.ReadFile(path)
    if err != nil {
        return nil, err
    }

    var cfg Config
    if err := yaml.Unmarshal(data, &cfg); err != nil {
        return nil, err
    }

    // 确保路径是绝对路径
    if !filepath.IsAbs(cfg.Database.Path) {
        cfg.Database.Path = absPath(cfg.Database.Path)
    }
    if !filepath.IsAbs(cfg.Storage.BasePath) {
        cfg.Storage.BasePath = absPath(cfg.Storage.BasePath)
    }
    if !filepath.IsAbs(cfg.Logger.FilePath) {
        cfg.Logger.FilePath = absPath(cfg.Logger.FilePath)
    }

    return &cfg, nil
}

func absPath(p string) string {
    abs, err := filepath.Abs(p)
    if err != nil {
        return p
    }
    return abs
}
```

**Step 2: 提交**

```bash
git add internal/config/config.go
git commit -m "feat: add config loading module"
```

---

## Task 3: 实现日志模块

**Files:**
- Create: `docufiller-update-server/internal/logger/logger.go`
- Modify: `docufiller-update-server/go.mod`

**Step 1: 更新依赖**

修改 `docufiller-update-server/go.mod`，添加日志轮转依赖:

```go
require (
    github.com/WQGroup/logger v0.0.0
    github.com/gin-gonic/gin v1.9.1
    gorm.io/gorm v1.25.5
    gorm.io/driver/sqlite v1.25.4
    gopkg.in/yaml.v3 v3.0.1
    gopkg.in/natefinch/lumberjack.v2 v2.2.1
)
```

**Step 2: 创建日志初始化模块**

创建 `docufiller-update-server/internal/logger/logger.go`:

```go
package logger

import (
    "io"
    "os"
    "path/filepath"

    applog "github.com/WQGroup/logger"
    "gopkg.in/natefinch/lumberjack.v2"
)

var Log applog.Logger

// Init 初始化日志系统
func Init(cfg LoggerConfig) error {
    var writers []io.Writer

    // 控制台输出
    if cfg.Output == "console" || cfg.Output == "both" {
        writers = append(writers, os.Stdout)
    }

    // 文件输出
    if cfg.Output == "file" || cfg.Output == "both" {
        logDir := filepath.Dir(cfg.FilePath)
        if err := os.MkdirAll(logDir, 0755); err != nil {
            return err
        }

        fileWriter := &lumberjack.Logger{
            Filename:   cfg.FilePath,
            MaxSize:    int(cfg.MaxSize / 1024 / 1024),
            MaxBackups: cfg.MaxBackups,
            MaxAge:     cfg.MaxAge,
            Compress:   cfg.Compress,
        }
        writers = append(writers, fileWriter)
    }

    // 创建 Logger
    Log = applog.New(
        applog.WithMultiWriter(writers...),
        applog.WithLevel(parseLevel(cfg.Level)),
    )

    return nil
}

type LoggerConfig struct {
    Level      string
    Output     string
    FilePath   string
    MaxSize    int64
    MaxBackups int
    MaxAge     int
    Compress   bool
}

func parseLevel(level string) applog.Level {
    switch level {
    case "debug":
        return applog.DebugLevel
    case "info":
        return applog.InfoLevel
    case "warn":
        return applog.WarnLevel
    case "error":
        return applog.ErrorLevel
    default:
        return applog.InfoLevel
    }
}

// 便捷函数
func Debug(args ...interface{}) {
    Log.Debug(args...)
}

func Info(args ...interface{}) {
    Log.Info(args...)
}

func Warn(args ...interface{}) {
    Log.Warn(args...)
}

func Error(args ...interface{}) {
    Log.Error(args...)
}

func Debugf(format string, args ...interface{}) {
    Log.Debugf(format, args...)
}

func Infof(format string, args ...interface{}) {
    Log.Infof(format, args...)
}

func Warnf(format string, args ...interface{}) {
    Log.Warnf(format, args...)
}

func Errorf(format string, args ...interface{}) {
    Log.Errorf(format, args...)
}
```

**Step 3: 提交**

```bash
git add internal/logger/logger.go go.mod
git commit -m "feat: add logger module with WQGroup/logger"
```

---

## Task 4: 实现数据模型

**Files:**
- Create: `docufiller-update-server/internal/models/version.go`

**Step 1: 创建版本模型**

创建 `docufiller-update-server/internal/models/version.go`:

```go
package models

import (
    "time"
    "gorm.io/gorm"
)

type Version struct {
    gorm.Model
    Version      string    `gorm:"type:varchar(20);uniqueIndex:idx_version_channel" json:"version"`
    Channel      string    `gorm:"type:varchar(10);uniqueIndex:idx_version_channel" json:"channel"`
    FileName     string    `gorm:"type:varchar(255);not null" json:"fileName"`
    FilePath     string    `gorm:"type:varchar(500);not null" json:"filePath"`
    FileSize     int64     `json:"fileSize"`
    FileHash     string    `gorm:"type:varchar(64);not null" json:"fileHash"`
    ReleaseNotes string    `gorm:"type:text" json:"releaseNotes"`
    PublishDate  time.Time `json:"publishDate"`
    DownloadCount int64    `gorm:"default:0" json:"downloadCount"`
    Mandatory    bool      `gorm:"default:false" json:"mandatory"`
}

func (Version) TableName() string {
    return "versions"
}
```

**Step 2: 提交**

```bash
git add internal/models/version.go
git commit -m "feat: add version model with GORM"
```

---

## Task 5: 实现数据库模块

**Files:**
- Create: `docufiller-update-server/internal/database/gorm.go`

**Step 1: 创建 GORM 初始化**

创建 `docufiller-update-server/internal/database/gorm.go`:

```go
package database

import (
    "time"

    "docufiller-update-server/internal/logger"
    "gorm.io/driver/sqlite"
    "gorm.io/gorm"
    "gorm.io/gorm/logger"
)

func NewGORM(dbPath string) (*gorm.DB, error) {
    db, err := gorm.Open(sqlite.Open(dbPath), &gorm.Config{
        Logger: NewGormLogger(),
    })
    if err != nil {
        return nil, err
    }

    // 配置连接池
    sqlDB, err := db.DB()
    if err != nil {
        return nil, err
    }

    sqlDB.SetMaxIdleConns(10)
    sqlDB.SetMaxOpenConns(100)
    sqlDB.SetConnMaxLifetime(time.Hour)

    return db, nil
}

// NewGormLogger 创建 GORM 日志适配器
func NewGormLogger() logger.Interface {
    return &gormLogger{}
}

type gormLogger struct{}

func (l *gormLogger) LogMode(level logger.LogLevel) logger.Interface {
    return l
}

func (l *gormLogger) Info(ctx context.Context, msg string, data ...interface{}) {
    logger.Info(msg, data)
}

func (l *gormLogger) Warn(ctx context.Context, msg string, data ...interface{}) {
    logger.Warn(msg, data)
}

func (l *gormLogger) Error(ctx context.Context, msg string, data ...interface{}) {
    logger.Error(msg, data)
}

func (l *gormLogger) Trace(ctx context.Context, begin time.Time, fc func() (string, int64), err error) {
    elapsed := time.Since(begin)
    sql, _ := fc()

    if err != nil {
        logger.Errorf("SQL error: %s, duration: %v, error: %v", sql, elapsed, err)
    } else if elapsed > 200*time.Millisecond {
        logger.Warnf("Slow SQL: %s, duration: %v", sql, elapsed)
    } else {
        logger.Debugf("SQL: %s, duration: %v", sql, elapsed)
    }
}
```

**Step 2: 提交**

```bash
git add internal/database/gorm.go
git commit -m "feat: add GORM database module"
```

---

## Task 6: 实现认证中间件

**Files:**
- Create: `docufiller-update-server/internal/middleware/auth.go`

**Step 1: 创建认证中间件**

创建 `docufiller-update-server/internal/middleware/auth.go`:

```go
package middleware

import (
    "strings"

    "github.com/gin-gonic/gin"
    "docufiller-update-server/internal/logger"
)

var uploadToken string

func InitAuth(token string) {
    uploadToken = token
    logger.Info("Auth middleware initialized")
}

func AuthMiddleware() gin.HandlerFunc {
    return func(c *gin.Context) {
        // 只对上传和删除操作进行认证
        if !strings.Contains(c.Request.URL.Path, "/upload") &&
           !strings.Contains(c.Request.URL.Path, "/delete") {
            c.Next()
            return
        }

        authHeader := c.GetHeader("Authorization")
        if authHeader == "" {
            logger.Warnf("Upload request without authorization from %s", c.ClientIP())
            c.JSON(401, gin.H{"error": "Unauthorized"})
            c.Abort()
            return
        }

        token := strings.TrimPrefix(authHeader, "Bearer ")
        if token != uploadToken {
            logger.Warnf("Invalid upload token from %s", c.ClientIP())
            c.JSON(403, gin.H{"error": "Forbidden"})
            c.Abort()
            return
        }

        logger.Debugf("Authorized request from %s", c.ClientIP())
        c.Next()
    }
}
```

**Step 2: 提交**

```bash
git add internal/middleware/auth.go
git commit -m "feat: add auth middleware"
```

---

## Task 7: 实现版本处理器

**Files:**
- Create: `docufiller-update-server/internal/handler/version.go`
- Create: `docufiller-update-server/internal/service/version.go`
- Create: `docufiller-update-server/internal/service/storage.go`

**Step 1: 创建存储服务**

创建 `docufiller-update-server/internal/service/storage.go`:

```go
package service

import (
    "crypto/sha256"
    "encoding/hex"
    "fmt"
    "io"
    "os"
    "path/filepath"

    "docufiller-update-server/internal/logger"
)

type StorageService struct {
    basePath string
}

func NewStorageService(basePath string) *StorageService {
    return &StorageService{basePath: basePath}
}

// SaveFile 保存文件到指定路径
func (s *StorageService) SaveFile(channel, version string, file io.Reader) (string, int64, string, error) {
    // 创建目录
    dir := filepath.Join(s.basePath, channel, version)
    if err := os.MkdirAll(dir, 0755); err != nil {
        return "", 0, "", err
    }

    // 创建文件
    fileName := fmt.Sprintf("docufiller-%s.zip", version)
    filePath := filepath.Join(dir, fileName)

    outFile, err := os.Create(filePath)
    if err != nil {
        return "", 0, "", err
    }
    defer outFile.Close()

    // 计算哈希
    hash := sha256.New()
    multiWriter := io.MultiWriter(outFile, hash)

    size, err := io.Copy(multiWriter, file)
    if err != nil {
        return "", 0, "", err
    }

    fileHash := hex.EncodeToString(hash.Sum(nil))

    logger.Infof("File saved: %s, size: %d, hash: %s", filePath, size, fileHash)

    return fileName, size, fileHash, nil
}

// DeleteFile 删除文件
func (s *StorageService) DeleteFile(channel, version string) error {
    dir := filepath.Join(s.basePath, channel, version)
    return os.RemoveAll(dir)
}

// GetFilePath 获取文件路径
func (s *StorageService) GetFilePath(channel, version string) string {
    return filepath.Join(s.basePath, channel, version, fmt.Sprintf("docufiller-%s.zip", version))
}
```

**Step 2: 创建版本服务**

创建 `docufiller-update-server/internal/service/version.go`:

```go
package service

import (
    "docufiller-update-server/internal/models"
    "gorm.io/gorm"
)

type VersionService struct {
    db            *gorm.DB
    storageSvc    *StorageService
}

func NewVersionService(db *gorm.DB, storageSvc *StorageService) *VersionService {
    return &VersionService{
        db:         db,
        storageSvc: storageSvc,
    }
}

// GetLatestVersion 获取最新版本
func (s *VersionService) GetLatestVersion(channel string) (*models.Version, error) {
    var version models.Version
    err := s.db.Where("channel = ?", channel).Order("publish_date DESC").First(&version).Error
    return &version, err
}

// GetVersionList 获取版本列表
func (s *VersionService) GetVersionList(channel string) ([]models.Version, error) {
    var versions []models.Version
    query := s.db.Order("publish_date DESC")
    if channel != "" {
        query = query.Where("channel = ?", channel)
    }
    err := query.Find(&versions).Error
    return versions, err
}

// GetVersion 获取指定版本
func (s *VersionService) GetVersion(channel, version string) (*models.Version, error) {
    var v models.Version
    err := s.db.Where("channel = ? AND version = ?", channel, version).First(&v).Error
    return &v, err
}

// CreateVersion 创建新版本
func (s *VersionService) CreateVersion(version *models.Version) error {
    return s.db.Create(version).Error
}

// DeleteVersion 删除版本
func (s *VersionService) DeleteVersion(channel, version string) error {
    return s.db.Where("channel = ? AND version = ?", channel, version).Delete(&models.Version{}).Error
}

// IncrementDownloadCount 增加下载计数
func (s *VersionService) IncrementDownloadCount(id uint) error {
    return s.db.Model(&models.Version{}).Where("id = ?", id).UpdateColumn("download_count", gorm.Expr("download_count + ?", 1)).Error
}
```

**Step 3: 创建版本处理器**

创建 `docufiller-update-server/internal/handler/version.go`:

```go
package handler

import (
    "net/http"
    "path/filepath"
    "strconv"
    "time"

    "github.com/gin-gonic/gin"
    "docufiller-update-server/internal/logger"
    "docufiller-update-server/internal/models"
    "docufiller-update-server/internal/service"
    "gorm.io/gorm"
)

type VersionHandler struct {
    versionSvc *service.VersionService
}

func NewVersionHandler(db *gorm.DB) *VersionHandler {
    storageSvc := service.NewStorageService("./data/packages")
    return &VersionHandler{
        versionSvc: service.NewVersionService(db, storageSvc),
    }
}

// GetLatestVersion 获取最新版本
func (h *VersionHandler) GetLatestVersion(c *gin.Context) {
    channel := c.DefaultQuery("channel", "stable")

    logger.Debugf("Get latest version request, channel: %s", channel)

    version, err := h.versionSvc.GetLatestVersion(channel)
    if err != nil {
        if err == gorm.ErrRecordNotFound {
            c.JSON(404, gin.H{"error": "No version found"})
        } else {
            logger.Errorf("Failed to get latest version: %v", err)
            c.JSON(500, gin.H{"error": "Internal server error"})
        }
        return
    }

    c.JSON(200, version)
}

// GetVersionList 获取版本列表
func (h *VersionHandler) GetVersionList(c *gin.Context) {
    channel := c.Query("channel")

    versions, err := h.versionSvc.GetVersionList(channel)
    if err != nil {
        logger.Errorf("Failed to get version list: %v", err)
        c.JSON(500, gin.H{"error": "Internal server error"})
        return
    }

    c.JSON(200, versions)
}

// GetVersionDetail 获取版本详情
func (h *VersionHandler) GetVersionDetail(c *gin.Context) {
    channel := c.Param("channel")
    version := c.Param("version")

    logger.Debugf("Get version detail: %s/%s", channel, version)

    v, err := h.versionSvc.GetVersion(channel, version)
    if err != nil {
        if err == gorm.ErrRecordNotFound {
            c.JSON(404, gin.H{"error": "Version not found"})
        } else {
            logger.Errorf("Failed to get version: %v", err)
            c.JSON(500, gin.H{"error": "Internal server error"})
        }
        return
    }

    c.JSON(200, v)
}

// UploadVersion 上传新版本
func (h *VersionHandler) UploadVersion(c *gin.Context) {
    channel := c.PostForm("channel")
    version := c.PostForm("version")
    notes := c.PostForm("notes")
    mandatory, _ := strconv.ParseBool(c.PostForm("mandatory"))

    if channel == "" || version == "" {
        c.JSON(400, gin.H{"error": "channel and version are required"})
        return
    }

    fileHeader, err := c.FormFile("file")
    if err != nil {
        c.JSON(400, gin.H{"error": "file is required"})
        return
    }

    logger.Infof("Upload request: %s/%s, file: %s", channel, version, fileHeader.Filename)

    // 打开文件
    file, err := fileHeader.Open()
    if err != nil {
        logger.Errorf("Failed to open uploaded file: %v", err)
        c.JSON(500, gin.H{"error": "Failed to process file"})
        return
    }
    defer file.Close()

    // 保存文件
    fileName, fileSize, fileHash, err := h.versionSvc.(*service.VersionService).GetStorageService().SaveFile(channel, version, file)
    if err != nil {
        logger.Errorf("Failed to save file: %v", err)
        c.JSON(500, gin.H{"error": "Failed to save file"})
        return
    }

    // 创建版本记录
    v := &models.Version{
        Version:      version,
        Channel:      channel,
        FileName:     fileName,
        FilePath:     filepath.Join("./data/packages", channel, version),
        FileSize:     fileSize,
        FileHash:     fileHash,
        ReleaseNotes: notes,
        PublishDate:  time.Now(),
        Mandatory:    mandatory,
    }

    if err := h.versionSvc.CreateVersion(v); err != nil {
        logger.Errorf("Failed to create version record: %v", err)
        c.JSON(500, gin.H{"error": "Failed to create version"})
        return
    }

    logger.Infof("Version uploaded successfully: %s/%s", channel, version)
    c.JSON(200, gin.H{"message": "Version uploaded successfully", "version": v})
}

// DeleteVersion 删除版本
func (h *VersionHandler) DeleteVersion(c *gin.Context) {
    channel := c.Param("channel")
    version := c.Param("version")

    logger.Infof("Delete request: %s/%s", channel, version)

    // 删除文件
    if err := h.versionSvc.(*service.VersionService).GetStorageService().DeleteFile(channel, version); err != nil {
        logger.Warnf("Failed to delete file: %v", err)
    }

    // 删除记录
    if err := h.versionSvc.DeleteVersion(channel, version); err != nil {
        logger.Errorf("Failed to delete version: %v", err)
        c.JSON(500, gin.H{"error": "Failed to delete version"})
        return
    }

    c.JSON(200, gin.H{"message": "Version deleted successfully"})
}

// DownloadFile 下载文件
func (h *VersionHandler) DownloadFile(c *gin.Context) {
    channel := c.Param("channel")
    version := c.Param("version")

    logger.Debugf("Download request: %s/%s", channel, version)

    v, err := h.versionSvc.GetVersion(channel, version)
    if err != nil {
        if err == gorm.ErrRecordNotFound {
            c.JSON(404, gin.H{"error": "Version not found"})
        } else {
            c.JSON(500, gin.H{"error": "Internal server error"})
        }
        return
    }

    filePath := h.versionSvc.(*service.VersionService).GetStorageService().GetFilePath(channel, version)
    c.File(filePath)

    // 增加下载计数
    go h.versionSvc.IncrementDownloadCount(v.ID)
}
```

**Step 4: 修复导入问题**

修改 `docufiller-update-server/internal/service/version.go` 添加获取存储服务的方法:

```go
// GetStorageService 返回存储服务（用于 Handler）
func (s *VersionService) GetStorageService() *StorageService {
    return s.storageSvc
}
```

**Step 5: 提交**

```bash
git add internal/handler/version.go internal/service/version.go internal/service/storage.go
git commit -m "feat: add version handler and service"
```

---

## Task 8: 创建发布脚本

**Files:**
- Create: `scripts/build.bat`
- Create: `scripts/publish.bat`
- Create: `scripts/build-and-publish.bat`
- Create: `scripts/config/publish-config.bat`

**Step 1: 创建发布配置**

创建 `scripts/config/publish-config.bat`:

```batch
@echo off
set UPDATE_SERVER_URL=http://192.168.1.100:8080
set UPDATE_SERVER_TOKEN=change-this-token-in-production
set DEFAULT_CHANNEL=stable
```

**Step 2: 创建编译脚本**

创建 `scripts/build.bat`:

```batch
@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DocuFiller Build Script
echo ========================================

REM 读取版本号
for /f "tokens=2 delims==" %%a in ('findstr /r "^.*Version>.*<" ..\DocuFiller.csproj') do (
    set VERSION_LINE=%%a
    for /f "tokens=2 delims=<>" %%b in ("%%a") do set VERSION=%%b
)

if "!VERSION!"=="" (
    echo Error: Cannot read version from DocuFiller.csproj
    exit /b 1
)

echo Building version: !VERSION!

REM 清理旧的构建输出
if exist "build" rmdir /s /q "build"
mkdir "build"

REM 编译发布
dotnet publish ..\DocuFiller.csproj -c Release -r win-x64 --self-contained -o "build\temp"

REM 打包
cd build\temp
tar -a -cf ..\docufiller-!VERSION!.zip .
cd ..\..

echo ========================================
echo Build completed!
echo Output: build\docufiller-!VERSION!.zip
echo ========================================
```

**Step 3: 创建发布脚本**

创建 `scripts/publish.bat`:

```batch
@echo off
if "%1"=="" (
    echo Usage: publish.bat [stable^|beta] [version]
    echo Example: publish.bat stable 1.2.0
    exit /b 1
)

set CHANNEL=%1
set VERSION=%2

REM 加载配置
call config\publish-config.bat

REM 检查文件
if not exist "build\docufiller-%VERSION%.zip" (
    echo Error: Build file not found!
    echo Please run build.bat first.
    exit /b 1
)

echo ========================================
echo Publishing %CHANNEL% version %VERSION%
echo Server: %UPDATE_SERVER_URL%
echo ========================================

REM 调用 API 上传
curl -X POST "%UPDATE_SERVER_URL%/api/version/upload" ^
  -H "Authorization: Bearer %UPDATE_SERVER_TOKEN%" ^
  -F "channel=%CHANNEL%" ^
  -F "version=%VERSION%" ^
  -F "file=@build\docufiller-%VERSION%.zip" ^
  -F "mandatory=false"

echo.
echo ========================================
echo Publish completed!
echo ========================================
```

**Step 4: 创建一键发布脚本**

创建 `scripts/build-and-publish.bat`:

```batch
@echo off
setlocal enabledelayedexpansion

set CHANNEL=%1
if "%CHANNEL%"=="" set CHANNEL=stable

echo ========================================
echo DocuFiller Build and Publish
echo Channel: %CHANNEL%
echo ========================================

REM 编译
call build.bat
if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

REM 发布
call publish.bat %CHANNEL% %VERSION%
if errorlevel 1 (
    echo Publish failed!
    exit /b 1
)

echo ========================================
echo All done! Version %VERSION% (%CHANNEL%) published.
echo ========================================
```

**Step 5: 提交**

```bash
git add scripts/
git commit -m "feat: add build and publish scripts"
```

---

## Task 9: 创建 WPF 更新服务接口

**Files:**
- Create: `DocuFiller/Services/Update/IUpdateService.cs`
- Create: `DocuFiller/Models/Update/VersionInfo.cs`
- Create: `DocuFiller/Models/Update/UpdateConfig.cs`
- Create: `DocuFiller/Models/Update/DownloadProgress.cs`

**Step 1: 创建版本信息模型**

创建 `DocuFiller/Models/Update/VersionInfo.cs`:

```csharp
namespace DocuFiller.Models.Update;

public class VersionInfo
{
    public string Version { get; set; }
    public string Channel { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; }
    public string ReleaseNotes { get; set; }
    public DateTime PublishDate { get; set; }
    public bool Mandatory { get; set; }
    public bool IsDownloaded { get; set; }
}

public class UpdateConfig
{
    public string ServerUrl { get; set; } = "http://192.168.1.100:8080";
    public string Channel { get; set; } = "stable";
    public bool CheckOnStartup { get; set; } = true;
    public bool AutoDownload { get; set; } = true;
}

public class DownloadProgress
{
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public int ProgressPercentage { get; set; }
}
```

**Step 2: 创建更新服务接口**

创建 `DocuFiller/Services/Update/IUpdateService.cs`:

```csharp
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update;

public interface IUpdateService
{
    Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel);
    Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress);
    Task<bool> VerifyFileHashAsync(string filePath, string expectedHash);
    Task<bool> InstallUpdateAsync(string packagePath);
    event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
}

public class UpdateAvailableEventArgs : EventArgs
{
    public VersionInfo Version { get; set; } = null!;
    public bool IsDownloaded { get; set; }
}
```

**Step 3: 提交**

```bash
git add DocuFiller/Services/Update/IUpdateService.cs DocuFiller/Models/Update/
git commit -m "feat: add update service interfaces and models"
```

---

## Task 10: 实现 WPF 更新服务

**Files:**
- Create: `DocuFiller/Services/Update/UpdateService.cs`
- Modify: `DocuFiller/App.xaml.cs` (注册服务)

**Step 1: 创建更新服务实现**

创建 `DocuFiller/Services/Update/UpdateService.cs`:

```csharp
using System.Security.Cryptography;
using System.Diagnostics;
using DocuFiller.Models.Update;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DocuFiller.Services.Update;

public class UpdateService : IUpdateService, IDisposable
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly UpdateConfig _config;
    private readonly string _tempDir;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _config = configuration.GetSection("Update").Get<UpdateConfig>() ?? new UpdateConfig();
        _tempDir = Path.Combine(Path.GetTempPath(), "DocuFiller", "Updates");
        Directory.CreateDirectory(_tempDir);
    }

    public async Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel)
    {
        try
        {
            var url = $"{_config.ServerUrl}/api/version/latest?channel={channel}";
            _logger.LogDebug("Checking for updates at: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var version = await response.Content.ReadFromJsonAsync<VersionInfo>();
            if (version == null) return null;

            if (IsNewerVersion(version.Version, currentVersion))
            {
                _logger.LogInformation("New version available: {Version} (current: {CurrentVersion})",
                    version.Version, currentVersion);

                var localPath = Path.Combine(_tempDir, version.FileName);
                version.IsDownloaded = File.Exists(localPath);

                return version;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress)
    {
        var outputPath = Path.Combine(_tempDir, version.FileName);
        var downloadUrl = $"{_config.ServerUrl}/api/download/{version.Channel}/{version.Version}";

        _logger.LogInformation("Downloading update: {Version} to {Path}", version.Version, outputPath);

        var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long bytesRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, read);
            bytesRead += read;

            progress?.Report(new DownloadProgress
            {
                BytesReceived = bytesRead,
                TotalBytes = totalBytes,
                ProgressPercentage = totalBytes > 0 ? (int)((bytesRead * 100) / totalBytes) : 0
            });
        }

        _logger.LogInformation("Download completed: {Version}", version.Version);
        return outputPath;
    }

    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        var hash = await sha256.ComputeHashAsync(stream);
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();

        _logger.LogInformation("File hash verification: expected={Expected}, actual={Actual}",
            expectedHash, hashString);

        return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> InstallUpdateAsync(string packagePath)
    {
        try
        {
            _logger.LogInformation("Installing update from: {Path}", packagePath);

            // 启动安装程序
            var startInfo = new ProcessStartInfo
            {
                FileName = packagePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);

            // 关闭当前应用
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install update");
            return false;
        }
    }

    private bool IsNewerVersion(string newVersion, string currentVersion)
    {
        if (!Version.TryParse(newVersion, out var newVer)) return false;
        if (!Version.TryParse(currentVersion, out var curVer)) return false;
        return newVer > curVer;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

**Step 2: 注册服务**

修改 `DocuFiller/App.xaml.cs`，在 `ConfigureServices` 中添加:

```csharp
services.AddSingleton<IUpdateService, UpdateService>();
```

**Step 3: 提交**

```bash
git add DocuFiller/Services/Update/UpdateService.cs
git commit -m "feat: implement update service"
```

---

## Task 11: 创建更新窗口 ViewModel 和 View

**Files:**
- Create: `DocuFiller/ViewModels/Update/UpdateViewModel.cs`
- Create: `DocuFiller/Views/Update/UpdateWindow.xaml`
- Create: `DocuFiller/Views/Update/UpdateWindow.xaml.cs`

**Step 1: 创建更新窗口 ViewModel**

创建 `DocuFiller/ViewModels/Update/UpdateViewModel.cs`:

```csharp
using System.Windows.Input;
using DocuFiller.Models.Update;
using DocuFiller.Services.Update;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels.Update;

public class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private readonly VersionInfo _versionInfo;
    private readonly ILogger<UpdateViewModel> _logger;
    private string _downloadPath = string.Empty;

    private bool _isDownloading;
    private int _downloadProgress;
    private string _statusMessage = "准备下载...";
    private bool _canInstall;

    public ICommand DownloadCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RemindLaterCommand { get; }

    public UpdateViewModel(IUpdateService updateService, VersionInfo versionInfo, ILogger<UpdateViewModel> logger)
    {
        _updateService = updateService;
        _versionInfo = versionInfo;
        _logger = logger;

        DownloadCommand = new RelayCommand(DownloadUpdate, () => !IsDownloading);
        InstallCommand = new RelayCommand(InstallUpdate, () => CanInstall);
        CancelCommand = new RelayCommand(Cancel);
        RemindLaterCommand = new RelayCommand(RemindLater);

        if (_versionInfo.IsDownloaded)
        {
            _downloadPath = Path.Combine(Path.GetTempPath(), "DocuFiller", "Updates", _versionInfo.FileName);
            StatusMessage = "更新已下载完成";
            CanInstall = true;
        }
    }

    public string Version => _versionInfo.Version;
    public string ReleaseNotes => _versionInfo.ReleaseNotes ?? "暂无更新说明";
    public DateTime PublishDate => _versionInfo.PublishDate;
    public long FileSizeMB => _versionInfo.FileSize / (1024 * 1024);

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
            ((RelayCommand)DownloadCommand).OnCanExecuteChanged();
        }
    }

    public int DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            _downloadProgress = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool CanInstall
    {
        get => _canInstall;
        set
        {
            _canInstall = value;
            OnPropertyChanged();
            ((RelayCommand)InstallCommand).OnCanExecuteChanged();
        }
    }

    private async void DownloadUpdate()
    {
        IsDownloading = true;
        StatusMessage = "正在下载更新...";

        var progress = new Progress<DownloadProgress>(p =>
        {
            DownloadProgress = p.ProgressPercentage;
            StatusMessage = $"下载中... {p.ProgressPercentage}%";
        });

        try
        {
            _downloadPath = await _updateService.DownloadUpdateAsync(_versionInfo, progress);

            // 验证文件哈希
            var verified = await _updateService.VerifyFileHashAsync(_downloadPath, _versionInfo.FileHash);
            if (!verified)
            {
                StatusMessage = "下载文件校验失败！";
                _logger.LogError("File hash verification failed for version {Version}", _versionInfo.Version);
                return;
            }

            StatusMessage = "下载完成！";
            CanInstall = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败: {ex.Message}";
            _logger.LogError(ex, "Failed to download update");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private async void InstallUpdate()
    {
        StatusMessage = "正在安装更新...";
        await _updateService.InstallUpdateAsync(_downloadPath);
    }

    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    private void RemindLater()
    {
        // TODO: 记录提醒时间
        RequestClose?.Invoke();
    }

    public Action? RequestClose { get; set; }
}
```

**Step 2: 创建更新窗口 View**

创建 `DocuFiller/Views/Update/UpdateWindow.xaml`:

```xaml
<Window x:Class="DocuFiller.Views.Update.UpdateWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="发现新版本" Height="400" Width="500"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <StackPanel Grid.Row="0" Margin="0,0,0,15">
            <TextBlock Text="发现新版本" FontSize="20" FontWeight="Bold"/>
            <TextBlock FontSize="14" Foreground="Gray">
                <Run Text="当前最新版本: "/><Run Text="{Binding Version}" FontWeight="Bold"/>
            </TextBlock>
        </StackPanel>

        <!-- 更新信息 -->
        <Border Grid.Row="1" BorderBrush="LightGray" BorderThickness="1" Padding="10" Background="WhiteSmoke">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <TextBlock Text="{Binding ReleaseNotes}" TextWrapping="Wrap" Foreground="#333"/>
            </ScrollViewer>
        </Border>

        <!-- 下载进度 -->
        <StackPanel Grid.Row="2" Margin="0,15,0,10">
            <ProgressBar Height="25" Minimum="0" Maximum="100" Value="{Binding DownloadProgress}"
                         Visibility="{Binding IsDownloading, Converter={StaticResource BoolToVisibilityConverter}}"/>
            <TextBlock Text="{Binding StatusMessage}" Margin="0,5,0,0" HorizontalAlignment="Center"/>
        </StackPanel>

        <!-- 按钮 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="稍后提醒" Command="{Binding RemindLaterCommand}" Width="100" Height="32" Margin="5,0"/>
            <Button Content="关闭" Command="{Binding CancelCommand}" Width="80" Height="32" Margin="5,0"/>
            <Button Content="下载更新" Command="{Binding DownloadCommand}"
                    Width="100" Height="32" Margin="5,0"
                    Visibility="{Binding IsDownloading, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
            <Button Content="立即安装" Command="{Binding InstallCommand}"
                    Width="100" Height="32" Margin="5,0"
                    Background="Green" Foreground="White"
                    Visibility="{Binding CanInstall, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </StackPanel>
    </Grid>
</Window>
```

创建 `DocuFiller/Views/Update/UpdateWindow.xaml.cs`:

```csharp
using DocuFiller.ViewModels.Update;

namespace DocuFiller.Views.Update;

public partial class UpdateWindow : Window
{
    public UpdateWindow(UpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose = () => Close();
    }
}
```

**Step 3: 提交**

```bash
git add DocuFiller/ViewModels/Update/ DocuFiller/Views/Update/
git commit -m "feat: add update window view and viewmodel"
```

---

## Task 12: 集成更新检查到主窗口

**Files:**
- Modify: `DocuFiller/ViewModels/MainViewModel.cs`

**Step 1: 在 MainViewModel 中集成更新检查**

在 `DocuFiller/ViewModels/MainViewModel.cs` 中添加:

```csharp
private readonly IUpdateService _updateService;

public MainViewModel(IUpdateService updateService, ...)
{
    _updateService = updateService;
    // ... 其他初始化
}

protected override async Task OnInitializedAsync()
{
    // 启动时检查更新
    if (_config.CheckOnStartup)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000); // 延迟2秒，让主界面先加载

            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0";

            var update = await _updateService.CheckForUpdateAsync(currentVersion, _config.Channel);

            if (update != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ShowUpdateWindow(update);
                });
            }
        });
    }
}

private void ShowUpdateWindow(VersionInfo version)
{
    var logger = App.Current.Services.GetService<ILogger<UpdateViewModel>>();
    var viewModel = new UpdateViewModel(_updateService, version, logger);
    var window = new UpdateWindow(viewModel);
    window.ShowDialog();
}
```

**Step 2: 更新 App.config**

修改 `DocuFiller/App.config`，添加更新配置:

```xml
<appSettings>
  <!-- 其他配置 -->
  <add key="Update:ServerUrl" value="http://192.168.1.100:8080" />
  <add key="Update:Channel" value="stable" />
  <add key="Update:CheckOnStartup" value="true" />
  <add key="Update:AutoDownload" value="true" />
</appSettings>
```

**Step 3: 提交**

```bash
git add DocuFiller/ViewModels/MainViewModel.cs DocuFiller/App.config
git commit -m "feat: integrate update check to main window"
```

---

## Task 13: 测试整个更新流程

**Files:**
- None (测试任务)

**Step 1: 启动 Go 服务器**

```bash
cd docufiller-update-server
go run main.go
```

预期输出: `Server listening on 0.0.0.0:8080`

**Step 2: 测试健康检查**

```bash
curl http://localhost:8080/api/health
```

预期输出: `{"status":"ok"}`

**Step 3: 使用脚本发布测试版本**

```bash
cd scripts
build-and-publish.bat stable
```

预期: 编译成功并上传到服务器

**Step 4: 验证版本已上传**

```bash
curl http://localhost:8080/api/version/latest?channel=stable
```

预期: 返回刚发布的版本信息

**Step 5: 测试客户端更新检查**

1. 修改 `DocuFiller.csproj` 中的版本号为更低的版本（如 0.9.0）
2. 运行 DocuFiller 应用
3. 预期: 启动后2秒显示更新窗口

**Step 6: 测试下载流程**

在更新窗口中点击"下载更新"，验证:
- 下载进度显示正确
- 下载完成后显示"安装"按钮

**Step 7: 测试安装流程**

点击"立即安装"，验证:
- 应用关闭
- 安装程序启动

**Step 8: 提交测试结果**

```bash
echo "测试完成 - 更新流程正常工作" > docs/test-results/update-system-test.md
git add docs/test-results/update-system-test.md
git commit -m "test: complete update system testing"
```

---

## 验收标准

- [ ] Go 服务器可以正常启动和响应 API 请求
- [ ] 可以通过脚本成功发布新版本
- [ ] 客户端启动时能检测到新版本
- [ ] 更新窗口正确显示版本信息
- [ ] 下载进度正确显示
- [ ] 文件哈希验证正常工作
- [ ] 安装流程能正常启动
- [ ] 所有日志正确记录
- [ ] 认证中间件正常工作
- [ ] 数据库正确存储版本信息

---

## 实施顺序

1. Task 1-6: Go 服务器基础架构
2. Task 7: Go 服务器业务逻辑
3. Task 8: 发布脚本
4. Task 9-12: WPF 客户端更新组件
5. Task 13: 端到端测试

每个 Task 完成后提交一次代码，保持小步快跑。
