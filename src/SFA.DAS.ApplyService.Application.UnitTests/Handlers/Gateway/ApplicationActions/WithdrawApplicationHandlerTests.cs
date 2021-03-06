﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Gateway.ApplicationActions;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Types;
using SFA.DAS.ApplyService.Domain.Interfaces;
using SFA.DAS.ApplyService.EmailService.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.Gateway.ApplicationActions
{
    [TestFixture]
    public class WithdrawApplicationHandlerTests
    {
        private WithdrawApplicationHandler _handler;
        private Mock<IGatewayRepository> _gatewayRepository;
        private Mock<IOversightReviewRepository> _oversightReviewRepository;
        private Mock<IAuditService> _auditService;
        private Mock<IApplicationUpdatedEmailService> _emailService;

        private Guid _applicationId;
        private const string _comments = "comments";
        private const string _userId = "userId";
        private const string _userName = "_userName";

        [SetUp]
        public void SetUp()
        {
            _applicationId = Guid.NewGuid();

            _gatewayRepository = new Mock<IGatewayRepository>();
            _oversightReviewRepository = new Mock<IOversightReviewRepository>();
            _auditService = new Mock<IAuditService>();
            _emailService = new Mock<IApplicationUpdatedEmailService>();
            var logger = Mock.Of<ILogger<WithdrawApplicationHandler>>();
            
            _handler = new WithdrawApplicationHandler(_gatewayRepository.Object, logger, _oversightReviewRepository.Object, _auditService.Object, _emailService.Object);
        }

        [Test]
        public async Task Handler_withdraws_application()
        {
            await _handler.Handle(new WithdrawApplicationCommand(_applicationId, _comments, _userId, _userName), CancellationToken.None);
            _gatewayRepository.Verify(x => x.WithdrawApplication(_applicationId, _comments, _userId, _userName), Times.Once);
        }

        [Test]
        public async Task Handler_adds_oversight_review()
        {
            await _handler.Handle(new WithdrawApplicationCommand(_applicationId, _comments, _userId, _userName), CancellationToken.None);

            _oversightReviewRepository.Verify(x => 
                x.Add(It.Is<OversightReview>(or => or.ApplicationId == _applicationId
                    && or.Status == OversightReviewStatus.Withdrawn
                    && or.InternalComments == _comments
                    && or.UserId == _userId
                    && or.UserName == _userName)), 
                Times.Once);
        }

        [Test]
        public async Task Handler_sends_updated_email()
        {
            _gatewayRepository.Setup(x => x.WithdrawApplication(_applicationId, _comments, _userId, _userName)).Returns(Task.FromResult(true));

            await _handler.Handle(new WithdrawApplicationCommand(_applicationId, _comments, _userId, _userName), CancellationToken.None);

            _emailService.Verify(x => x.SendEmail(It.Is<Guid>(id => id == _applicationId)), Times.Once);
        }
    }
}
