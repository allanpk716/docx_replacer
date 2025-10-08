using System;
using DocuFiller.Utils;

namespace DocuFiller.Exceptions
{
    /// <summary>
    /// 模板验证过程中发生的异常
    /// </summary>
    public class TemplateValidationException : Exception
    {
        public string? TemplatePath { get; }
        public ValidationResult? ValidationResult { get; }

        public TemplateValidationException() : base()
        {
        }

        public TemplateValidationException(string message) : base(message)
        {
        }

        public TemplateValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public TemplateValidationException(string message, string templatePath) : base(message)
        {
            TemplatePath = templatePath;
        }

        public TemplateValidationException(string message, string templatePath, ValidationResult validationResult) : base(message)
        {
            TemplatePath = templatePath;
            ValidationResult = validationResult;
        }

        public TemplateValidationException(string message, string templatePath, Exception innerException) : base(message, innerException)
        {
            TemplatePath = templatePath;
        }
    }
}