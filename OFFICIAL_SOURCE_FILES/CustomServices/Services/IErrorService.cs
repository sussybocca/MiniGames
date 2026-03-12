using System.Threading.Tasks;

namespace MiniGames.CustomServices.Services;

public interface IErrorService
{
    Task SubmitErrorReportAsync(object report); // or strongly typed model
}