using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

public class CustomerController(DataContext db, UserManager<AppUser> usrMgr) : Controller
{
  // this controller depends on the DataContext & UserManager classes
  private readonly DataContext _dataContext = db;
  private readonly UserManager<AppUser> _userManager = usrMgr;

  public IActionResult Register() => View();
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async System.Threading.Tasks.Task<IActionResult> Register(CustomerWithPassword customerWithPassword)
  {
    if (ModelState.IsValid)
    {
      Customer customer = new Customer
      {
        CompanyName = customerWithPassword.CompanyName,
        Email = customerWithPassword.Email,
        Address = customerWithPassword.Address,
        City = customerWithPassword.City,
        Region = customerWithPassword.Region,
        PostalCode = customerWithPassword.PostalCode,
        Country = customerWithPassword.Country,
        Phone = customerWithPassword.Phone,
        Fax = customerWithPassword.Fax
      };
      if (_dataContext.Customers.Any(c => c.CompanyName == customer.CompanyName))
      {
        ModelState.AddModelError("", "Company Name must be unique");
      }
      else
      {
        AppUser user = new AppUser
        {
          // email and username are synced - this is by choice
          Email = customer.Email,
          UserName = customer.Email
        };
        // Add user to Identity DB
        IdentityResult result = await _userManager.CreateAsync(user, customerWithPassword.Password);
        if (!result.Succeeded)
        {
          AddErrorsFromResult(result);
        }
        else
        {
          // Assign user to customers Role
          result = await _userManager.AddToRoleAsync(user, "northwind-customer");

          if (!result.Succeeded)
          {
            // Delete User from Identity DB
            await _userManager.DeleteAsync(user);
            AddErrorsFromResult(result);
          }
          else
          {
            // Create customer (Northwind)
            _dataContext.AddCustomer(customer);
            return RedirectToAction("Index", "Home");
          }
        }
      }
    }
    return View();
  }
  [Authorize(Roles = "northwind-customer")]
  public IActionResult Account() => View(_dataContext.Customers.FirstOrDefault(c => c.Email == User.Identity.Name));
    [Authorize(Roles = "northwind-customer"), HttpPost, ValidateAntiForgeryToken]
    public IActionResult Account(Customer customer)
    {
        // Edit customer info
      _dataContext.EditCustomer(customer);
      return RedirectToAction("Index", "Home");
  }
    private void AddErrorsFromResult(IdentityResult result)
    {
      foreach (IdentityError error in result.Errors)
      {
        ModelState.AddModelError("", error.Description);
      }
    }
}