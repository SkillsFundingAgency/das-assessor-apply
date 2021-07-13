using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using KellermanSoftware.CompareNetObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Oversight;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Commands.CreateAppeal;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Commands.UploadAppealFile;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetAppeal;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetAppealUpload;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetOversightDetails;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetOversightReview;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetStagedFiles;
using SFA.DAS.ApplyService.Domain.QueryResults;
using SFA.DAS.ApplyService.InternalApi.Controllers;
using SFA.DAS.ApplyService.InternalApi.Services;
using SFA.DAS.ApplyService.InternalApi.Types.Requests.Oversight;
using SFA.DAS.ApplyService.InternalApi.Types.Responses.Oversight;
using SFA.DAS.ApplyService.Types;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests
{
    [TestFixture]
    public class OversightControllerTests
    {
        private Mock<IMediator> _mediator;
        private Mock<IRegistrationDetailsService> _service;
        private OversightController _controller;
        private static readonly Fixture AutoFixture = new Fixture();

        [SetUp]
        public void Before_each_test()
        {
            _mediator = new Mock<IMediator>();
            _service = new Mock<IRegistrationDetailsService>();
            _controller = new OversightController(_mediator.Object, _service.Object);
        }

        [Test]
        public async Task Check_count_of_pending_results_are_correct()
        {
            var pendingOversights = new PendingOversightReviews 
            {
                Reviews = new List<PendingOversightReview> { 
                new PendingOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName = "XXX Limited",
                    Ukprn = "12344321",
                    ProviderRoute = "Main",
                    ApplicationReferenceNumber = "APR000111"
                },
                new PendingOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName = "ZZZ Limited",
                    Ukprn = "43211234",
                    ProviderRoute = "Employer",
                    ApplicationReferenceNumber = "APR000112",
                }
            }};

            _mediator
                .Setup(x => x.Send(It.IsAny<GetOversightsPendingRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingOversights);

            var actualResult = await _controller.OversightsPending(null,null,null);
            Assert.AreEqual(pendingOversights.Reviews.Count, actualResult.Value.Reviews.Count);
        }

        [Test]
        public async Task Check_pending_results_are_as_expected()
        {
            var pendingOversights = new PendingOversightReviews
            {
                Reviews = new List<PendingOversightReview>
                {
                    new PendingOversightReview{
                        ApplicationId = Guid.NewGuid(),
                        OrganisationName = "XXX Limited",
                        Ukprn = "12344321",
                        ProviderRoute = "Main",
                        ApplicationReferenceNumber = "APR000111"
                    }
                }
            };

            _mediator
                .Setup(x => x.Send(It.IsAny<GetOversightsPendingRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingOversights);

            var actualResult = await _controller.OversightsPending(null,null,null);
            var returnedOversight = actualResult.Value.Reviews.First();

            Assert.That(returnedOversight,Is.SameAs(pendingOversights.Reviews.First()));
        }

        [Test]
        public async Task Check_count_of_completed_results_are_correct()
        {
            var completedOversights = new CompletedOversightReviews
            {
                Reviews = new List<CompletedOversightReview> {
                new CompletedOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName = "XXX Limited",
                    Ukprn = "12344321",
                    ProviderRoute = "Main",
                    ApplicationReferenceNumber = "APR000111"
                },
                new CompletedOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName = "ZZZ Limited",
                    Ukprn = "43211234",
                    ProviderRoute = "Employer",
                    ApplicationReferenceNumber = "APR000112",
                }
            }
            };

            _mediator
                .Setup(x => x.Send(It.IsAny<GetOversightsCompletedRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(completedOversights);

            var actualResult = await _controller.OversightsCompleted(null, null, null);
            Assert.AreEqual(completedOversights.Reviews.Count, actualResult.Value.Reviews.Count);
        }

        [Test]
        public async Task Check_completed_results_are_as_expected()
        {
            var completedOversights = new CompletedOversightReviews
            {
                Reviews = new List<CompletedOversightReview>
                {
                    new CompletedOversightReview{
                        ApplicationId = Guid.NewGuid(),
                        OrganisationName = "XXX Limited",
                        Ukprn = "12344321",
                        ProviderRoute = "Main",
                        ApplicationReferenceNumber = "APR000111"
                    }
                }
            };

            _mediator
                .Setup(x => x.Send(It.IsAny<GetOversightsCompletedRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(completedOversights);

            var actualResult = await _controller.OversightsCompleted(null, null, null);
            var returnedOversight = actualResult.Value.Reviews.First();

            Assert.That(returnedOversight, Is.SameAs(completedOversights.Reviews.First()));
        }

        [Test]
        public async Task Check_oversight_details_result_is_as_expected()
        {
            var applicationId = Guid.NewGuid();

            var oversight = new ApplicationOversightDetails
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                OrganisationName = "XXX Limited",
                Ukprn = "12344321",
                ProviderRoute = "Main",
                ApplicationReferenceNumber = "APR000111",
                OversightStatus = OversightReviewStatus.InProgress
            };

            _mediator
                .Setup(x => x.Send(It.IsAny<GetOversightApplicationDetailsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(oversight);

            var actualResult = await _controller.OversightDetails(applicationId);

            var returnedOversight = actualResult.Value;

            Assert.That(returnedOversight, Is.SameAs(oversight));
        }

        [TestCase(OversightReviewStatus.Successful)]
        [TestCase(OversightReviewStatus.Unsuccessful)]
        public async Task Record_oversight_outcome_updates_oversight_status_and_determined_date(OversightReviewStatus oversightStatus)
        {
            var command = new RecordOversightOutcomeCommand
            {
                OversightStatus = oversightStatus,
                ApplicationId = Guid.NewGuid(),
                UserId = "User Id",
                UserName = "Test user"
            };

            _mediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _controller.RecordOversightOutcome(command);
            result.Should().NotBeNull();
            result.Value.Should().BeTrue();
        }

        [Test]
        public async Task Record_oversight_gateway_fail_outcome_updates_oversight_status_and_determined_date()
        {
            var command = new RecordOversightGatewayFailOutcomeCommand
            {
                ApplicationId = Guid.NewGuid(),
                UserId = "User Id",
                UserName = "Test user"
            };

            _mediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.RecordOversightGatewayFailOutcome(command);
            result.Should().BeOfType<OkResult>();
        }


        [Test]
        public async Task Record_oversight_gateway_removed_outcome_updates_oversight_status_and_determined_date()
        {
            var command = new RecordOversightGatewayRemovedOutcomeCommand
            {
                ApplicationId = Guid.NewGuid(),
                UserId = "User Id",
                UserName = "Test user"
            };

            _mediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.RecordOversightGatewayRemovedOutcome(command);
            result.Should().BeOfType<OkResult>();
        }

        [Test]
        public async Task Get_registration_details_retrieves_information_for_adding_provider_to_register()
        {
            var applicationId = Guid.NewGuid();
            var response = new RoatpRegistrationDetails
            {
                UKPRN = "10001234",
                LegalName = "Legal Name",
                TradingName = "Trading Name",
                ProviderTypeId = 1,
                OrganisationTypeId = 2,
                CompanyNumber = "5550003",
                CharityNumber = "125622"
            };

            _service.Setup(x => x.GetRegistrationDetails(applicationId)).ReturnsAsync(response);

            var result = await _controller.GetRegistrationDetails(applicationId);

            result.Value.UKPRN.Should().Be(response.UKPRN);
        }

        [Test]
        public async Task Upload_Appeal_File_Adds_Uploaded_File_To_Application()
        {
            var request = new UploadAppealFileRequest
            {
                ApplicationId = AutoFixture.Create<Guid>(),
                File = GenerateFile(),
                UserId = AutoFixture.Create<string>(),
                UserName = AutoFixture.Create<string>()
            };

            _mediator.Setup(x => x.Send(It.IsAny<UploadAppealFileCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.UploadAppealFile(request);
            result.Should().BeOfType<OkResult>();

            string expectedFileData;
            using (var reader = new StreamReader(request.File.OpenReadStream()))
            {
                expectedFileData = await reader.ReadToEndAsync();
            }

            _mediator.Verify(x => x.Send(It.Is<UploadAppealFileCommand>(c =>
                    c.ApplicationId == request.ApplicationId
                    && c.UserId == request.UserId
                    && c.UserName == request.UserName
                    && c.File.Filename == request.File.FileName
                    && Encoding.UTF8.GetString(c.File.Data) == expectedFileData),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Remove_Appeal_File_Removes_Uploaded_File_From_Application()
        {
            var applicationId = AutoFixture.Create<Guid>();
            var fileId = AutoFixture.Create<Guid>();

            var request = new RemoveAppealFileRequest
            {
                UserId = AutoFixture.Create<string>(),
                UserName = AutoFixture.Create<string>()
            };

            _mediator.Setup(x => x.Send(It.IsAny<RemoveAppealFileCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.RemoveAppealFile(applicationId, fileId, request);
            result.Should().BeOfType<OkResult>();

            _mediator.Verify(
                x => x.Send(It.Is<RemoveAppealFileCommand>(c =>
                    c.ApplicationId == applicationId
                    && c.FileId == fileId
                    && c.UserId == request.UserId
                    && c.UserName == request.UserName), 
    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AppealUploads_Gets_Files_For_Application_Appeal()
        {
            var request = new GetStagedFilesRequest();
            var queryResult = new GetStagedFilesQueryResult
            {
                Files = new List<GetStagedFilesQueryResult.AppealFile>
                {
                    new GetStagedFilesQueryResult.AppealFile{ Id = Guid.NewGuid(), Filename = AutoFixture.Create<string>()}
                }
            };

            _mediator.Setup(x => x.Send(It.IsAny<GetStagedFilesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

            var result = await _controller.StagedUploads(request);
            result.Should().BeOfType<ActionResult<GetStagedFilesResponse>>();

            var compareLogic = new CompareLogic(new ComparisonConfig { IgnoreObjectTypes = true });
            var comparisonResult = compareLogic.Compare(queryResult, result);
            Assert.IsTrue(comparisonResult.AreEqual);
        }

        [Test]
        public async Task CreateAppeal_Adds_Appeal_To_Oversight_Review()
        {
            var applicationId = AutoFixture.Create<Guid>();
            var oversightReviewId = AutoFixture.Create<Guid>();
            var request = AutoFixture.Create<CreateAppealRequest>();

            _mediator.Setup(x => x.Send(It.IsAny<CreateAppealCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.CreateAppeal(applicationId, oversightReviewId, request);
            Assert.IsInstanceOf<OkResult>(result);

            _mediator.Verify(x => x.Send(It.Is<CreateAppealCommand>(c =>
                c.ApplicationId == applicationId &&
                c.OversightReviewId == oversightReviewId &&
                c.Message == request.Message &&
                c.UserId == request.UserId &&
                c.UserName == request.UserName), It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task GetAppeal_Gets_Appeal_For_Application_And_OversightReview()
        {
            var request = new GetAppealRequest();
            var queryResult = AutoFixture.Create<GetAppealQueryResult>();

            _mediator.Setup(x => x.Send(It.IsAny<GetAppealQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

            var result = await _controller.GetAppeal(request);
            result.Should().BeOfType<ActionResult<GetAppealResponse>>();

            var compareLogic = new CompareLogic(new ComparisonConfig { IgnoreObjectTypes = true });
            var comparisonResult = compareLogic.Compare(queryResult, result);
            Assert.IsTrue(comparisonResult.AreEqual);
        }

        [Test]
        public async Task GetAppealUpload_Gets_AppealUpload_For_Application_Appeal()
        {
            var request = new GetAppealUploadRequest();
            var queryResult = AutoFixture.Create<GetAppealUploadQueryResult>();

            _mediator.Setup(x => x.Send(It.IsAny<GetAppealUploadQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

            var result = await _controller.GetAppealUpload(request);
            result.Should().BeOfType<ActionResult<GetAppealUploadResponse>>();

            Assert.AreEqual(queryResult.Filename, result.Value.Filename);
            Assert.AreEqual(queryResult.Content, result.Value.Content);
            Assert.AreEqual(queryResult.ContentType, result.Value.ContentType);
        }

        [Test]
        public async Task OversightReview_Gets_OversightReview_For_Application()
        {
            var request = new GetOversightReviewRequest();
            var queryResult = AutoFixture.Create<GetOversightReviewQueryResult>();

            _mediator.Setup(x => x.Send(It.IsAny<GetOversightReviewQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

            var result = await _controller.OversightReview(request);
            result.Should().BeOfType<ActionResult<GetOversightReviewResponse>>();

            var compareLogic = new CompareLogic(new ComparisonConfig { IgnoreObjectTypes = true });
            var comparisonResult = compareLogic.Compare(queryResult, result);
            Assert.IsTrue(comparisonResult.AreEqual);
        }

        private static IFormFile GenerateFile()
        {
            var fileName = AutoFixture.Create<string>();
            var content = AutoFixture.Create<string>();
            return new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(content)),
                0,
                content.Length,
                fileName,
                fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }
    }
}
