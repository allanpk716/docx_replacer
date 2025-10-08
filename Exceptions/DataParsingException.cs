using System;

namespace DocuFiller.Exceptions
{
    /// <summary>
    /// 数据解析过程中发生的异常
    /// </summary>
    public class DataParsingException : Exception
    {
        public string? DataFilePath { get; }
        public int? LineNumber { get; }

        public DataParsingException() : base()
        {
        }

        public DataParsingException(string message) : base(message)
        {
        }

        public DataParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DataParsingException(string message, string dataFilePath) : base(message)
        {
            DataFilePath = dataFilePath;
        }

        public DataParsingException(string message, string dataFilePath, int lineNumber) : base(message)
        {
            DataFilePath = dataFilePath;
            LineNumber = lineNumber;
        }

        public DataParsingException(string message, string dataFilePath, Exception innerException) : base(message, innerException)
        {
            DataFilePath = dataFilePath;
        }
    }
}