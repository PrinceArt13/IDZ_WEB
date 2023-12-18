using IDZ_WEB.Models;
using IDZ_WEB.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace IDZ_WEB.Controllers
{
    public class IDZController : Controller
    {
        public ActionResult ListOfCategories()
        {
            List<categories> categories = new List<categories>();
            using (var db = new SupermarketEntities())
            {
                categories = db.categories.OrderByDescending(x => x.name).ToList();
            }
            return View(categories);
        }
        public ActionResult ListOfProducts(Guid categoryID)
        {
            List<products> model = new List<products>();
            using (var db = new SupermarketEntities())
            {
                foreach(var product in db.products.Where(x=>x.category_id == categoryID))
                {
                    model.Add(product);
                }
            }

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult Prices(Guid product_id)
        {
            string UserLogin = "";
            Guid UserID;
            if (User.Identity.IsAuthenticated == true)
            {
                UserLogin = User.Identity.Name;
            }
            double discount = 1;
            double price;
            string prices = "";
            string discountInfo = "";
            using (var context = new SupermarketEntities())
            {
                if (User.Identity.IsAuthenticated == true && User.IsInRole("Participant"))
                {
                    UserID = context.User.Where(x => x.Login == UserLogin).FirstOrDefault().ID;
                    discount = context.discount_cards.Where(x => x.user_id == UserID).FirstOrDefault().discount;
                    discountInfo = $" - скидка {(1 - discount) * 100}%";
                }

                price = context.price_change
                    .Where(x => x.date_price_change <= DateTime.Now && x.product_id == product_id)
                    .OrderByDescending(x => x.date_price_change)
                    .FirstOrDefault().price;
                prices = $"{price} рублей " + discountInfo;
            }
            return PartialView("Prices", prices);
        }

        [HttpPost]
        public void AddToCart(Guid guid)
        {
            string currentUserLogin = HttpContext.User.Identity.Name;

            User currentUser = new User();
            products pr = new products();
            purchase_item pui = new purchase_item();
            cashiers c = new cashiers();
            using(var db = new SupermarketEntities())
            {
                currentUser = db.User.Where(x => x.Login == currentUserLogin).FirstOrDefault();
                pr = db.products.Find(guid);

                //ID карты у текущего пользователя
                Guid thisPurchase = db.discount_cards.Where(x => x.user_id == currentUser.ID).FirstOrDefault().card_id;
                
                purchase pu = db.purchase.Where(x => x.date == null && thisPurchase == x.discount_card_id).FirstOrDefault();
                if (pu != null)
                {
                    //добавление товара в покупку
                    pui.count = 1;
                    pui.product_id = pr.product_id;
                    pui.purchase_id = pu.purchase_id;
                    
                    db.purchase_item.Add(pui);
                }
                else
                {
                    //создание покупки без даты
                    pu = new purchase();
                    pu.cashier_id = db.cashiers.Where(x => x.first_name == "Сергей").FirstOrDefault().cashier_id;
                    pu.cash_register_number = 1;
                    pu.date = null;
                    pu.purchase_id = Guid.NewGuid();
                    pu.discount_card_id = db.discount_cards.Where(x => x.user_id == currentUser.ID).FirstOrDefault().card_id;
                    
                    //Добавление товара в покупку
                    pui.count = 1;
                    pui.product_id = pr.product_id;
                    pui.purchase_id = pu.purchase_id;

                    db.purchase_item.Add(pui);

                    db.purchase.Add(pu);
                }
                db.SaveChanges();
            }
        }

        public ActionResult Cart()
        {
            string currentUserLogin = HttpContext.User.Identity.Name;
            List<products> prList = new List<products>();
            User currentUser = new User();
            Cart model = new Cart(prList, Guid.NewGuid());

            using (var db = new SupermarketEntities())
            {
                currentUser = db.User.Where(x => x.Login == currentUserLogin).FirstOrDefault();
                Guid thisPurchase = db.discount_cards.Where(x => x.user_id == currentUser.ID).FirstOrDefault().card_id;

                purchase pu = db.purchase.Where(x => x.date == null && thisPurchase == x.discount_card_id).FirstOrDefault();
                if (pu == null)
                {
                    return View(model);
                }
                List<purchase_item> pui = db.purchase_item
                            .Where(x => x.purchase_id == pu.purchase_id).ToList();

                foreach (var purItem in pui)
                {
                    products product = db.products.Find(purItem.product_id);
                    prList.Add(product);
                }

                model = new Cart(prList, pu.purchase_id);
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult BuyProducts(Guid guid)
        {
            string UserLogin = "";
            Guid UserID;
            if (User.Identity.IsAuthenticated == true)
            {
                UserLogin = User.Identity.Name;
            }
            double discount = 1;
            double sum = 0;
            using (var db = new SupermarketEntities())
            {
                if (User.Identity.IsAuthenticated == true)
                {
                    UserID = db.User.Where(x => x.Login == UserLogin).FirstOrDefault().ID;
                    discount = db.discount_cards.Where(x => x.user_id == UserID).FirstOrDefault().discount;
                }

                List<purchase_item> purItem = db.purchase_item.Where(x => x.purchase_id == guid).ToList();
                foreach (var pui in purItem)
                {
                    sum += db.price_change.Where(x => x.date_price_change <= DateTime.Now && x.product_id == pui.product_id)
                        .OrderByDescending(x => x.date_price_change).FirstOrDefault().price * discount;
                }

                purchase pu = db.purchase.Find(guid);
                pu.date = DateTime.Now;

                db.SaveChanges();
            }
            return View(sum);
        }

        List<categories> GetCategoriesList()
        {
            List<categories> categoriesList = new List<categories>();
            using (var db = new SupermarketEntities())
            {
                categoriesList = db.categories.OrderByDescending(x => x.name).ToList();
            }
            return categoriesList;
        }
        List<manufacturers> GetManufacturersList()
        {
            List<manufacturers> manufacturersList = new List<manufacturers>();
            using (var db = new SupermarketEntities())
            {
                manufacturersList = db.manufacturers.OrderByDescending(x => x.name).ToList();
            }
            return manufacturersList;
        }

        #region Создание товаров, категорий, производителей
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateProduct()
        {
            ViewBag.manufacturers = new SelectList(GetManufacturersList(), "manufacturer_id", "name");
            ViewBag.categories = new SelectList(GetCategoriesList(), "category_id", "name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateProduct(ProductVM newproduct)
        {
            if (ModelState.IsValid)
            {
                using (var context = new SupermarketEntities())
                {
                    products product = new products()
                    {
                        product_id = Guid.NewGuid(),
                        name = newproduct.name,
                        manufacturer_id = newproduct.manufacturer_id,
                        category_id = newproduct.category_id
                    };
                    context.products.Add(product);
                    price_change price = new price_change()
                    {
                        price_id = Guid.NewGuid(),
                        products = product,
                        price = newproduct.price,
                        date_price_change = DateTime.Now
                    };
                    context.price_change.Add(price);
                    context.SaveChanges();
                }
                return RedirectToAction("ListOfCategories");
            }
            ViewBag.manufacturers = new SelectList(GetManufacturersList(), "manufacturer_id", "name");
            ViewBag.categories = new SelectList(GetCategoriesList(), "category_id", "name");
            return View(newproduct);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateCategory()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateCategory(CategoryVM newcategory)
        {
            if (ModelState.IsValid)
            {
                using (var context = new SupermarketEntities())
                {
                    categories category = new categories()
                    {
                        category_id = Guid.NewGuid(),
                        name = newcategory.name
                    };
                    context.categories.Add(category);
                    context.SaveChanges();
                }
            }
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateManufacturer()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateManufacturer(ManufacturerVM newmanufacturer)
        {
            if (ModelState.IsValid)
            {
                using (var context = new SupermarketEntities())
                {
                    manufacturers manufacturer = new manufacturers()
                    {
                        manufacturer_id = Guid.NewGuid(),
                        name = newmanufacturer.name
                    };
                    context.manufacturers.Add(manufacturer);
                    context.SaveChanges();
                }
            }
            return View();
        }
        #endregion

        #region Изменение/удаление товаров
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult EditProduct(Guid productID)
        {
            ProductVM model;
            using (var context = new SupermarketEntities())
            {
                products product = context.products.Find(productID);
                int price = context.price_change.Where(x => x.date_price_change <= DateTime.Now && x.product_id == productID)
                                    .OrderByDescending(x => x.date_price_change).FirstOrDefault().price;
                model = new ProductVM
                {
                    product_id = product.product_id,
                    manufacturer_id = product.manufacturer_id,
                    name = product.name,
                    category_id = product.category_id,
                    price = price
                };
            }
            ViewBag.manufacturers = new SelectList(GetManufacturersList(), "manufacturer_id", "name");
            ViewBag.categories = new SelectList(GetCategoriesList(), "category_id", "name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult EditProduct(ProductVM model)
        {
            if (ModelState.IsValid)
            {
                using (var context = new SupermarketEntities())
                {
                    products editedProduct = new products
                    {
                        category_id = model.category_id,
                        product_id = model.product_id,
                        manufacturer_id = model.manufacturer_id,
                        name = model.name
                    };

                    price_change newprice = new price_change
                    {
                        price_id = Guid.NewGuid(),
                        product_id = model.product_id,
                        price = model.price,
                        date_price_change = DateTime.Now
                    };
                    context.products.Attach(editedProduct);
                    context.Entry(editedProduct).State = System.Data.Entity.EntityState.Modified;
                    context.price_change.Add(newprice);

                    context.SaveChanges();
                }
                return RedirectToAction("ListOfCategories");
            }
            ViewBag.manufacturers = new SelectList(GetManufacturersList(), "manufacturer_id", "name");
            ViewBag.categories = new SelectList(GetCategoriesList(), "category_id", "name");
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteProduct(Guid productID)
        {
            products productToDelete;
            using (var context = new SupermarketEntities())
            {
                productToDelete = context.products.Find(productID);
            }
            ViewBag.manufacturers = GetManufacturersList()
                                    .First(x => x.manufacturer_id == productToDelete.manufacturer_id).name;
            ViewBag.categories = GetCategoriesList()
                                 .First(x => x.category_id == productToDelete.category_id).name;
            return View(productToDelete);
        }

        [HttpPost, ActionName("DeleteProduct")]
        public ActionResult DeleteConfirmed(Guid productID)
        {
            using (var context = new SupermarketEntities())
            {
                products productToDelete = new products
                {
                    product_id = productID
                };
                foreach(var price in context.price_change.Where(x => x.product_id == productToDelete.product_id))//??
                {
                    context.Entry(price).State = System.Data.Entity.EntityState.Deleted;
                }
                context.Entry(productToDelete).State = System.Data.Entity.EntityState.Deleted;
                context.SaveChanges();
            }
            return RedirectToAction("ListOfCategories");
        }
        #endregion

        #region Авторизация
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(UserVM webUser)
        {
            if (ModelState.IsValid)
                using (SupermarketEntities context = new SupermarketEntities())
                {
                    User user = null;
                    user = context.User.Where(u => u.Login == webUser.Login).FirstOrDefault();
                    if (user != null)
                    {
                        string passwordHash = ReturnHashCode(webUser.Password + user.Salt.ToString().ToUpper());
                        if (passwordHash == user.PasswordHash)
                        {
                            string userRole = "";
                            switch (user.UserRole)
                            {
                                case 1:
                                    userRole = "Admin";
                                    break;
                                case 2:
                                    userRole = "Participant";
                                    break;
                            }

                            FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                                                        1,
                                                        user.Login, //name
                                                        DateTime.Now,
                                                        DateTime.Now.AddDays(1),
                                                        false,
                                                        userRole //role
                                                        );
                            string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                            HttpContext.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket));
                            return RedirectToAction("ListOfCategories", "IDZ");
                        }
                    }
                }
            ViewBag.Error = "НЕПРАЫВЛИЬОН!!!!!!!!1111111";
            return View(webUser);
        }

        string ReturnHashCode(string loginAndSalt)
        {
            string hash = "";
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(loginAndSalt));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                hash = sBuilder.ToString().ToUpper();
            }
            return hash;
        }
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("ListOfCategories", "IDZ");
        }
        #endregion
    }
}