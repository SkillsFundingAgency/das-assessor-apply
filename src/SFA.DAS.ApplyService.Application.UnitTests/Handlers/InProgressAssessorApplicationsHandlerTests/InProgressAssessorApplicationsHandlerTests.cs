using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.InProgressAssessorApplicationsHandlerTests
{
    [TestFixture]
    public class InProgressAssessorApplicationsHandlerTests
    {
        private Mock<IAssessorRepository> _repository;
        private InProgressAssessorApplicationsHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IAssessorRepository>();
            _handler = new InProgressAssessorApplicationsHandler(_repository.Object);
        }

        [Test]
        public async Task Get_in_progress_applications_returns_in_progress_applications_for_the_user()
        {
            var expectedUser = "sadjkffgdji";
            var expectedResult = new List<AssessorApplicationSummary>();
            _repository.Setup(x => x.GetInProgressAssessorApplications(expectedUser,null,null)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new InProgressAssessorApplicationsRequest(expectedUser,null,null), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
