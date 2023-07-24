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
    public class AddUntraslatedFile
    {
        private AdministrationDbContext _context;

        public AddUntraslatedFile(AdministrationDbContext administrationDbContext, ILogger<AddUntraslatedFile> logger)
        {
            _context = administrationDbContext;        
        }

        [FunctionName("AddUntraslatedFile")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            string filename = req.Query["filename"];

            if (string.IsNullOrWhiteSpace(filename) || req == null || string.IsNullOrWhiteSpace(req.Body.ToString()))
            {
                return new BadRequestResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<Dictionary<long, string>>(requestBody);

            foreach (var item in data)
            {
                var existingTranslation = await _context.Translations.FirstOrDefaultAsync(p => p.Id == item.Key);

                if (existingTranslation != null)
                {
                    if (existingTranslation.UntranslatedText != item.Value)
                    {
                        existingTranslation.UntranslatedText = item.Value;
                        existingTranslation.TranslateStatus = "Untranslated";
                        existingTranslation.ChangedDate = DateTime.UtcNow;
                    }
                }
                else
                {
                    var translation = new Translation
                    {
                        Id = item.Key,
                        UntranslatedText = item.Value,
                        TranslateStatus = "Untranslated",
                        FileName = filename,
                        ChangedDate = DateTime.UtcNow
                    };

                    await _context.Translations.AddAsync(translation);
                }
            }

            await _context.SaveChangesAsync();

            return new OkResult();
        }
    }
}
