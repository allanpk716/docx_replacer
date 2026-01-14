using Xunit;
using DocuFiller.Models;

namespace DocuFiller.Tests
{
    public class FormattedCellValueTests
    {
        [Fact]
        public void FromPlainText_CreatesSingleFragment()
        {
            // Arrange & Act
            var value = FormattedCellValue.FromPlainText("test text");

            // Assert
            Assert.Single(value.Fragments);
            Assert.Equal("test text", value.Fragments[0].Text);
            Assert.False(value.Fragments[0].IsSuperscript);
            Assert.False(value.Fragments[0].IsSubscript);
        }

        [Fact]
        public void PlainText_ReturnsConcatenatedFragments()
        {
            // Arrange
            var value = new FormattedCellValue
            {
                Fragments = new List<TextFragment>
                {
                    new TextFragment { Text = "2x10" },
                    new TextFragment { Text = "9", IsSuperscript = true }
                }
            };

            // Act & Assert
            Assert.Equal("2x109", value.PlainText);
        }

        [Fact]
        public void FromPlainText_WithNull_ReturnsEmptyFragment()
        {
            // Arrange & Act
            var value = FormattedCellValue.FromPlainText(null);

            // Assert
            Assert.Single(value.Fragments);
            Assert.Equal("", value.Fragments[0].Text);
        }
    }
}
