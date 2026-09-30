using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the editable fields for a user's profile.</summary>
public class ProfileFormViewModel
{
  /// <summary>Gets or sets the user's preferred time zone identifier.</summary>
  [Required]
  [DisplayName("Time zone")]
  public string TimeZone { get; set; } = string.Empty;

  internal ProfileViewModel AsProfileViewModel()
  {
    return new ProfileViewModel()
    {
      TimeZone = TimeZone,
      TimeZones = [
        new SelectListItem(string.Empty, string.Empty),

        .. TimeZoneInfo
          .GetSystemTimeZones()
          .Select(tz => new SelectListItem(tz.DisplayName, tz.Id))
      ]
    };
  }
}

internal class ProfileViewModel : ProfileFormViewModel
{
  public required IEnumerable<SelectListItem> TimeZones { get; init; }
}
