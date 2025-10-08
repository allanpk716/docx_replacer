using System;

namespace DocuFiller.Exceptions
{
    /// <summary>
    /// 文档处理过程中发生的异常
    /// </summary>
    public class DocumentProcessingException : Exception
    {
        public string? DocumentPath { get; }

        public DocumentProcessingException() : base()
        {
        }

        public DocumentProcessingException(string message) : base(message)
        {
        }

        public DocumentProcessingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DocumentProcessingException(string message, string documentPath) : base(message)
        {
            DocumentPath = documentPath;
        }

        public DocumentProcessingException(string message, string documentPath, Exception innerException) : base(message, innerException)
        {
            DocumentPath = documentPath;
        }
    }
}