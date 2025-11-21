using System.ComponentModel.DataAnnotations;
public class CustomerWithPassword : Customer
{
    [UIHint("password"), Required]
    public string Password { get; set; }
}