using Microsoft.AspNetCore.Mvc;
namespace CasaDoCodigo.Controllers;
public class HomeController : Controller
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error() { Response.StatusCode = 500; return View(); }
}
