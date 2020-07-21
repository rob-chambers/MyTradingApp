using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTradingApp.Domain
{
    public class Setting
    {        
        [Key]
        [Column("varchar(25)")]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
