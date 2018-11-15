using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Application
{
    public interface IEmailService
    {
        Task SendEmail(string ToAddress, int emailId, object replacements);

        Task SendPreAmbleEmail(string toAddress, int emailId, object replacements);
    }
}