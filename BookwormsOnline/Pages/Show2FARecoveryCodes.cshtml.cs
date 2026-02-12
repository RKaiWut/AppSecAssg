using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookwormsOnline.Pages
{
    [Authorize]
    public class Show2FARecoveryCodesModel : PageModel
    {
        public string[] RecoveryCodes { get; set; }

        public IActionResult OnGet()
        {
            var codes = TempData["RecoveryCodes"] as string;
            if (string.IsNullOrEmpty(codes))
            {
                return RedirectToPage("./Index");
            }

            RecoveryCodes = codes.Split(',');
            return Page();
        }
    }
}
