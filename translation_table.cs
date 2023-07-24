using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbAdministration
{
    [Table("translation", Schema = "public")]
    public class Translation
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("untranslated_text")]
        public string UntranslatedText { get; set; }

        [Column("translated_text")]
        public string TranslatedText { get; set; }

        [Column("translate_status")]
        public string TranslateStatus { get; set; }

        [Column("changed_date")]
        public DateTime ChangedDate { get; set; }

        [Column("file_name")]
        public string FileName { get; set; }
    }
}
