using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Domain.Entities;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.StartApplicationHandlerTests
{
    public class When_any_organisation_starts_application : StartApplicationHandlerTestsBase
    {
        private void Init()
        {
            OrganisationRepository.Setup(r => r.GetUserOrganisation(UserId)).ReturnsAsync(new Organisation
            {
                Id = ApplyingOrganisationId,
                OrganisationType = "",
                RoEPAOApproved = false
            });

            ApplyRepository.Setup(r => r.GetAssets()).ReturnsAsync(new List<Asset> {new Asset {Reference = "REPLACEME", Text = "REPLACEDWITH"}});
        }

        [Test]
        public void Then_QnaData_Should_Be_Updated_With_Assets()
        {
            Init();

            Handler.Handle(new StartApplicationRequest(UserId, ApplicationType), new CancellationToken()).Wait();

            ApplyRepository.Verify(r => r.UpdateSections(It.Is<List<ApplicationSection>>(response => response.Any(
                section =>
                    section.SectionId == 1
                    && section.QnAData.Pages[0].Title == "REPLACEDWITH"))));
        }

        [Test]
        public void Then_All_QnaData_Pages_Should_Be_Required()
        {
            Init();

            Handler.Handle(new StartApplicationRequest(UserId, ApplicationType), new CancellationToken()).Wait();

            ApplyRepository.Verify(r => r.UpdateSections(It.Is<List<ApplicationSection>>(response => response.Any(
                section =>
                    section.SectionId == 1
                    && section.QnAData.Pages.All(p => !p.NotRequired)))));
        }

        [Test]
        public async Task Then_ApplicationId_is_returned()
        {
            Init();

            var result = await Handler.Handle(new StartApplicationRequest(UserId, ApplicationType), CancellationToken.None);

            result.Should().BeOfType<StartApplicationResponse>();
            result.As<StartApplicationResponse>().ApplicationId.Should().Be(ApplicationId);
        }
    }
}