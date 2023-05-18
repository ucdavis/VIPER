﻿using Microsoft.AspNetCore.Mvc;

namespace Viper.Classes
{
    /// <summary>
    /// Common base class for API style controllers
    /// </summary>
    [ApiController]
    [ApiResponse]
    [ApiExceptionFilter]
    public class ApiController : ControllerBase
    {

    }
}
