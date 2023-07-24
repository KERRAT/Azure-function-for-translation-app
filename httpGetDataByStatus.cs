using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace dbAdministration
{
    public class GetTranslationsByStatus
    {
        private AdministrationDbContext _context;

        public GetTranslationsByStatus(AdministrationDbContext administrationDbContext, ILogger<GetTranslationsByStatus> logger)
        {
            _context = administrationDbContext;
        }

        [FunctionName("GetTranslationsByStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            string status = req.Query["status"];

            if (string.IsNullOrWhiteSpace(status))
            {
                return new BadRequestResult();
            }

            var translations = await _context.Translations.Where(t => t.TranslateStatus == status).ToListAsync();

            if (translations == null || translations.Count == 0)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(JsonSerializer.Serialize(translations));
        }
    }
}
