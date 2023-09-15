using Arts_N_Crafts.DataAccess.Repository.IRepository;
using Arts_N_Crafts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Arts_N_Crafts.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db):base(db)
        { 
                _db = db;
             
        } 


        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

		public void UpdateStatus(int id, string OrderStatus, string? paymentStatus=null)
		{
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
            if(orderFromDb != null)
            {
                orderFromDb.OrderStatus = OrderStatus;
                if(paymentStatus!=null)
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
		}
		public void UpdateStripePaymentID(int id,string sessionId, string paymentIntentId)
		{
			var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
			orderFromDb.PaymentIntentId = paymentIntentId;
            orderFromDb.SessionId = sessionId;

		}
	}
}
