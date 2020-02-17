﻿using MediatR;
using SFA.DAS.ApplyService.Application.Email.Consts;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Application.Users;
using SFA.DAS.ApplyService.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Application.Apply.Submit
{
    public class SubmitApplicationHandler : IRequestHandler<SubmitApplicationRequest, bool>
    {
        private readonly IApplyRepository _applyRepository;
        private readonly IContactRepository _contactRepository;

        public SubmitApplicationHandler(IApplyRepository applyRepository, IContactRepository contactRepository)
        {
            _applyRepository = applyRepository;
            _contactRepository = contactRepository;
        }

        public async Task<bool> Handle(SubmitApplicationRequest request, CancellationToken cancellationToken)
        {
            if (await _applyRepository.CanSubmitApplication(request.ApplicationId))
            {
                var application = await _applyRepository.GetApplication(request.ApplicationId);
                var submittingContact = await _contactRepository.GetContact(request.SubmittingContactId);

                if (application.ApplyData != null && submittingContact != null)
                {
                    application.ApplyData.ApplyDetails = request.ApplyData.ApplyDetails;
                    application.ApplyData.Sequences = request.ApplyData.Sequences;

                    if (string.IsNullOrWhiteSpace(application.ApplyData.ApplyDetails.ReferenceNumber))
                    {
                        application.ApplyData.ApplyDetails.ReferenceNumber = await _applyRepository.GetNextRoatpApplicationReference();
                    }

                    if (application.ApplyData.ApplyDetails.ProviderRoute > 0)
                    {
                        application.ApplyData.ApplyDetails.ProviderRoute = request.ProviderRoute;
                    }
                    
                    application.ApplyData.ApplyDetails.ApplicationSubmittedOn = DateTime.UtcNow;
                    application.ApplyData.ApplyDetails.ApplicationSubmittedBy = submittingContact.Id;

                    foreach(var sequence in application.ApplyData.Sequences)
                    {
                        if (!sequence.NotRequired)
                        {
                            sequence.Status = ApplicationStatus.Submitted;
                        }
                    }
                    
                    await _applyRepository.SubmitApplication(application.ApplicationId, application.ApplyData, submittingContact.Id);

                    return true;
                }
            }

            return false;
        }
    }
}
