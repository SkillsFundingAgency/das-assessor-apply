using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.ApplicationsInModerationHandlerTests
{
    [TestFixture]
    public class ApplicationsInModerationHandlerTests
    {
        private Mock<IAssessorRepository> _repository;
        private ApplicationsInModerationHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IAssessorRepository>();
            _handler = new ApplicationsInModerationHandler(_repository.Object);
        }

        [Test]
        public async Task Get_applications_in_moderation_returns_applications()
        {
            var expectedUser = "sadjkffgdji";
            var expectedResult = new List<ModerationApplicationSummary>();
            _repository.Setup(x => x.GetApplicationsInModeration(null, null, null)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new ApplicationsInModerationRequest(expectedUser, null, null, null), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
