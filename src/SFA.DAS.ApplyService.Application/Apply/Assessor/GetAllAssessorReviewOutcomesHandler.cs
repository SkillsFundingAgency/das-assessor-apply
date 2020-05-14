﻿using MediatR;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Domain.Apply;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class GetAllAssessorReviewOutcomesHandler : IRequestHandler<GetAllAssessorReviewOutcomesRequest, List<PageReviewOutcome>>
    {
        private readonly IAssessorRepository _repository;
        private readonly ILogger<GetAllAssessorReviewOutcomesHandler> _logger;

        public GetAllAssessorReviewOutcomesHandler(IAssessorRepository repository, ILogger<GetAllAssessorReviewOutcomesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<PageReviewOutcome>> Handle(GetAllAssessorReviewOutcomesRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"GetAllAssessorReviewOutcomes for ApplicationId '{request.ApplicationId}' - " +
                                                    $"AssessorType '{request.AssessorType}' - UserId '{request.UserId}'");

            var assessorReviewOutcomes = await _repository.GetAllAssessorReviewOutcomes(request.ApplicationId,
                                                                                        request.AssessorType,
                                                                                        request.UserId);

            return assessorReviewOutcomes;
        }
    }
}