using Microsoft.AspNetCore.Mvc;

namespace FrontendMvc.Areas.Admin.Controllers;

public class AdminBaseController : Controller
{
    protected void SetFlashMessage(string message, string type = "success")
    {
        TempData["AdminMessage"] = message;
        TempData["AdminMessageType"] = type;
    }
}
