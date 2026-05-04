using DocuFiller.Configuration;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DocuFiller.Tests.Services
{
    public class TemplateCacheServiceTests : IDisposable
    {
        private readonly Mock<ILogger<TemplateCacheService>> _loggerMock;
        private readonly Mock<IOptionsMonitor<PerformanceSettings>> _optionsMock;
        private readonly PerformanceSettings _settings;
        private TemplateCacheService? _service;

        public TemplateCacheServiceTests()
        {
            _loggerMock = new Mock<ILogger<TemplateCacheService>>();
            _optionsMock = new Mock<IOptionsMonitor<PerformanceSettings>>();
            _settings = new PerformanceSettings
            {
                EnableTemplateCache = true,
                CacheExpirationMinutes = 30
            };
            _optionsMock.Setup(x => x.CurrentValue).Returns(_settings);
        }

        private TemplateCacheService CreateService()
        {
            _service = new TemplateCacheService(_loggerMock.Object, _optionsMock.Object);
            return _service;
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        [Fact]
        public void CacheValidationResult_ThenGet_ReturnsCachedValue()
        {
            // Arrange
            var svc = CreateService();
            var result = new ValidationResult { IsValid = true };
            var path = "template.docx";

            // Act
            svc.CacheValidationResult(path, result);
            var cached = svc.GetCachedValidationResult(path);

            // Assert
            Assert.NotNull(cached);
            Assert.True(cached.IsValid);
        }

        [Fact]
        public void GetCachedValidationResult_NotCached_ReturnsNull()
        {
            // Arrange
            var svc = CreateService();

            // Act
            var cached = svc.GetCachedValidationResult("notexist.docx");

            // Assert
            Assert.Null(cached);
        }

        [Fact]
        public void GetCachedValidationResult_Expired_ReturnsNull()
        {
            // Arrange
            var svc = CreateService();
            var result = new ValidationResult { IsValid = true };
            var path = "expired.docx";
            svc.CacheValidationResult(path, result);

            // Change expiration to 0 minutes — even tiny delay causes expiry
            _settings.CacheExpirationMinutes = 0;
            Thread.Sleep(10);

            // Act
            var cached = svc.GetCachedValidationResult(path);

            // Assert
            Assert.Null(cached);
        }

        [Fact]
        public void CacheContentControls_ThenGet_ReturnsCachedList()
        {
            // Arrange
            var svc = CreateService();
            var controls = new List<ContentControlData>
            {
                new() { Tag = "field1", Title = "Field 1" },
                new() { Tag = "field2", Title = "Field 2" }
            };
            var path = "controls.docx";

            // Act
            svc.CacheContentControls(path, controls);
            var cached = svc.GetCachedContentControls(path);

            // Assert
            Assert.NotNull(cached);
            Assert.Equal(2, cached.Count);
            Assert.Equal("field1", cached[0].Tag);
        }

        [Fact]
        public void InvalidateCache_RemovesCachedItem()
        {
            // Arrange
            var svc = CreateService();
            var path = "invalidate.docx";
            svc.CacheValidationResult(path, new ValidationResult { IsValid = true });
            svc.CacheContentControls(path, new List<ContentControlData>());

            // Act
            svc.InvalidateCache(path);

            // Assert
            Assert.Null(svc.GetCachedValidationResult(path));
            Assert.Null(svc.GetCachedContentControls(path));
        }

        [Fact]
        public void ClearAllCache_RemovesAllItems()
        {
            // Arrange
            var svc = CreateService();
            svc.CacheValidationResult("a.docx", new ValidationResult { IsValid = true });
            svc.CacheValidationResult("b.docx", new ValidationResult { IsValid = false });
            svc.CacheContentControls("a.docx", new List<ContentControlData>());

            // Act
            svc.ClearAllCache();

            // Assert
            Assert.Null(svc.GetCachedValidationResult("a.docx"));
            Assert.Null(svc.GetCachedValidationResult("b.docx"));
            Assert.Null(svc.GetCachedContentControls("a.docx"));
        }

        [Fact]
        public void Dispose_ThenAccess_ThrowsObjectDisposedException()
        {
            // Arrange
            var svc = CreateService();
            svc.CacheValidationResult("t.docx", new ValidationResult { IsValid = true });

            // Act
            svc.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => svc.GetCachedValidationResult("t.docx"));
            Assert.Throws<ObjectDisposedException>(() => svc.CacheValidationResult("x.docx", new ValidationResult()));
            Assert.Throws<ObjectDisposedException>(() => svc.GetCachedContentControls("t.docx"));
            Assert.Throws<ObjectDisposedException>(() => svc.CacheContentControls("x.docx", new List<ContentControlData>()));
            Assert.Throws<ObjectDisposedException>(() => svc.InvalidateCache("t.docx"));
            Assert.Throws<ObjectDisposedException>(() => svc.ClearAllCache());
        }

        [Fact]
        public void CacheDisabled_ReturnsNull()
        {
            // Arrange
            _settings.EnableTemplateCache = false;
            var svc = CreateService();
            var path = "disabled.docx";

            // Act — cache should be skipped when disabled
            svc.CacheValidationResult(path, new ValidationResult { IsValid = true });
            var cachedValidation = svc.GetCachedValidationResult(path);

            svc.CacheContentControls(path, new List<ContentControlData>());
            var cachedControls = svc.GetCachedContentControls(path);

            // Assert
            Assert.Null(cachedValidation);
            Assert.Null(cachedControls);
        }

        [Fact]
        public void NullOrEmptyTemplatePath_ReturnsNull()
        {
            // Arrange
            var svc = CreateService();

            // Act & Assert — null / empty / whitespace paths return null for get
            Assert.Null(svc.GetCachedValidationResult(null!));
            Assert.Null(svc.GetCachedValidationResult(""));
            Assert.Null(svc.GetCachedValidationResult("   "));

            Assert.Null(svc.GetCachedContentControls(null!));
            Assert.Null(svc.GetCachedContentControls(""));
            Assert.Null(svc.GetCachedContentControls("   "));

            // Cache calls with null/empty paths should not throw and not cache anything
            svc.CacheValidationResult(null!, new ValidationResult());
            svc.CacheValidationResult("", new ValidationResult());
            svc.CacheContentControls(null!, new List<ContentControlData>());
            svc.CacheContentControls("", new List<ContentControlData>());
        }

        [Fact]
        public void CacheValidationResult_OverwritesExistingValue()
        {
            // Arrange
            var svc = CreateService();
            var path = "overwrite.docx";
            svc.CacheValidationResult(path, new ValidationResult { IsValid = true });

            // Act — cache again with different result
            svc.CacheValidationResult(path, new ValidationResult { IsValid = false });
            var cached = svc.GetCachedValidationResult(path);

            // Assert — should return the latest value
            Assert.NotNull(cached);
            Assert.False(cached.IsValid);
        }

        [Fact]
        public void ClearExpiredCache_RemovesExpiredItems()
        {
            // Arrange — cache items, then set expiration to 0 so they expire immediately
            var svc = CreateService();
            svc.CacheValidationResult("old.docx", new ValidationResult { IsValid = true });
            svc.CacheContentControls("old.docx", new List<ContentControlData> { new() { Tag = "t1" } });

            // Set expiration to 0 and wait — items will be considered expired
            _settings.CacheExpirationMinutes = 0;
            Thread.Sleep(10);

            // Act
            svc.ClearExpiredCache();

            // Assert — expired items are removed (get returns null)
            // Note: get itself checks expiration with CacheExpirationMinutes=0, so null either way
            // To confirm ClearExpiredCache actually removed items, restore normal expiration
            _settings.CacheExpirationMinutes = 30;
            Assert.Null(svc.GetCachedValidationResult("old.docx"));
            Assert.Null(svc.GetCachedContentControls("old.docx"));
        }
    }
}
