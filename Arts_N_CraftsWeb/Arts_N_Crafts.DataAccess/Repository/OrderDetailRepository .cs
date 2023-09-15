﻿using Arts_N_Crafts.DataAccess.Repository.IRepository;
using Arts_N_Crafts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Arts_N_Crafts.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
	{
        private ApplicationDbContext _db;
        public OrderDetailRepository(ApplicationDbContext db):base(db)
        { 
                _db = db;
             
        } 


        public void Update(OrderDetail obj)
        {
            _db.OrderDetail.Update(obj);
        } 
    }
}