using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.NewAssessorApplicationsHandlerTests
{
    [TestFixture]
    public class NewAssessorApplicationsHandlerTests
    {
        private Mock<IAssessorRepository> _repository;
        private NewAssessorApplicationsHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IAssessorRepository>();
            _handler = new NewAssessorApplicationsHandler(_repository.Object);
        }

        [Test]
        public async Task Get_new_applications_returns_new_applications_for_the_user()
        {
            var expectedUser = "sadjkffgdji";
            var expectedResult = new List<AssessorApplicationSummary>();
            _repository.Setup(x => x.GetNewAssessorApplications(expectedUser,null,null)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new NewAssessorApplicationsRequest(expectedUser,null,null), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
