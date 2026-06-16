
using System.ComponentModel.DataAnnotations;


public class FarmerLoginViewModel
{
    [Required]
    public string FarmerCode { get; set; }

    [Required]
    public string Password { get; set; }
}