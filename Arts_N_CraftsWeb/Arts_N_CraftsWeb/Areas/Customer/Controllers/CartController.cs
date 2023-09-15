using Arts_N_Crafts.DataAccess.Repository;
using Arts_N_Crafts.DataAccess.Repository.IRepository;
using Arts_N_Crafts.Models;
using Arts_N_Crafts.Models.ViewModels;
using Arts_N_Crafts.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace Arts_N_CraftsWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitofWork;
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }
        public int OrderTotal { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitofWork = unitOfWork;
        }
        public IActionResult Index()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
				OrderHeader=new()
            };
            foreach (var cart in ShoppingCartVM.ListCart)
            {
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Product.Price * cart.Count);  
            }
            return View(ShoppingCartVM);
        }

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			ShoppingCartVM = new ShoppingCartVM()
			{
				ListCart = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
				OrderHeader=new()
			};

			ShoppingCartVM.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser.GetFirstOrDefault(
				u => u.Id == claim.Value);

			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


			foreach (var cart in ShoppingCartVM.ListCart)
			{
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Product.Price * cart.Count);
			}
			return View(ShoppingCartVM);
			return View();
		}
		[HttpPost]
		[ActionName("Summary")]
		[ValidateAntiForgeryToken]

		public IActionResult SummaryPOST( )
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			ShoppingCartVM.ListCart = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
				includeProperties: "Product");
			
			ShoppingCartVM.OrderHeader.OrderDate= System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

			foreach (var cart in ShoppingCartVM.ListCart)
			{
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Product.Price * cart.Count);
			}

            ApplicationUser applicationUser = _unitofWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);
            if (applicationUser.CompanyId.GetValueOrDefault() ==0)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
			else
			{
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitofWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitofWork.Save();

			foreach (var cart in ShoppingCartVM.ListCart)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderId = ShoppingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count,
				};
				_unitofWork.OrderDetail.Add(orderDetail);
				_unitofWork.Save();
			}
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{

				//stripe settings


				var domain = "https://localhost:44336/";
				var options = new SessionCreateOptions
				{
					PaymentMethodTypes = new List<string>
				{
				  "card",
				},
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + $"customer/cart/index",
				};

				foreach (var item in ShoppingCartVM.ListCart)
				{

					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Product.Price * 100),//20.00 -> 2000
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							},

						},
						Quantity = item.Count,
					};
					options.LineItems.Add(sessionLineItem);
				}
				var service = new SessionService();
				Session session = service.Create(options);

				_unitofWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitofWork.Save();

				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}


			else
			{
				return RedirectToAction("OrderConfirmation", "Cart", new
				{
					id = ShoppingCartVM.OrderHeader.Id
				});
			}
		}


			public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitofWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
				//check the stripe status
				if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitofWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitofWork.Save();
                }
            }

			List<ShoppingCart> shoppingCarts = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
			_unitofWork.ShoppingCart.RemoveRange(shoppingCarts);
			_unitofWork.Save();
			return View(id);

		}



		public IActionResult Plus(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _unitofWork.ShoppingCart.IncrementCount(cart, 1);
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));
        }
		public IActionResult Remove(int cartId)
		{
			var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
			_unitofWork.ShoppingCart.Remove(cart);
			_unitofWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Minus(int cartId)
		{
			var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            if (cart.Count <= 1)
            {
                _unitofWork.ShoppingCart.Remove(cart);
            }
            else
            {
				_unitofWork.ShoppingCart.DecrementCount(cart, 1);

			}
			_unitofWork.Save();
			return RedirectToAction(nameof(Index));
		}
		//private double GetPrice(double quantity, double price)
		//{
		//    return price;
		//}
	}
}

