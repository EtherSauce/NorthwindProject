using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

public class AccountController(UserManager<AppUser> userMgr, SignInManager<AppUser> signInMgr) : Controller
{
    private readonly UserManager<AppUser> _userManager = userMgr;
    private readonly SignInManager<AppUser> _signInManager = signInMgr;
    public IActionResult Login(string returnUrl)
    {
        // return url remembers the user's original request
        ViewBag.returnUrl = returnUrl;
        return View();
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UserLogin details, string returnUrl)
    {
      if (ModelState.IsValid)
      {
        AppUser user = await _userManager.FindByEmailAsync(details.Email);
        if (user != null)
        {
          await _signInManager.SignOutAsync();
          Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user, details.Password, false, false);
          if (result.Succeeded)
          {
            return Redirect(returnUrl ?? "/");
          }
        }
        ModelState.AddModelError(nameof(UserLogin.Email), "Invalid user or password");
      }
      return View(details);
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
      await _signInManager.SignOutAsync();
      return RedirectToAction("Index", "Home");
    }

    public ViewResult AccessDenied() => View();
}