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
using System.Linq;
using System.Text.Json.Serialization;

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

            if (string.IsNullOrWhiteSpace(filename) || req.Body == null)
            {
                return new BadRequestResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<Dictionary<long, string>>(requestBody, new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                });

            if (data == null || data.Count == 0)
            {
                return new BadRequestResult();
            }

            var idsToSearch = data.Keys;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var allIdsInDatabase = await _context.Translations.Select(p => p.Id).ToListAsync();
                var idsToDelete = allIdsInDatabase.Except(idsToSearch);
                _context.Translations.RemoveRange(_context.Translations.Where(p => idsToDelete.Contains(p.Id)));

                var existingTranslations = await _context.Translations.Where(p => idsToSearch.Contains(p.Id)).ToListAsync();

                foreach (var item in data)
                {
                    var existingTranslation = existingTranslations.FirstOrDefault(p => p.Id == item.Key);

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
                await transaction.CommitAsync();
            }

            return new OkResult();
        }
    }
}
