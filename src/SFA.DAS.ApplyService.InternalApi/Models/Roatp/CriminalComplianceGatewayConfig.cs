﻿namespace SFA.DAS.ApplyService.InternalApi.Models.Roatp
{
    public class CriminalComplianceGatewayConfig
    {
        public string GatewayPageId { get; set; }
        public int SectionId { get; set; }
        public string QnaPageId { get; set; }
        public string QnaQuestionId { get; set; }
    }

    public class CriminalComplianceGatewayOverrideConfig : CriminalComplianceGatewayConfig
    {

    }
}
