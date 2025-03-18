using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Model;


[Table("USER")]
public class User
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("ID")]
    public int Id { get; set; }
    
    [Column("PASSWORD")]
    public int Password { get; set; }
    
    [Column("USERNAME", TypeName = "string"), StringLength(20)]
    public string Username { get; set; }
    
}