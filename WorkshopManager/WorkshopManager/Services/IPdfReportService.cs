using System.Threading.Tasks;

namespace WorkshopManager.Services
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateOpenOrdersReportAsync();
        Task<byte[]> GenerateServiceOrderReportAsync(int orderId);
    }
}