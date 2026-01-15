using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// SafeFormattedContentReplacer 服务的单元测试
    /// </summary>
    public class SafeFormattedContentReplacerTests
    {
        private readonly SafeFormattedContentReplacer _replacer;

        public SafeFormattedContentReplacerTests()
        {
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = new Logger<SafeFormattedContentReplacer>(loggerFactory);
            _replacer = new SafeFormattedContentReplacer(logger);
        }

        /// <summary>
        /// 测试表格单元格中的格式化内容替换时保留富文本格式
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInTableCell_PreservesStructure()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(
                            new Run(new Text("old")),
                            new Run(new Text(" text"))
                        )
                    )
                )
            );

            var formattedValue = new FormattedCellValue
            {
                Fragments = new System.Collections.Generic.List<TextFragment>
                {
                    new TextFragment { Text = "H", IsSuperscript = false, IsSubscript = false },
                    new TextFragment { Text = "2", IsSuperscript = true, IsSubscript = false },
                    new TextFragment { Text = "O", IsSuperscript = false, IsSubscript = false }
                }
            };

            var originalChildCount = cell.ChildElements.Count;

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceFormattedContentInControl(control, formattedValue);

            // Assert
            // 验证单元格子元素数量没有增加
            Assert.Equal(originalChildCount, cell.ChildElements.Count);
            // 验证有三个 Run（每个片段一个 Run）
            var runs = cell.Descendants<Run>().ToList();
            Assert.Equal(3, runs.Count);
            // 验证文本内容正确
            Assert.Equal("H", runs[0].GetFirstChild<Text>()?.Text);
            Assert.Equal("2", runs[1].GetFirstChild<Text>()?.Text);
            Assert.Equal("O", runs[2].GetFirstChild<Text>()?.Text);
            // 验证第二个 Run 有上标格式
            Assert.NotNull(runs[1].RunProperties);
            Assert.NotNull(runs[1].RunProperties?.VerticalTextAlignment);
            Assert.Equal(VerticalPositionValues.Superscript, runs[1].RunProperties?.VerticalTextAlignment?.Val?.Value);
        }

        /// <summary>
        /// 测试上标格式设置
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInTableCell_Superscript_AppliesCorrectFormat()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(new Run(new Text("old")))
                    )
                )
            );

            var formattedValue = new FormattedCellValue
            {
                Fragments = new System.Collections.Generic.List<TextFragment>
                {
                    new TextFragment { Text = "X", IsSuperscript = true }
                }
            };

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceFormattedContentInControl(control, formattedValue);

            // Assert
            var run = cell.Descendants<Run>().First();
            var runProperties = run.RunProperties;
            Assert.NotNull(runProperties);
            Assert.NotNull(runProperties.VerticalTextAlignment);
            Assert.Equal(VerticalPositionValues.Superscript, runProperties.VerticalTextAlignment.Val.Value);
        }

        /// <summary>
        /// 测试下标格式设置
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInTableCell_Subscript_AppliesCorrectFormat()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(new Run(new Text("old")))
                    )
                )
            );

            var formattedValue = new FormattedCellValue
            {
                Fragments = new System.Collections.Generic.List<TextFragment>
                {
                    new TextFragment { Text = "X", IsSubscript = true }
                }
            };

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceFormattedContentInControl(control, formattedValue);

            // Assert
            var run = cell.Descendants<Run>().First();
            var runProperties = run.RunProperties;
            Assert.NotNull(runProperties);
            Assert.NotNull(runProperties.VerticalTextAlignment);
            Assert.Equal(VerticalPositionValues.Subscript, runProperties.VerticalTextAlignment.Val.Value);
        }

        /// <summary>
        /// 测试多个 Run 合并为多个 Run，每个片段一个 Run，保留格式
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInTableCell_MultipleRuns_CreatesMultipleRunWithFormatting()
        {
            // Arrange
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(
                    new Run(new Text("part1")),
                    new Run(new Text("part2"))
                )
            );

            var formattedValue = new FormattedCellValue
            {
                Fragments = new System.Collections.Generic.List<TextFragment>
                {
                    new TextFragment { Text = "A" },
                    new TextFragment { Text = "B" }
                }
            };

            // Act
            _replacer.ReplaceFormattedContentInControl(sdtRun, formattedValue);

            // Assert
            var runs = sdtRun.Descendants<Run>().ToList();
            // 应该有两个 Run（每个片段一个）
            Assert.Equal(2, runs.Count);
            Assert.Equal("A", runs[0].GetFirstChild<Text>()?.Text);
            Assert.Equal("B", runs[1].GetFirstChild<Text>()?.Text);
        }

        /// <summary>
        /// 测试空格式化值替换
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInControl_EmptyValue_ClearsContent()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(new Run(new Text("old text")))
                    )
                )
            );

            var formattedValue = new FormattedCellValue
            {
                Fragments = new System.Collections.Generic.List<TextFragment>()
            };

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceFormattedContentInControl(control, formattedValue);

            // Assert
            var runs = cell.Descendants<Run>().ToList();
            // 应该没有 Run（因为没有任何片段）
            Assert.Empty(runs);
        }

        /// <summary>
        /// 测试传入 null 参数时抛出异常
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInControl_NullControl_ThrowsArgumentNullException()
        {
            // Arrange
            var formattedValue = FormattedCellValue.FromPlainText("test");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _replacer.ReplaceFormattedContentInControl(null!, formattedValue));
        }

        /// <summary>
        /// 测试传入 null 格式化值时抛出异常
        /// </summary>
        [Fact]
        public void ReplaceFormattedContentInControl_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(new Run(new Text("old")))
                    )
                )
            );

            // Act & Assert
            var control = cell.Descendants<SdtRun>().First();
            Assert.Throws<ArgumentNullException>(() => _replacer.ReplaceFormattedContentInControl(control, null!));
        }
    }
}
