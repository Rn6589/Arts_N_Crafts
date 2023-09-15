using Arts_N_Crafts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arts_N_Crafts.DataAccess.Repository.IRepository
{
    public interface IOrderDetailRepository:IRepository<OrderDetail>
    {
        void Update(OrderDetail obj);
    }
}
