using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Northwind.Controllers
{
    public class APIController(DataContext db) : Controller
    {
        // this controller depends on the NorthwindRepository
        private readonly DataContext _dataContext = db;

        [HttpGet, Route("api/product")]
        // returns all products
        public IEnumerable<Product> Get() => _dataContext.Products.OrderBy(p => p.ProductName);
        [HttpGet, Route("api/product/{id}")]
        // returns specific product
        public Product Get(int id) => _dataContext.Products.FirstOrDefault(p => p.ProductId == id);
        [HttpGet, Route("api/product/discontinued/{discontinued}")]
        // returns all products where discontinued = true/false
        public IEnumerable<Product> GetDiscontinued(bool discontinued) => _dataContext.Products.Where(p => p.Discontinued == discontinued).OrderBy(p => p.ProductName);
        [HttpGet, Route("api/category/{CategoryId}/product")]
        // returns all products in a specific category
        public IEnumerable<Product> GetByCategory(int CategoryId) => _dataContext.Products.Where(p => p.CategoryId == CategoryId).OrderBy(p => p.ProductName);
        [HttpGet, Route("api/category/{CategoryId}/product/discontinued/{discontinued}")]
        // returns all products in a specific category where discontinued = true/false
        public IEnumerable<Product> GetByCategoryDiscontinued(int CategoryId, bool discontinued) => _dataContext.Products.Where(p => p.CategoryId == CategoryId && p.Discontinued == discontinued).OrderBy(p => p.ProductName);
        [HttpPost, Route("api/addtocart")]
        // adds a row to the cartitem table
        public CartItem Post([FromBody] CartItemJSON cartItem) => _dataContext.AddToCart(cartItem);

        [HttpGet, Route("api/cart/{email}")]
        [Authorize(Roles = "northwind-customer")]
        // gets all cart items for a customer
        public IEnumerable<object> GetCart(string email)
        {
            var cartItems = _dataContext.GetCartItems(email);
            return cartItems.Select(ci => new
            {
                cartItemId = ci.CartItemId,
                productId = ci.ProductId,
                productName = ci.Product.ProductName,
                unitPrice = ci.Product.UnitPrice,
                quantity = ci.Quantity,
                subtotal = ci.Product.UnitPrice * ci.Quantity,
                discount = _dataContext.GetActiveProductDiscount(ci.ProductId)
            });
        }

        [HttpPut, Route("api/cart/update")]
        [Authorize(Roles = "northwind-customer")]
        // updates cart item quantity
        public IActionResult UpdateCartQuantity([FromBody] dynamic data)
        {
            try
            {
                int cartItemId = data.cartItemId;
                int quantity = data.quantity;
                
                if (quantity <= 0)
                {
                    return BadRequest("Quantity must be greater than 0");
                }

                _dataContext.UpdateCartItemQuantity(cartItemId, quantity);
                return Ok(new { success = true });
            }
            catch
            {
                return BadRequest(new { success = false });
            }
        }

        [HttpDelete, Route("api/cart/remove/{cartItemId}")]
        [Authorize(Roles = "northwind-customer")]
        // removes cart item
        public IActionResult RemoveFromCart(int cartItemId)
        {
            try
            {
                _dataContext.RemoveFromCart(cartItemId);
                return Ok(new { success = true });
            }
            catch
            {
                return BadRequest(new { success = false });
            }
        }

        [HttpGet, Route("api/cart/count/{email}")]
        [Authorize(Roles = "northwind-customer")]
        // gets cart item count for badge
        public int GetCartCount(string email)
        {
            return _dataContext.GetCartItems(email).Sum(ci => ci.Quantity);
        }
    }
}