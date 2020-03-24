﻿using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.InternalApi.Infrastructure;
using SFA.DAS.ApplyService.InternalApi.Models.Roatp;
using SFA.DAS.ApplyService.InternalApi.Services;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using SFA.DAS.ApplyService.Application.Apply.Roatp;
using System;
using SFA.DAS.ApplyService.InternalApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests
{
    [TestFixture]
    public class RoatpGatewayCriminalComplianceChecksControllerTests
    {
        private Mock<IInternalQnaApiClient> _qnaApiClient;
        private Mock<ICriminalComplianceChecksQuestionLookupService> _lookupService;
        private RoatpGatewayCriminalComplianceChecksController _controller;

        [SetUp]
        public void Before_each_test()
        {
            _qnaApiClient = new Mock<IInternalQnaApiClient>();
            _lookupService = new Mock<ICriminalComplianceChecksQuestionLookupService>();
            _controller = new RoatpGatewayCriminalComplianceChecksController(_qnaApiClient.Object, _lookupService.Object);
        }

        [Test]
        public void Get_question_details_retrieves_question_details_and_answer()
        {
            var gatewayPageId = "Page1";

            var pageDetails = new QnaQuestionDetails
            {
                PageId = "1000",
                QuestionId = "CC-22"
            };

            _lookupService.Setup(x => x.GetQuestionDetailsForGatewayPageId(gatewayPageId)).Returns(pageDetails);

            var qnaPageWithQuestion = new Page
            {
                PageId = "1000",
                Questions = new List<Question>
                {
                    new Question
                    {
                        QuestionId = "CC-22",
                        Label = "lorem ipsum",
                        Input = new Input
                        {
                            Type = "Radio",
                            Options = new List<Option>
                            {
                                new Option
                                {
                                    Value = "Yes",
                                    FurtherQuestions = new List<Question>
                                    {
                                        new Question
                                        {
                                            QuestionId = "CC-22.1",
                                            Input = new Input
                                            {
                                                Type = "Text"
                                            }
                                        }
                                    }
                                },
                                new Option
                                {
                                    Value = "No"
                                }
                            }
                        }
                    }
                },
                PageOfAnswers = new List<PageOfAnswers>
                {
                    new PageOfAnswers
                    {
                        Answers = new List<Answer>
                        {
                            new Answer
                            {
                                QuestionId = "CC-22",
                                Value =  "No"
                            }
                        }
                    }
                }
            };

            _qnaApiClient.Setup(x => x.GetPageBySectionNo(It.IsAny<Guid>(), RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                                                          RoatpWorkflowSectionIds.CriminalComplianceChecks.ChecksOnYourOrganisation, pageDetails.PageId))
                                .ReturnsAsync(qnaPageWithQuestion);

            var result = _controller.GetCriminalComplianceQuestionDetails(Guid.NewGuid(), gatewayPageId).GetAwaiter().GetResult();

            var objectResult = result as OkObjectResult;
            objectResult.Should().NotBeNull();
            var criminalComplianceDetails = objectResult.Value as CriminalComplianceCheckDetails;
            criminalComplianceDetails.Should().NotBeNull();
            criminalComplianceDetails.PageId.Should().Be(qnaPageWithQuestion.PageId);
            criminalComplianceDetails.QuestionText.Should().Be(qnaPageWithQuestion.Questions[0].Label);
            criminalComplianceDetails.QuestionId.Should().Be(qnaPageWithQuestion.Questions[0].QuestionId);
            criminalComplianceDetails.Answer.Should().Be(qnaPageWithQuestion.PageOfAnswers[0].Answers[0].Value);
            criminalComplianceDetails.FurtherQuestionId.Should().BeNull();
            criminalComplianceDetails.FurtherAnswer.Should().BeNull();
        }

        [Test]
        public void Get_question_details_retrieves_question_details_and_answer_with_further_question()
        {
            var gatewayPageId = "Page2";

            var pageDetails = new QnaQuestionDetails
            {
                PageId = "1000",
                QuestionId = "CC-22"
            };

            _lookupService.Setup(x => x.GetQuestionDetailsForGatewayPageId(gatewayPageId)).Returns(pageDetails);

            var qnaPageWithQuestion = new Page
            {
                PageId = "1000",
                Questions = new List<Question>
                {
                    new Question
                    {
                        QuestionId = "CC-22",
                        Label = "lorem ipsum",
                        Input = new Input
                        {
                            Type = "Radio",
                            Options = new List<Option>
                            {
                                new Option
                                {
                                    Value = "Yes",
                                    FurtherQuestions = new List<Question>
                                    {
                                        new Question
                                        {
                                            QuestionId = "CC-22.1",
                                            Input = new Input
                                            {
                                                Type = "Text"
                                            }
                                        }
                                    }
                                },
                                new Option
                                {
                                    Value = "No"
                                }
                            }
                        }
                    }
                },
                PageOfAnswers = new List<PageOfAnswers>
                {
                    new PageOfAnswers
                    {
                        Answers = new List<Answer>
                        {
                            new Answer
                            {
                                QuestionId = "CC-22",
                                Value =  "Yes"
                            },
                            new Answer
                            {
                                QuestionId = "CC-22.1",
                                Value = "Lorem ipsum"
                            }
                        }
                    }
                }
            };

            _qnaApiClient.Setup(x => x.GetPageBySectionNo(It.IsAny<Guid>(), RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                                                          RoatpWorkflowSectionIds.CriminalComplianceChecks.ChecksOnYourOrganisation, pageDetails.PageId))
                                .ReturnsAsync(qnaPageWithQuestion);

            var result = _controller.GetCriminalComplianceQuestionDetails(Guid.NewGuid(), gatewayPageId).GetAwaiter().GetResult();

            var objectResult = result as OkObjectResult;
            objectResult.Should().NotBeNull();
            var criminalComplianceDetails = objectResult.Value as CriminalComplianceCheckDetails;
            criminalComplianceDetails.Should().NotBeNull();
            criminalComplianceDetails.PageId.Should().Be(qnaPageWithQuestion.PageId);
            criminalComplianceDetails.QuestionText.Should().Be(qnaPageWithQuestion.Questions[0].Label);
            criminalComplianceDetails.QuestionId.Should().Be(qnaPageWithQuestion.Questions[0].QuestionId);
            criminalComplianceDetails.Answer.Should().Be(qnaPageWithQuestion.PageOfAnswers[0].Answers[0].Value);
            criminalComplianceDetails.FurtherQuestionId.Should().Be(qnaPageWithQuestion.PageOfAnswers[0].Answers[1].QuestionId);
            criminalComplianceDetails.FurtherAnswer.Should().Be(qnaPageWithQuestion.PageOfAnswers[0].Answers[1].Value);
        }
    }
}