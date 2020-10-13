﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Roatp;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;
using SFA.DAS.ApplyService.Web.Configuration;
using SFA.DAS.ApplyService.Web.Infrastructure;
using SFA.DAS.ApplyService.Web.Infrastructure.Services;

namespace SFA.DAS.ApplyService.Web.UnitTests.Services
{
    [TestFixture]
    public class ResetRouteQuestionServiceTests
    {
        private ResetRouteQuestionsService _service;
        private Mock<IQnaApiClient> _qnaApiClient;

        private Guid _applicationId;

        [SetUp]
        public void Before_each_test()
        {
            _applicationId = Guid.NewGuid();
            _qnaApiClient = new Mock<IQnaApiClient>();
        }
        [Test]
        public void Check_reset_for_main_route_resets_employer_and_supporting_questions()
        {
            var routeId = ApplicationRoute.MainProviderApplicationRoute;

            _service = new ResetRouteQuestionsService(_qnaApiClient.Object);
            _service.ResetRouteQuestions(_applicationId, routeId).GetAwaiter().GetResult();

            _qnaApiClient.Verify(x=>x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EmployerStartPage));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId, 1, 4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Employer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2,  RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Supporting));

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.ApplicationFrameworks_Supporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OrganisationTransition_Supporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OnlyDeliveringApprenticeshipFrameworks_Supporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Main), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.MainSupportingStartPage), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAMainSupporting),Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAMainSupporting), Times.Never);
        }


        [Test]
        public void Check_reset_for_supporting_route_resets_employer_and_main_questions()
        {
            var routeId = ApplicationRoute.SupportingProviderApplicationRoute;

            _service = new ResetRouteQuestionsService(_qnaApiClient.Object);
            _service.ResetRouteQuestions(_applicationId, routeId).GetAwaiter().GetResult();

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EmployerStartPage));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Employer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Main));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.ApplicationFrameworks_MainEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OrganisationTransition_MainEmployer));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OnlyDeliveringApprenticeshipFrameworks_MainEmployer));

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Supporting),Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.MainSupportingStartPage), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAMainSupporting), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAMainSupporting), Times.Never);
        }

        [Test]
        public void Check_reset_for_employer_route_resets_supporting_and_main_questions()
        {
            var routeId = ApplicationRoute.EmployerProviderApplicationRoute;

            _service = new ResetRouteQuestionsService(_qnaApiClient.Object);
            _service.ResetRouteQuestions(_applicationId, routeId).GetAwaiter().GetResult();

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.MainSupportingStartPage));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeMainSupporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeMainSupporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolMainSupporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAMainSupporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAMainSupporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Main));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Supporting));

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.ApplicationFrameworks_Supporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OrganisationTransition_Supporting));
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.OnlyDeliveringApprenticeshipFrameworks_Supporting));

            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,6,2, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.TypeOfApprenticeshipTraining_Employer),Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EmployerStartPage), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.EducationalInstituteTypeEmployer), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.PublicBodyTypeEmployer), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.SchoolEmployer), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.RegisteredESFAEmployer), Times.Never);
            _qnaApiClient.Verify(x => x.ResetPageAnswersBySequenceAndSectionNumber(_applicationId,1,4, RoatpWorkflowPageIds.DescribeYourOrganisation.FundedESFAEmployer), Times.Never);
        }
    }
}