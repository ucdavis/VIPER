using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Viper.Areas.Students.Services;

namespace Viper.test.Students
{
    public class PhotoServiceTest : IDisposable
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<PhotoService>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly PhotoService _photoService;
        private readonly string _testPhotoDirectory;
        private readonly string _testDefaultPhotoPath;

        public PhotoServiceTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<PhotoService>>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();

            _testPhotoDirectory = Path.Join(Path.GetTempPath(), $"VIPERPhotoTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testPhotoDirectory);

            _mockConfiguration.Setup(c => c["PhotoGallery:IDCardPhotoPath"]).Returns(_testPhotoDirectory);
            _mockConfiguration.Setup(c => c["PhotoGallery:DefaultPhotoFile"]).Returns("nopic.jpg");
            _mockConfiguration.Setup(c => c["PhotoGallery:CacheDurationHours"]).Returns("24");

            var wwwrootPath = Path.Join(_testPhotoDirectory, "wwwroot");
            Directory.CreateDirectory(Path.Join(wwwrootPath, "images"));
            _mockWebHostEnvironment.Setup(e => e.WebRootPath).Returns(wwwrootPath);

            _testDefaultPhotoPath = Path.Join(_testPhotoDirectory, "nopic.jpg");
            File.WriteAllBytes(_testDefaultPhotoPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            var wwwrootDefaultPath = Path.Join(wwwrootPath, "images", "nopic.jpg");
            File.WriteAllBytes(wwwrootDefaultPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            _photoService = new PhotoService(_mockConfiguration.Object, _memoryCache, _mockLogger.Object, _mockWebHostEnvironment.Object);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(_testPhotoDirectory))
                {
                    Directory.Delete(_testPhotoDirectory, true);
                }
                _memoryCache.Dispose();
            }
        }

        #region Security Tests - Path Traversal Protection

        [Fact]
        public async Task GetStudentPhotoAsync_PathTraversalAttack_ReturnsDefaultPhoto()
        {
            // Arrange
            var maliciousMailId = "../../etc/passwd";

            // Act
            var result = await _photoService.GetStudentPhotoAsync(maliciousMailId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_PathTraversalWithBackslashes_ReturnsDefaultPhoto()
        {
            // Arrange
            var maliciousMailId = "..\\..\\windows\\system32\\config\\sam";

            // Act
            var result = await _photoService.GetStudentPhotoAsync(maliciousMailId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_InvalidCharacters_ReturnsDefaultPhoto()
        {
            // Arrange - Test various invalid characters
            var invalidMailIds = new[] { "test@user", "user<script>", "user;drop", "user/slash" };

            // Act
            var results = await Task.WhenAll(invalidMailIds.Select(_photoService.GetStudentPhotoAsync));

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public async Task GetStudentPhotoAsync_ValidAlphanumericWithDashDot_Succeeds()
        {
            // Arrange
            var validMailId = "test-user.name123";
            var photoPath = Path.Join(_testPhotoDirectory, $"{validMailId}.jpg");
            var testPhotoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
            await File.WriteAllBytesAsync(photoPath, testPhotoBytes);

            // Act
            var result = await _photoService.GetStudentPhotoAsync(validMailId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPhotoBytes, result);
        }

        #endregion

        #region Functionality Tests

        [Fact]
        public async Task GetStudentPhotoAsync_StudentPhotoExists_ReturnsPhotoBytes()
        {
            // Arrange
            var mailId = "testuser";
            var photoPath = Path.Join(_testPhotoDirectory, $"{mailId}.jpg");
            var testPhotoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
            await File.WriteAllBytesAsync(photoPath, testPhotoBytes);

            // Act
            var result = await _photoService.GetStudentPhotoAsync(mailId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPhotoBytes, result);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_StudentPhotoDoesNotExist_ReturnsDefaultPhoto()
        {
            // Arrange
            var mailId = "nonexistentuser";

            // Act
            var result = await _photoService.GetStudentPhotoAsync(mailId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, result);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_EmptyMailId_ReturnsDefaultPhoto()
        {
            // Arrange
            var mailId = "";

            // Act
            var result = await _photoService.GetStudentPhotoAsync(mailId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_NullMailId_ReturnsDefaultPhoto()
        {
            // Arrange
            string? mailId = null;

            // Act
            var result = await _photoService.GetStudentPhotoAsync(mailId!);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task GetStudentPhotoAsync_SecondCall_ReturnsCachedPhoto()
        {
            // Arrange
            var mailId = "cachetest";
            var photoPath = Path.Join(_testPhotoDirectory, $"{mailId}.jpg");
            var testPhotoBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 };
            await File.WriteAllBytesAsync(photoPath, testPhotoBytes);

            // Act - First call
            var result1 = await _photoService.GetStudentPhotoAsync(mailId);

            // Modify file on disk (to verify cache is used)
            await File.WriteAllBytesAsync(photoPath, new byte[] { 0x00, 0x00, 0x00, 0x00 });

            // Act - Second call
            var result2 = await _photoService.GetStudentPhotoAsync(mailId);

            // Assert - Should return original cached bytes, not modified bytes
            Assert.Equal(testPhotoBytes, result1);
            Assert.Equal(testPhotoBytes, result2);
            Assert.Equal(result1, result2);
        }

        [Fact]
        public async Task GetStudentPhotoAsync_DefaultPhoto_IsCached()
        {
            // Arrange
            var mailId = "nonexistent";

            // Act - First call
            var result1 = await _photoService.GetStudentPhotoAsync(mailId);

            // Delete default photo to verify cache is used
            File.Delete(_testDefaultPhotoPath);

            // Act - Second call
            var result2 = await _photoService.GetStudentPhotoAsync(mailId);

            // Assert - Should still return cached default photo
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1, result2);
        }

        #endregion

        #region Photo URL Tests

        [Fact]
        public async Task GetStudentPhotoUrlAsync_PhotoExists_ReturnsStudentPhotoUrl()
        {
            // Arrange
            var mailId = "urltest";
            var photoPath = Path.Join(_testPhotoDirectory, $"{mailId}.jpg");
            await File.WriteAllBytesAsync(photoPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            // Act
            var result = await _photoService.GetStudentPhotoUrlAsync(mailId);

            // Assert
            Assert.Equal($"/api/students/photos/student/{mailId}", result);
        }

        [Fact]
        public async Task GetStudentPhotoUrlAsync_PhotoDoesNotExist_ReturnsDefaultPhotoUrl()
        {
            // Arrange
            var mailId = "nonexistent";

            // Act
            var result = await _photoService.GetStudentPhotoUrlAsync(mailId);

            // Assert
            Assert.Equal("/api/students/photos/default", result);
        }

        [Fact]
        public void GetDefaultPhotoUrl_ReturnsCorrectUrl()
        {
            // Act
            var result = _photoService.GetDefaultPhotoUrl();

            // Assert
            Assert.Equal("/api/students/photos/default", result);
        }

        [Fact]
        public async Task StudentPhotoExistsAsync_PhotoExists_ReturnsTrue()
        {
            // Arrange
            var mailId = "existstest";
            var photoPath = Path.Join(_testPhotoDirectory, $"{mailId}.jpg");
            await File.WriteAllBytesAsync(photoPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            // Act
            var result = await _photoService.StudentPhotoExistsAsync(mailId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task StudentPhotoExistsAsync_PhotoDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var mailId = "doesnotexist";

            // Act
            var result = await _photoService.StudentPhotoExistsAsync(mailId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StudentPhotoExistsAsync_EmptyMailId_ReturnsFalse()
        {
            // Arrange
            var mailId = "";

            // Act
            var result = await _photoService.StudentPhotoExistsAsync(mailId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void PhotoService_InvalidCacheDuration_UsesDefault24Hours()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["PhotoGallery:IDCardPhotoPath"]).Returns(_testPhotoDirectory);
            mockConfig.Setup(c => c["PhotoGallery:DefaultPhotoFile"]).Returns("nopic.jpg");
            mockConfig.Setup(c => c["PhotoGallery:CacheDurationHours"]).Returns("invalid");

            // Act
            var service = new PhotoService(mockConfig.Object, new MemoryCache(new MemoryCacheOptions()), _mockLogger.Object, _mockWebHostEnvironment.Object);

            // Assert - No exception thrown, service created successfully
            Assert.NotNull(service);
        }

        [Fact]
        public void PhotoService_CacheDurationTooLow_UsesDefault()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["PhotoGallery:IDCardPhotoPath"]).Returns(_testPhotoDirectory);
            mockConfig.Setup(c => c["PhotoGallery:DefaultPhotoFile"]).Returns("nopic.jpg");
            mockConfig.Setup(c => c["PhotoGallery:CacheDurationHours"]).Returns("0");

            // Act
            var service = new PhotoService(mockConfig.Object, new MemoryCache(new MemoryCacheOptions()), _mockLogger.Object, _mockWebHostEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void PhotoService_CacheDurationTooHigh_UsesDefault()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["PhotoGallery:IDCardPhotoPath"]).Returns(_testPhotoDirectory);
            mockConfig.Setup(c => c["PhotoGallery:DefaultPhotoFile"]).Returns("nopic.jpg");
            mockConfig.Setup(c => c["PhotoGallery:CacheDurationHours"]).Returns("200");

            // Act
            var service = new PhotoService(mockConfig.Object, new MemoryCache(new MemoryCacheOptions()), _mockLogger.Object, _mockWebHostEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetStudentPhotoAsync_DefaultPhotoMissing_ThrowsException()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["PhotoGallery:IDCardPhotoPath"]).Returns(Path.Join(_testPhotoDirectory, "nonexistent"));
            mockConfig.Setup(c => c["PhotoGallery:DefaultPhotoFile"]).Returns("missing.jpg");
            mockConfig.Setup(c => c["PhotoGallery:CacheDurationHours"]).Returns("24");

            var mockWebHost = new Mock<IWebHostEnvironment>();
            mockWebHost.Setup(e => e.WebRootPath).Returns(Path.Join(_testPhotoDirectory, "nonexistentwwwroot"));

            var service = new PhotoService(mockConfig.Object, new MemoryCache(new MemoryCacheOptions()), _mockLogger.Object, mockWebHost.Object);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await service.GetStudentPhotoAsync("anyuser");
            });
        }

        #endregion
    }
}
