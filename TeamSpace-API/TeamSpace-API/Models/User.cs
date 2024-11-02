using System.ComponentModel.DataAnnotations;
using TeamSpace_API.Data;

namespace TeamSpace_API.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    public string Nickname { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    public string Photo { get; set; }  //Base64-строка

    public string Description { get; set; }

    public string Country { get; set; }

    //один пользователь может иметь несколько тегов
    public List<Tag> Tags { get; set; }
}
