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
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db):base(db)
        { 
                _db = db;
             
        } 


        public void Update(Product obj)
        {
            var objFromDb = _db.Products.FirstOrDefault(u =>u.Id==obj.Id);
            if(objFromDb != null)
            {
                objFromDb.Title = obj.Title;
                objFromDb.Description = obj.Description;
                objFromDb.Price = obj.Price;
                objFromDb.Creator = obj.Creator;
                objFromDb.CategoryId = obj.CategoryId;
                if(obj.ImageUrl!= null)
                {
                    objFromDb.ImageUrl = obj.ImageUrl;
                }
            }
        } 
    }
}
