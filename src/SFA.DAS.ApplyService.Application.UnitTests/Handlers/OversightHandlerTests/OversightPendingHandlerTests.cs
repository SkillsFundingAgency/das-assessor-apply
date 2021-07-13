using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Oversight;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Domain.Interfaces;
using SFA.DAS.ApplyService.Domain.QueryResults;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.OversightHandlerTests
{

    [TestFixture]
    public class OversightPendingHandlerTests
    {
        protected Mock<IOversightReviewQueries> OversightReviewQueries;
        protected GetOversightsPendingHandler PendingHandler;
        protected string FirstOrganisationName = "XXX Limited";
        protected string SecondOrganisationName = "ZZZ Limited";
        [SetUp]
        public void Setup()
        {
            OversightReviewQueries = new Mock<IOversightReviewQueries>();
            OversightReviewQueries.Setup(r => r.GetPendingOversightReviews(null,null,null)).ReturnsAsync(() => new PendingOversightReviews());
            PendingHandler = new GetOversightsPendingHandler(OversightReviewQueries.Object);
          }

        [Test]
        public void Check_pending_results_are_as_expected()
        {
            var completedApplications = new PendingOversightReviews
            {
                Reviews = new List<PendingOversightReview> { 
                new PendingOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName =FirstOrganisationName,
                    Ukprn = "12344321",
                    ProviderRoute = "Main",
                    ApplicationReferenceNumber = "APR000111",
                    ApplicationSubmittedDate = DateTime.Today.AddDays(-1)
                },
                new PendingOversightReview
                {
                    ApplicationId = Guid.NewGuid(),
                    OrganisationName = SecondOrganisationName,
                    Ukprn = "43211234",
                    ProviderRoute = "Employer",
                    ApplicationReferenceNumber = "APR000112",
                    ApplicationSubmittedDate = DateTime.Today.AddDays(-1)
                }
            }};

            OversightReviewQueries.Setup(r => r.GetPendingOversightReviews(null,null,null)).ReturnsAsync(completedApplications);

            var result = PendingHandler.Handle(new GetOversightsPendingRequest(null,null,null), new CancellationToken()).GetAwaiter().GetResult();

            Assert.AreEqual(2,result.Reviews.Count);
            Assert.AreEqual(1,result.Reviews.Count(x=> x.ApplicationId == completedApplications.Reviews.First().ApplicationId));
            Assert.AreEqual(1, result.Reviews.Count(x => x.OrganisationName==FirstOrganisationName));
            Assert.AreEqual(1, result.Reviews.Count(x => x.OrganisationName == SecondOrganisationName));
        }
    }
}
