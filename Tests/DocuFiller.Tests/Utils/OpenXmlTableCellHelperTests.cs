using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Utils;
using Xunit;

namespace DocuFiller.Tests.Utils
{
    /// <summary>
    /// OpenXmlTableCellHelper 工具类的单元测试
    /// </summary>
    public class OpenXmlTableCellHelperTests
    {
        /// <summary>
        /// 测试当内容控件在表格单元格中时，IsInTableCell 应返回 true
        /// </summary>
        [Fact]
        public void IsInTableCell_WhenControlInTableCell_ReturnsTrue()
        {
            // Arrange
            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(new SdtProperties())
                        )
                    )
                )
            );

            var sdtRun = table.Descendants<SdtRun>().First();

            // Act
            var result = OpenXmlTableCellHelper.IsInTableCell(sdtRun);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试当内容控件在正文时，IsInTableCell 应返回 false
        /// </summary>
        [Fact]
        public void IsInTableCell_WhenControlInBody_ReturnsFalse()
        {
            // Arrange
            var body = new Body(
                new Paragraph(
                    new SdtRun(new SdtProperties())
                )
            );

            var sdtRun = body.Descendants<SdtRun>().First();

            // Act
            var result = OpenXmlTableCellHelper.IsInTableCell(sdtRun);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试获取元素所在的父级表格单元格
        /// </summary>
        [Fact]
        public void GetParentTableCell_WhenInTableCell_ReturnsCell()
        {
            // Arrange
            var tableCell = new TableCell(
                new Paragraph(
                    new SdtRun(new SdtProperties())
                )
            );

            var sdtRun = tableCell.Descendants<SdtRun>().First();

            // Act
            var result = OpenXmlTableCellHelper.GetParentTableCell(sdtRun);

            // Assert
            Assert.NotNull(result);
            Assert.Same(tableCell, result);
        }

        /// <summary>
        /// 测试当元素不在表格单元格中时，应返回 null
        /// </summary>
        [Fact]
        public void GetParentTableCell_WhenNotInTableCell_ReturnsNull()
        {
            // Arrange
            var body = new Body(
                new Paragraph(
                    new SdtRun(new SdtProperties())
                )
            );

            var sdtRun = body.Descendants<SdtRun>().First();

            // Act
            var result = OpenXmlTableCellHelper.GetParentTableCell(sdtRun);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试 GetOrCreateSingleParagraph - 单元格已有单个段落
        /// </summary>
        [Fact]
        public void GetOrCreateSingleParagraph_WhenCellHasSingleParagraph_ReturnsExisting()
        {
            // Arrange
            var paragraph = new Paragraph();
            var cell = new TableCell(paragraph);

            // Act
            var result = OpenXmlTableCellHelper.GetOrCreateSingleParagraph(cell);

            // Assert
            Assert.Same(paragraph, result);
            Assert.Single(cell.Elements<Paragraph>());
        }

        /// <summary>
        /// 测试 GetOrCreateSingleParagraph - 单元格为空时创建新段落
        /// </summary>
        [Fact]
        public void GetOrCreateSingleParagraph_WhenCellIsEmpty_CreatesNew()
        {
            // Arrange
            var cell = new TableCell();

            // Act
            var result = OpenXmlTableCellHelper.GetOrCreateSingleParagraph(cell);

            // Assert
            Assert.NotNull(result);
            Assert.Single(cell.Elements<Paragraph>());
        }

        /// <summary>
        /// 测试 GetOrCreateSingleParagraph - 单元格有多个段落时清理并保留一个
        /// </summary>
        [Fact]
        public void GetOrCreateSingleParagraph_WhenCellHasMultipleParagraphs_CleansAndReturnsOne()
        {
            // Arrange
            var para1 = new Paragraph();
            var para2 = new Paragraph();
            var para3 = new Paragraph();
            var cell = new TableCell(para1, para2, para3);

            // Act
            var result = OpenXmlTableCellHelper.GetOrCreateSingleParagraph(cell);

            // Assert
            Assert.NotNull(result);
            Assert.Single(cell.Elements<Paragraph>());
        }

        /// <summary>
        /// 测试 GetParentTableCell - 嵌套表格中的元素应返回直接父级单元格
        /// </summary>
        [Fact]
        public void GetParentTableCell_WhenInNestedTable_ReturnsDirectParentCell()
        {
            // Arrange
            var innerCell = new TableCell(
                new Paragraph(
                    new Run(new Text("Inner content"))
                )
            );

            var outerCell = new TableCell(
                new Paragraph(),
                new Table(
                    new TableRow(innerCell)
                )
            );

            var run = innerCell.Descendants<Run>().First();

            // Act
            var result = OpenXmlTableCellHelper.GetParentTableCell(run);

            // Assert
            Assert.NotNull(result);
            Assert.Same(innerCell, result);
            Assert.NotSame(outerCell, result);
        }
    }
}
