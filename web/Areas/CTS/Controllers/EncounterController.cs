using Microsoft.AspNetCore.Mvc;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/encounters")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class EncounterController : ApiController
    {
        public EncounterController(VIPERContext context, RAPSContext rapsContext)
        {
        }

    }
}
