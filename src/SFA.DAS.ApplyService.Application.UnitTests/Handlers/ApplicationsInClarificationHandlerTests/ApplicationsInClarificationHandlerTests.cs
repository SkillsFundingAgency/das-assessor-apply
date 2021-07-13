using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.ApplicationsInClarificationHandlerTests
{
    [TestFixture]
    public class ApplicationsInClarificationHandlerTests
    {
        private Mock<IAssessorRepository> _repository;
        private ApplicationsInClarificationHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IAssessorRepository>();
            _handler = new ApplicationsInClarificationHandler(_repository.Object);
        }

        [Test]
        public async Task Get_applications_in_clarification_returns_applications()
        {
            var expectedUser = "sadjkffgdji";
            var expectedResult = new List<ClarificationApplicationSummary>();
            _repository.Setup(x => x.GetApplicationsInClarification(null,null)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new ApplicationsInClarificationRequest(expectedUser,null,null), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
