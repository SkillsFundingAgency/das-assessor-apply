﻿using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SFA.DAS.ApplyService.Web.ViewModels.Roatp
{
    public class EmployerProviderContinueApplicationViewModel
    {
        [Required(ErrorMessage = "Tell us if you want to continue with this application")]
        public string ContinueWithApplication { get; set; }

        public string LevyPayingEmployer { get; set; }

        public List<ValidationErrorDetail> ErrorMessages { get; set; }
    }
}
