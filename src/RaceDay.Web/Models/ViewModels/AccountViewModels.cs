using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.ViewModels;

// Validation attributes here give instant, field-level feedback in the browser and on the server.
// The API still performs its own validation, which remains authoritative.

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Enter your first name."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your mobile number.")]
    [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number, e.g. 082 555 0123.")]
    [Display(Name = "Mobile number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your date of birth.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Choose a password."), StringLength(100, MinimumLength = 6, ErrorMessage = "Use at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Enter your first name."), StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name."), StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your mobile number.")]
    [RegularExpression(@"^\+?[0-9 ]{10,15}$", ErrorMessage = "Enter a valid phone number, e.g. 082 555 0123.")]
    [Display(Name = "Mobile number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your date of birth.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    // Read-only details shown alongside the form.
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
