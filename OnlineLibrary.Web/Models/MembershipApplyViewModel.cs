using System.ComponentModel.DataAnnotations;

public class MembershipApplyViewModel
{
    [Required]
    public int DurationMonths { get; set; }

}
