using System.ComponentModel.DataAnnotations;

namespace OnlineAdService.Common
{
    public enum Category
    {
        Cars,
        [Display(Name = "Real Estate")]
        RealEstate,
        [Display(Name = "Free Stuff")]
        FreeStuff
    }
}
