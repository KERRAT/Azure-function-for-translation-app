using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace dbAdministration
{
    public class GetTranslations
    {
        private AdministrationDbContext _context;

        public GetTranslations(AdministrationDbContext administrationDbContext, ILogger<GetTranslations> logger)
        {
            _context = administrationDbContext;
        }

        [FunctionName("GetTranslations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            string status = req.Query["status"];
            string untranslatedText = req.Query["untranslatedText"];
            string translatedText = req.Query["translatedText"];
            string fileName = req.Query["fileName"];
            DateTimeOffset? sinceDate = ParseDate(req.Query["sinceDate"]);
            DateTimeOffset? untilDate = ParseDate(req.Query["untilDate"]);

            int pageNumber = int.TryParse(req.Query["pageNumber"], out int pn) ? pn : 1; // За замовчуванням 1
            int pageSize = int.TryParse(req.Query["pageSize"], out int ps) ? ps : 20; // За замовчуванням 20

            var translationsQuery = _context.Translations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                translationsQuery = translationsQuery.Where(t => t.TranslateStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(untranslatedText))
            {
                translationsQuery = translationsQuery.Where(t => t.UntranslatedText.Contains(untranslatedText));
            }

            if (!string.IsNullOrWhiteSpace(translatedText))
            {
                translationsQuery = translationsQuery.Where(t => t.TranslatedText.Contains(translatedText));
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                translationsQuery = translationsQuery.Where(t => t.FileName == fileName);
            }

            if (sinceDate.HasValue)
            {
                translationsQuery = translationsQuery.Where(t => t.ChangedDate >= sinceDate.Value.UtcDateTime);
            }

            if (untilDate.HasValue)
            {
                translationsQuery = translationsQuery.Where(t => t.ChangedDate <= untilDate.Value.UtcDateTime);
            }

            translationsQuery = translationsQuery
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize);

            var translations = await translationsQuery.ToListAsync();


            if (translations == null || translations.Count == 0)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(JsonSerializer.Serialize(translations));
        }

        private DateTimeOffset? ParseDate(string dateInput)
        {
            if (string.IsNullOrWhiteSpace(dateInput))
            {
                return null;
            }

            DateTimeOffset dateOutput;
            if (DateTimeOffset.TryParse(dateInput, out dateOutput))
            {
                if (dateInput.Length <= 10) // Date only, no time part
                {
                    dateOutput = new DateTimeOffset(dateOutput.Year, dateOutput.Month, dateOutput.Day, 0, 0, 0, dateOutput.Offset);
                }
                else if (dateInput.Length <= 16) // Date and hours and minutes, no seconds
                {
                    dateOutput = new DateTimeOffset(dateOutput.Year, dateOutput.Month, dateOutput.Day, dateOutput.Hour, dateOutput.Minute, 0, dateOutput.Offset);
                }

                return dateOutput;
            }

            return null;
        }
    }
}
