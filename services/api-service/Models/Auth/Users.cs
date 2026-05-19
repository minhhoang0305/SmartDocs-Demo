namespace api_service.Models;
public class Users
{
    public int ID {get; set;}
    public string Username {get; set;} = default!;
    public string Email {get; set;} = default!;
    public string Password {get; set;} = default!;
    public DateTime CreateAt {get; set;}
}