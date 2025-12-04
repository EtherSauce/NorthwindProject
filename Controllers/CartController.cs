using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "northwind-customer")]
public class CartController(DataContext db) : Controller
{
    private readonly DataContext _dataContext = db;

    public IActionResult Index()
    {
        var cartItems = _dataContext.GetCartItems(User.Identity.Name);
        ViewBag.CartTotal = CalculateCartTotal(cartItems);
        return View(cartItems);
    }

    public IActionResult Checkout()
    {
        var cartItems = _dataContext.GetCartItems(User.Identity.Name);
        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index");
        }
        
        ViewBag.CartTotal = CalculateCartTotal(cartItems);
        ViewBag.DiscountTotal = CalculateDiscountTotal(cartItems);
        ViewBag.FinalTotal = ViewBag.CartTotal - ViewBag.DiscountTotal;
        return View(cartItems);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ProcessCheckout()
    {
        var order = _dataContext.CreateOrderFromCart(User.Identity.Name);
        if (order != null)
        {
            TempData["Success"] = $"Order #{order.OrderId} has been placed successfully!";
            return RedirectToAction("OrderConfirmation", new { id = order.OrderId });
        }
        
        TempData["Error"] = "There was an error processing your order.";
        return RedirectToAction("Index");
    }

    public IActionResult OrderConfirmation(int id)
    {
        var order = _dataContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.OrderId == id);
            
        if (order == null || order.Customer.Email != User.Identity.Name)
        {
            return NotFound();
        }
        
        return View(order);
    }

    private decimal CalculateCartTotal(IEnumerable<CartItem> cartItems)
    {
        return cartItems.Sum(ci => ci.Product.UnitPrice * ci.Quantity);
    }

    private decimal CalculateDiscountTotal(IEnumerable<CartItem> cartItems)
    {
        decimal discountTotal = 0;
        foreach (var item in cartItems)
        {
            var discount = _dataContext.GetActiveProductDiscount(item.ProductId);
            if (discount != null)
            {
                discountTotal += item.Product.UnitPrice * item.Quantity * discount.DiscountPercent;
            }
        }
        return discountTotal;
    }
}