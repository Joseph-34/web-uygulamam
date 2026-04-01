using Microsoft.AspNetCore.Mvc;

namespace kazandakazan.Controllers;

public class LegalController : Controller
{
    public IActionResult Privacy() => View();

    public IActionResult Security() => View();

    public IActionResult Terms() => View();
}
