﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway
{
    public class UpdateGatewayReviewStatusAsClarificationHandler : IRequestHandler<UpdateGatewayReviewStatusAsClarificationRequest, bool>
    {
        private readonly IApplyRepository _applyRepository;
        private readonly IGatewayRepository _gatewayRepository;

        public UpdateGatewayReviewStatusAsClarificationHandler(IApplyRepository applyRepository, IGatewayRepository gatewayRepository)
        {
            _applyRepository = applyRepository;
            _gatewayRepository = gatewayRepository;
        }

        public async Task<bool> Handle(UpdateGatewayReviewStatusAsClarificationRequest request, CancellationToken cancellationToken)
        {
            var application = await _applyRepository.GetApplication(request.ApplicationId);

            if (application == null) return false;

            if (application.ApplyData == null)
                application.ApplyData = new ApplyData();

            if (application.ApplyData.GatewayReviewDetails == null)
            {
                application.ApplyData.GatewayReviewDetails = new ApplyGatewayDetails();
            }

            application.ApplyData.GatewayReviewDetails.ClarificationRequestedOn = DateTime.UtcNow;
            application.ApplyData.GatewayReviewDetails.ClarificationRequestedBy = request.UserId;

            return await _gatewayRepository.UpdateGatewayReviewStatusAndComment(request.ApplicationId,
                application.ApplyData, GatewayReviewStatus.ClarificationSent, request.UserId, request.UserName);
        }
    }
}
