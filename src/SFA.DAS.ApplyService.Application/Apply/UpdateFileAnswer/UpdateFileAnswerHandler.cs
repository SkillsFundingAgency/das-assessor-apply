using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;

namespace SFA.DAS.ApplyService.Application.Apply.UpdateFileAnswer
{
    public class UpdateFileAnswerHandler : IRequestHandler<UpdateFileAnswerRequest>
    {
        private readonly IApplyRepository _applyRepository;

        public UpdateFileAnswerHandler(IApplyRepository applyRepository)
        {
            _applyRepository = applyRepository;
        }
        
        
        public async Task<Unit> Handle(UpdateFileAnswerRequest request, CancellationToken cancellationToken)
        {
            var section = await _applyRepository.GetSection(request.ApplicationId, request.SequenceId, request.SectionId,
                request.UserId);

            //            var entity = await _applyRepository.GetEntity(request.ApplicationId, request.UserId);
            //            var workflow = entity.QnAWorkflow;
            //
            //            var sequence = workflow.GetSequenceContainingPage(request.PageId);
            //            var section = sequence.Sections.Single(s => s.Pages.Any(p => p.PageId == request.PageId));
            //
            //            if (!sequence.Active)
            //            {
            //                throw new BadRequestException("Sequence not active");
            //            }
            //
            var page = section.QnAData.Pages.Single(p => p.PageId == request.PageId);
            page.DisplayType = section.DisplayType;
            var existingAnswers = page.PageOfAnswers;

            var qnADataObject = section.QnAData;
            
            existingAnswers.Add(new PageOfAnswers() {Id = Guid.NewGuid(), Answers = new List<Answer>() {new Answer() {QuestionId = request.QuestionId, Value = request.FileName}}});
            
            qnADataObject.Pages.ForEach(p =>
            {
                if (p.PageId == request.PageId)
                {
                    p.Complete = page.Complete;
                    p.PageOfAnswers = page.PageOfAnswers;
                    p.Feedback = page.Feedback;
                }
            });

            qnADataObject.FinancialApplicationGrade = null; // Remove any previous grade as it doesn't reflect the new answers
            section.QnAData = qnADataObject;

            await _applyRepository.SaveSection(section, request.UserId);
            
            
            return Unit.Value;
        }
    }
}