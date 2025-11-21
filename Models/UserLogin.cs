using System.ComponentModel.DataAnnotations;

public class UserLogin
{
    [Required, UIHint("email")]
    public string Email { get; set; }

    [Required, UIHint("password")]
    public string Password { get; set; }
}