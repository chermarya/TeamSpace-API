using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamSpace_API.Data;

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