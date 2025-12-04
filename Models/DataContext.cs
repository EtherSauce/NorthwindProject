using Microsoft.EntityFrameworkCore;

public class DataContext : DbContext
{
  public DataContext(DbContextOptions<DataContext> options) : base(options) { }

  public DbSet<Product> Products { get; set; }
  public DbSet<Category> Categories { get; set; }
  public DbSet<Discount> Discounts { get; set; }
  public DbSet<Customer> Customers { get; set; }
  public DbSet<CartItem> CartItems { get; set; }
  public DbSet<Order> Orders { get; set; }
  public DbSet<OrderItem> OrderItems { get; set; }

  public void AddCustomer(Customer customer)
  {
    Customers.Add(customer);
    SaveChanges();
  }
  public void EditCustomer(Customer customer)
  {
    var customerToUpdate = Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
    customerToUpdate.Address = customer.Address;
    customerToUpdate.City = customer.City;
    customerToUpdate.Region = customer.Region;
    customerToUpdate.PostalCode = customer.PostalCode;
    customerToUpdate.Country = customer.Country;
    customerToUpdate.Phone = customer.Phone;
    customerToUpdate.Fax = customer.Fax;
    SaveChanges();
  }
  public CartItem AddToCart(CartItemJSON cartItemJSON)
  {
    int CustomerId = Customers.FirstOrDefault(c => c.Email == cartItemJSON.email).CustomerId;
    int ProductId = cartItemJSON.id;
    // check for duplicate cart item
    CartItem cartItem = CartItems.FirstOrDefault(ci => ci.ProductId == ProductId && ci.CustomerId == CustomerId);
    if (cartItem == null)
    {
      // this is a new cart item
      cartItem = new CartItem()
      {
        CustomerId = CustomerId,
        ProductId = cartItemJSON.id,
        Quantity = cartItemJSON.qty
      };
      CartItems.Add(cartItem);
    }
    else
    {
      // for duplicate cart item, simply update the quantity
      cartItem.Quantity += cartItemJSON.qty;
    }
    SaveChanges();
    cartItem.Product = Products.Find(cartItem.ProductId);
    return cartItem;
  }

  public IEnumerable<CartItem> GetCartItems(string email)
  {
    int customerId = Customers.FirstOrDefault(c => c.Email == email)?.CustomerId ?? 0;
    return CartItems.Where(ci => ci.CustomerId == customerId)
                   .Include(ci => ci.Product)
                   .ThenInclude(p => p.Category)
                   .ToList();
  }

  public void UpdateCartItemQuantity(int cartItemId, int quantity)
  {
    var cartItem = CartItems.Find(cartItemId);
    if (cartItem != null)
    {
      cartItem.Quantity = quantity;
      SaveChanges();
    }
  }

  public void RemoveFromCart(int cartItemId)
  {
    var cartItem = CartItems.Find(cartItemId);
    if (cartItem != null)
    {
      CartItems.Remove(cartItem);
      SaveChanges();
    }
  }

  public Order CreateOrderFromCart(string email)
  {
    var customer = Customers.FirstOrDefault(c => c.Email == email);
    if (customer == null) return null;

    var cartItems = GetCartItems(email).ToList();
    if (!cartItems.Any()) return null;

    var order = new Order
    {
      CustomerId = customer.CustomerId,
      OrderDate = DateTime.Now,
      Status = "Completed"
    };

    Orders.Add(order);
    SaveChanges(); // Save to get OrderId

    decimal totalAmount = 0;
    decimal totalDiscount = 0;

    foreach (var cartItem in cartItems)
    {
      var discount = GetActiveProductDiscount(cartItem.ProductId);
      var unitPrice = cartItem.Product.UnitPrice;
      var discountAmount = discount != null ? unitPrice * cartItem.Quantity * discount.DiscountPercent : 0;

      var orderItem = new OrderItem
      {
        OrderId = order.OrderId,
        ProductId = cartItem.ProductId,
        Quantity = cartItem.Quantity,
        UnitPrice = unitPrice,
        DiscountAmount = discountAmount
      };

      OrderItems.Add(orderItem);
      totalAmount += unitPrice * cartItem.Quantity;
      totalDiscount += discountAmount;
    }

    order.TotalAmount = totalAmount - totalDiscount;
    order.DiscountAmount = totalDiscount;

    // Clear cart items
    foreach (var cartItem in cartItems)
    {
      CartItems.Remove(cartItem);
    }

    SaveChanges();
    return order;
  }

  public Discount GetActiveProductDiscount(int productId)
  {
    return Discounts.FirstOrDefault(d => d.ProductId == productId && 
                                        d.StartTime <= DateTime.Now && 
                                        d.EndTime > DateTime.Now);
  }
}