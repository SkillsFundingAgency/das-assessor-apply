﻿using SFA.DAS.ApplyService.Application.Apply.Roatp;
using System;
using System.ComponentModel.DataAnnotations;

namespace SFA.DAS.ApplyService.Web.ViewModels.Roatp
{
    public class SoleTraderOrPartnershipViewModel : WhosInControlViewModel, IPageViewModel
    {
        public const string OrganisationTypeSoleTrader = "Sole trader";
        public const string OrganisationTypePartnership = "Partnership";

        public Guid ApplicationId { get; set; }
        [Required(ErrorMessage = "Tell us what your organisation is")]
        public string OrganisationType { get; set; }

        public string Title { get { return "Tell us your organisation's type"; } set { } }
        public string SequenceId { get { return RoatpWorkflowSequenceIds.YourOrganisation.ToString(); } set { } }
        public int SectionId { get { return RoatpWorkflowSectionIds.YourOrganisation.WhosInControl; } set { } }
        public string PageId { get; set; }
        public string GetHelpQuestion { get; set; }
        public bool GetHelpQuerySubmitted { get; set; }
        public string GetHelpErrorMessage { get; set; }
    }
}
