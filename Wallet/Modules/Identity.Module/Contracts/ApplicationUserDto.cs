using System.ComponentModel.DataAnnotations;

namespace Identity.Contracts;

 class ApplicationUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    [EmailAddress]
    public string Email { get; set; }
}
