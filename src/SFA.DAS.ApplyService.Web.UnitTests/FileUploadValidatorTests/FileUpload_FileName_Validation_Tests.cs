using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Validation;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Web.Infrastructure;

namespace SFA.DAS.ApplyService.Web.UnitTests.FileUploadValidatorTests
{
    [TestFixture]
    public class FileUpload_FileName_Validation_Tests
    {
        [TestCase("pdf,application/pdf", "This.File has too many full stops.pdf", true)]
        [TestCase("pdf,application/pdf", "This File has just the right number full stops.pdf", true)]
        [TestCase("pdf,application/pdf", "This File should not be uploaded.exe", false)]
        public void Then_validation_should_return_correct_result(string validationValue, string filename, bool validationResult)
        {
            var page = new Page()
            {
                Questions = new List<Question>
                {
                    new Question()
                    {
                        QuestionId = "Q1",
                        Input = new Input()
                        {
                            Type = "FileUpload", 
                            Validations = new List<ValidationDefinition>()
                            {
                                new ValidationDefinition() {Name = "FileType", Value = validationValue, ErrorMessage = "File must be a PDF"}
                            }
                        }
                    }
                }
            };
            var files = new FormFileCollection();

            var content = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            var ms = new MemoryStream(content);
            ms.Read(content, 0, content.Length);

            files.Add(new FormFile(ms, 0, ms.Length, "Q1", filename)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            });
            var result = FileValidator.FileValidationPassed(new List<Answer>(), page, new List<ValidationErrorDetail>(), new ModelStateDictionary(), files);

            result.Should().Be(validationResult);
        }
    }
}