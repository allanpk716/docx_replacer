using DocuFiller.Models;
using Xunit;
using Xunit.Abstractions;

namespace DocuFiller.Tests
{
    public class HeaderFooterIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public HeaderFooterIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ContentControlLocation_Enum_HasCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)ContentControlLocation.Body);
            Assert.Equal(1, (int)ContentControlLocation.Header);
            Assert.Equal(2, (int)ContentControlLocation.Footer);
        }

        [Fact]
        public void ContentControlData_LocationProperty_DefaultsToBody()
        {
            // Arrange & Act
            var data = new ContentControlData();

            // Assert
            Assert.Equal(ContentControlLocation.Body, data.Location);
        }

        [Fact]
        public void ContentControlData_CanSetLocationProperty()
        {
            // Arrange
            var data = new ContentControlData();

            // Act
            data.Location = ContentControlLocation.Header;

            // Assert
            Assert.Equal(ContentControlLocation.Header, data.Location);
        }
    }
}
