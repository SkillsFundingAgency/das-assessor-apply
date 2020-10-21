﻿using System;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Web.ViewModels.Roatp
{
    public class TaskList2ViewModel
    {
        public Guid ApplicationId { get; set; }
        public ApplicationSummaryViewModel ApplicationSummaryViewModel { get; set; }

        public bool ShowSubmission { get; set; }
        public bool AllowSubmission { get; set; }

        public List<Sequence> Sequences { get; set; }

        public TaskList2ViewModel()
        {
            Sequences = new List<Sequence>();
        }


        public class Sequence
        {
            public int Id { get; set; }
            public string Description { get; set; }

            public List<Section> Sections { get; set; }

            public Sequence()
            {
                Sections = new List<Section>();
            }
        }

        public class Section
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public bool IsNotRequired { get; set; }
            public string Status { get; set; }
            public string StatusCssClass => Status == TaskListSectionStatus.Blank ? "hidden" : Status.Replace(" ", "").ToLower();
            public bool IsLocked { get; set; }
        }

    }
}
