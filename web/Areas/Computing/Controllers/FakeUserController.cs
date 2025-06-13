using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Computing.Model;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.Computing.Controllers
{
    [Route("/api/fakeusers/")]
    [Permission(Allow = "SVMSecure")]
    public class FakeUserController : ApiController
    {
        private readonly AAUDContext _aaudcontext;
        private readonly IMapper _mapper;
        public FakeUserController(AAUDContext aaudcontext, IMapper mapper)
        {
            _aaudcontext = aaudcontext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<FakeUser>>> GetFakeUsers()
        {
            var fakePeople = await _aaudcontext.FakePeople
                .Include(u => u.FakeId)
                .ToListAsync();
            return _mapper.Map<List<FakeUser>>(fakePeople);
        }   
    }
}