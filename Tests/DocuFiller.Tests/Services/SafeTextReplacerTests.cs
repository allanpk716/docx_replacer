using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// SafeTextReplacer 服务的单元测试
    /// </summary>
    public class SafeTextReplacerTests
    {
        private readonly SafeTextReplacer _replacer;

        public SafeTextReplacerTests()
        {
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = new Logger<SafeTextReplacer>(loggerFactory);
            _replacer = new SafeTextReplacer(logger);
        }

        /// <summary>
        /// 测试简单文本替换功能
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_SimpleText_ReplacesCorrectly()
        {
            // Arrange
            var run = new Run(new Text("old text"));
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(run)
            );

            // Act
            _replacer.ReplaceTextInControl(sdtRun, "new text");

            // Assert
            // 从内容控件中重新获取 Run 和 Text 元素
            var updatedRun = sdtRun.Descendants<Run>().First();
            var newText = updatedRun.Descendants<Text>().First();
            Assert.Equal("new text", newText.Text);
        }

        /// <summary>
        /// 测试表格单元格中的内容控件替换时保留表格结构
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_PreservesTableCellStructure()
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

            var originalChildCount = cell.ChildElements.Count;

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceTextInControl(control, "new");

            // Assert
            // 验证单元格子元素数量没有增加
            Assert.Equal(originalChildCount, cell.ChildElements.Count);
            // 验证文本被替换
            var newText = cell.Descendants<Text>().First();
            Assert.Equal("new", newText.Text);
        }

        /// <summary>
        /// 测试多个 Run 合并为一个 Run
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_MultipleRuns_MergesIntoSingleRun()
        {
            // Arrange
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(
                    new Run(new Text("part1")),
                    new Run(new Text("part2"))
                )
            );

            // Act
            _replacer.ReplaceTextInControl(sdtRun, "new text");

            // Assert
            var runs = sdtRun.Descendants<Run>().ToList();
            // 应该只剩一个 Run
            Assert.Single(runs);
            Assert.Equal("new text", runs[0].Descendants<Text>().First().Text);
        }

        /// <summary>
        /// 测试空文本替换
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_EmptyText_ClearsContent()
        {
            // Arrange
            var run = new Run(new Text("old text"));
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(run)
            );

            // Act
            _replacer.ReplaceTextInControl(sdtRun, "");

            // Assert
            // 从内容控件中重新获取 Run 和 Text 元素
            var updatedRun = sdtRun.Descendants<Run>().First();
            var newText = updatedRun.Descendants<Text>().First();
            Assert.Equal("", newText.Text);
        }

        /// <summary>
        /// 测试 SdtBlock 类型的内容控件替换
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_SdtBlock_ReplacesCorrectly()
        {
            // Arrange
            var sdtBlock = new SdtBlock(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("old text")))
                )
            );

            // Act
            _replacer.ReplaceTextInControl(sdtBlock, "new text");

            // Assert
            var newText = sdtBlock.Descendants<Text>().First();
            Assert.Equal("new text", newText.Text);
        }

        [Fact]
        public void ReplaceTextInControl_SdtCellWrappingTableCell_PreservesColumns()
        {
            var wrappedCellControl = new SdtCell(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentCell(
                    new TableCell(
                        new Paragraph(
                            new Run(new Text("old"))
                        )
                    )
                )
            );

            var otherCell = new TableCell(
                new Paragraph(
                    new Run(new Text("keep"))
                )
            );

            var row = new TableRow(wrappedCellControl, otherCell);
            var table = new Table(row);

            _replacer.ReplaceTextInControl(wrappedCellControl, "new");

            var rowCellLikeCount = row.ChildElements.Count(e => e is TableCell || e is SdtCell);
            Assert.Equal(2, rowCellLikeCount);

            var replacedText = wrappedCellControl.Descendants<Text>().First().Text;
            Assert.Equal("new", replacedText);

            var otherText = otherCell.Descendants<Text>().First().Text;
            Assert.Equal("keep", otherText);
        }

        /// <summary>
        /// 测试表格单元格中的内容控件替换时保留段落结构
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_InTableCell_PreservesParagraphStructure()
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

            // Act
            var control = cell.Descendants<SdtRun>().First();
            _replacer.ReplaceTextInControl(control, "new");

            // Assert
            // 验证单元格中仍然只有一个段落
            var paragraphs = cell.Elements<Paragraph>().ToList();
            Assert.Single(paragraphs);
            // 验证文本被替换
            var newText = cell.Descendants<Text>().First();
            Assert.Equal("new", newText.Text);
        }

        /// <summary>
        /// 测试传入 null 参数时抛出异常
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_NullControl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _replacer.ReplaceTextInControl(null!, "test"));
        }
    }
}
