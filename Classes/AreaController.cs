using Microsoft.AspNetCore.Mvc;

namespace Viper.Classes
{
    public class AreaController : Controller
    {
        public AreaController() { }
        public async Task<ActionResult<List<NavMenuItem>>> Nav()
        {
            //TODO: get a default nav?
            return await Task.Run(() => new List<NavMenuItem>());
        }

        //TODO: Handle 403 and 500 errors here? 
    }
}
