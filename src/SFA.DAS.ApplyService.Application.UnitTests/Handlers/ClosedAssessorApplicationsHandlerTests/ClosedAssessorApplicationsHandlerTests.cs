using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.ClosedAssessorApplicationsHandlerTests
{
    [TestFixture]
    public class ClosedAssessorApplicationsHandlerTests
    {
        private Mock<IAssessorRepository> _repository;
        private ClosedAssessorApplicationsHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IAssessorRepository>();
            _handler = new ClosedAssessorApplicationsHandler(_repository.Object);
        }

        [Test]
        public async Task Get_new_applications_returns_new_applications_for_the_user()
        {
            var expectedUser = "sadjkffgdji";
            var expectedResult = new List<ClosedApplicationSummary>();
            _repository.Setup(x => x.GetClosedApplications(null, null, null)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new ClosedAssessorApplicationsRequest(expectedUser, null, null, null), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
