using System;
using System.IO;
using System.Threading.Tasks;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// FileService 异常路径单元测试
    /// 验证 T01 添加的 ILogger 依赖在异常路径中正确记录结构化日志
    /// </summary>
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<ILogger<FileService>> _loggerMock;
        private readonly FileService _fileService;
        private readonly string _testDirectory;

        public FileServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileService>>();
            _fileService = new FileService(_loggerMock.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), $"FileServiceTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // 忽略清理失败
            }
        }

        /// <summary>
        /// 验证 EnsureDirectoryExists 在异常时记录 LogError 并返回 false
        /// 使用包含非法字符的路径触发 Directory.CreateDirectory 异常
        /// </summary>
        [Fact]
        public void EnsureDirectoryExists_Exception_LogsErrorAndReturnsFalse()
        {
            // Arrange: 使用包含非法字符的路径触发异常
            string invalidPath = Path.Combine(_testDirectory, "test\0invalid");

            // Act
            bool result = _fileService.EnsureDirectoryExists(invalidPath);

            // Assert
            Assert.False(result);

            // 验证 LogError 被调用，包含异常和消息
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("创建目录失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "EnsureDirectoryExists 应在异常时记录包含路径的 LogError");
        }

        /// <summary>
        /// 验证 CopyFileAsync 在异常时记录 LogError 并返回 false
        /// 使用不存在的源文件触发 File.Copy 异常
        /// </summary>
        [Fact]
        public async Task CopyFileAsync_Exception_LogsErrorAndReturnsFalse()
        {
            // Arrange
            string nonExistentSource = Path.Combine(_testDirectory, "nonexistent_source.txt");
            string destination = Path.Combine(_testDirectory, "destination.txt");

            // Act
            bool result = await _fileService.CopyFileAsync(nonExistentSource, destination);

            // Assert
            Assert.False(result);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("复制文件失败") &&
                        state.ToString()!.Contains(nonExistentSource)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "CopyFileAsync 应在异常时记录包含源路径的 LogError");
        }

        /// <summary>
        /// 验证 DeleteFile 在异常时记录 LogError 并返回 false
        /// 使用不存在的路径并在只读目录中尝试删除来触发异常
        /// </summary>
        [Fact]
        public void DeleteFile_Exception_LogsErrorAndReturnsFalse()
        {
            // Arrange: 尝试删除深层不存在路径中的文件 — 驱动器根目录下的非法路径
            // 使用一个足够深的不存在路径来触发 DirectoryNotFoundException
            string invalidPath = Path.Combine(_testDirectory, "nonexistent_subdir_12345", "file_to_delete.txt");

            // Act
            bool result = _fileService.DeleteFile(invalidPath);

            // Assert
            Assert.False(result);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("删除文件失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "DeleteFile 应在异常时记录 LogError");
        }

        /// <summary>
        /// 验证 WriteFileContentAsync 在异常时记录 LogError 并返回 false
        /// 使用非法路径触发 File.WriteAllTextAsync 异常
        /// </summary>
        [Fact]
        public async Task WriteFileContentAsync_Exception_LogsErrorAndReturnsFalse()
        {
            // Arrange: 使用包含非法字符的路径触发异常
            string invalidPath = Path.Combine(_testDirectory, "bad\0path.txt");

            // Act
            bool result = await _fileService.WriteFileContentAsync(invalidPath, "test content");

            // Assert
            Assert.False(result);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) =>
                        state.ToString()!.Contains("写入文件失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "WriteFileContentAsync 应在异常时记录 LogError");
        }

        // ==================== 快乐路径测试 ====================

        [Fact]
        public void FileExists_ExistingFile_ReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "exists.txt");
            File.WriteAllText(filePath, "hello");

            // Act
            bool result = _fileService.FileExists(filePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FileExists_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "no_such_file.txt");

            // Act
            bool result = _fileService.FileExists(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EnsureDirectoryExists_NewPath_CreatesAndReturnsTrue()
        {
            // Arrange
            string newDir = Path.Combine(_testDirectory, "new_subdir");

            // Act
            bool result = _fileService.EnsureDirectoryExists(newDir);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(newDir));
        }

        [Fact]
        public void DirectoryExists_ExistingDir_ReturnsTrue()
        {
            // Arrange: _testDirectory was created in the constructor
            // Act
            bool result = _fileService.DirectoryExists(_testDirectory);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CopyFileAsync_ValidSource_CopiesAndReturnsTrue()
        {
            // Arrange
            string sourcePath = Path.Combine(_testDirectory, "source.txt");
            string destPath = Path.Combine(_testDirectory, "dest.txt");
            const string content = "copy me";
            File.WriteAllText(sourcePath, content);

            // Act
            bool result = await _fileService.CopyFileAsync(sourcePath, destPath);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(destPath));
            Assert.Equal(content, File.ReadAllText(destPath));
        }

        [Fact]
        public async Task CopyFileAsync_OverwriteTrue_OverwritesDestination()
        {
            // Arrange
            string sourcePath = Path.Combine(_testDirectory, "src.txt");
            string destPath = Path.Combine(_testDirectory, "dst.txt");
            File.WriteAllText(sourcePath, "new content");
            File.WriteAllText(destPath, "old content");

            // Act
            bool result = await _fileService.CopyFileAsync(sourcePath, destPath, overwrite: true);

            // Assert
            Assert.True(result);
            Assert.Equal("new content", File.ReadAllText(destPath));
        }

        [Fact]
        public async Task WriteFileContentAsync_ValidPath_WritesAndReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "write_test.txt");
            const string content = "hello world";

            // Act
            bool result = await _fileService.WriteFileContentAsync(filePath, content);

            // Assert
            Assert.True(result);
            Assert.Equal(content, await _fileService.ReadFileContentAsync(filePath));
        }

        [Fact]
        public void DeleteFile_ExistingFile_DeletesAndReturnsTrue()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "to_delete.txt");
            File.WriteAllText(filePath, "bye");

            // Act
            bool result = _fileService.DeleteFile(filePath);

            // Assert
            Assert.True(result);
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void GenerateUniqueFileName_ReturnsCorrectFormat()
        {
            // Arrange
            const string directory = @"C:\output";
            const string fileName = "report.docx";
            const string pattern = "{0}_{1}{2}";

            // Act
            string result = _fileService.GenerateUniqueFileName(directory, fileName, pattern, 3);

            // Assert
            Assert.Equal(@"C:\output\report_3.docx", result);
        }

        [Fact]
        public void ValidateFileExtension_Docx_ReturnsValid()
        {
            // Arrange
            var allowed = new List<string> { ".docx", ".doc" };

            // Act
            var result = _fileService.ValidateFileExtension("test.docx", allowed);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void ValidateFileExtension_Txt_ReturnsInvalid()
        {
            // Arrange
            var allowed = new List<string> { ".docx", ".doc" };

            // Act
            var result = _fileService.ValidateFileExtension("notes.txt", allowed);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(".txt", result.ErrorMessage);
        }

        [Fact]
        public void GetFileSize_ExistingFile_ReturnsCorrectSize()
        {
            // Arrange: UTF-8 without BOM, "hello" = 5 bytes
            string filePath = Path.Combine(_testDirectory, "sized.txt");
            File.WriteAllText(filePath, "hello");

            // Act
            long size = _fileService.GetFileSize(filePath);

            // Assert
            Assert.Equal(5, size);
        }

        [Fact]
        public async Task ReadFileContentAsync_ExistingFile_ReturnsContent()
        {
            // Arrange
            string filePath = Path.Combine(_testDirectory, "read_test.txt");
            const string expected = "read me";
            File.WriteAllText(filePath, expected);

            // Act
            string content = await _fileService.ReadFileContentAsync(filePath);

            // Assert
            Assert.Equal(expected, content);
        }
    }
}
