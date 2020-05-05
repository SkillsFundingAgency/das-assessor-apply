﻿using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class GetPageReviewOutcomeHandlerRequest : IRequest<PageReviewOutcome>
    {
        public GetPageReviewOutcomeHandlerRequest(Guid applicationId,
                                                    int sequenceNumber,
                                                    int sectionNumber,
                                                    string pageId,
                                                    int assessorType,
                                                    string userId)
        {
            ApplicationId = applicationId;
            SequenceNumber = sequenceNumber;
            SectionNumber = sectionNumber;
            PageId = pageId;
            AssessorType = assessorType;
            UserId = userId;
        }

        public Guid ApplicationId { get; set; }
        public int SequenceNumber { get; set; }
        public int SectionNumber { get; set; }
        public string PageId { get; set; }
        public int AssessorType { get; set; }
        public string UserId { get; set; }
    }
}
