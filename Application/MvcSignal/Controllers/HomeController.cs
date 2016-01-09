using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcSignal.Controllers
{
    public class HomeController : Controller
    {
           
        public ActionResult Index()
        {
            ViewBag.Message = "Your index page.";

            return View();
        }

        public ActionResult PlayGame()
        {
            ViewBag.Message = "Your index page.";

            return View();
        }
         public ActionResult BigScreen()
        {
            ViewBag.Message = "Your index page.";

            return View();
        }
        
    
    }
}
