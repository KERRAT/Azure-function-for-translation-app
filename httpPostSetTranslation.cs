using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using dbAdministration;
using Microsoft.Extensions.Logging;

namespace dbAdministration
{
    public class SetTranslationAndDate
    {
        private AdministrationDbContext _context;

        public SetTranslationAndDate(AdministrationDbContext administrationDbContext)
        {
            _context = administrationDbContext;
        }

        [FunctionName("SetTranslationAndDate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var inputData = JsonSerializer.Deserialize<TranslationInputData>(requestBody);

            if (inputData == null || string.IsNullOrWhiteSpace(inputData.TranslateStatus) ||
                string.IsNullOrWhiteSpace(inputData.TranslatedText) || inputData.Id <= 0)
            {
                return new BadRequestResult();
            }

            var existingTranslation = await _context.Translations.FirstOrDefaultAsync(p => p.Id == inputData.Id);

            if (existingTranslation == null)
            {
                return new NotFoundResult();
            }
            else
            {
                existingTranslation.TranslatedText = inputData.TranslatedText;
                existingTranslation.TranslateStatus = inputData.TranslateStatus;
                existingTranslation.ChangedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new OkResult();
        }

        public class TranslationInputData
        {
            public long Id { get; set; }
            public string TranslatedText { get; set; }
            public string TranslateStatus { get; set; }
        }
    }
}
