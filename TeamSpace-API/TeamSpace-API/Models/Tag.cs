using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TeamSpace_API.Models;

public class Tag
{
    [Key]
    public int TagId { get; set; }

    [Required]
    public string Title { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }
}
