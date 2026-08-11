using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Acorn.Models.AccountViewModels;

public class ProfileFormViewModel
{
  [Required]
  [DisplayName("Time zone")]
  public string TimeZone { get; set; } = string.Empty;

  public ProfileViewModel AsProfileViewModel()
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

public class ProfileViewModel : ProfileFormViewModel
{
  public required IEnumerable<SelectListItem> TimeZones { get; init; }
}
