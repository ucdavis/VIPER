using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Computing.Model;
using Viper.Areas.Computing.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Web.Authorization;

namespace Viper.Areas.Computing.Controllers
{
    [Route("/api/fakeusers/")]
    [Permission(Allow = "SVMSecure.CATS.FakeUser")]
    public class FakeUserController : ApiController
    {
        private readonly AAUDContext _aaudcontext;
        private readonly IMapper _mapper;
        private readonly FakeUserService _fakeUserService;
        public FakeUserController(AAUDContext aaudcontext, IMapper mapper)
        {
            _aaudcontext = aaudcontext;
            _mapper = mapper;
            _fakeUserService = new FakeUserService(_aaudcontext);
        }

        [HttpGet]
        public async Task<ActionResult<List<FakeUser>>> GetFakeUsers()
        {
            var fakePeople = await _aaudcontext.FakePeople
                .Include(u => u.FakeId)
                .ToListAsync();
            var fakeUsers = _mapper.Map<List<FakeUser>>(fakePeople);
            fakeUsers.ForEach(u => u.IsProtected = _fakeUserService.IsProtectedFakeUser(u.PKey));
            return fakeUsers;
        }

        [HttpPost]
        public async Task<ActionResult<FakeUser>> CreateFakeUser(FakeUser fakeUser)
        {
            fakeUser.PKey = _fakeUserService.GetNewPKey(fakeUser.ClientId);
            var isValid = await _fakeUserService.IsValidFakeUser(fakeUser, true);
            if (fakeUser == null || !isValid)
            {
                return BadRequest("Invalid fake user data.");
            }

            var newFakePerson = _mapper.Map<FakePerson>(fakeUser);
            var newFakeIds = _mapper.Map<FakeId>(fakeUser);
            _aaudcontext.FakePeople.Add(newFakePerson);
            _aaudcontext.FakeIds.Add(newFakeIds);
            await _aaudcontext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFakeUsers), new { id = fakeUser.PKey }, fakeUser);
        }

        [HttpPut("{pKey}")]
        public async Task<ActionResult<FakeUser>> UpdateFakeUser(string pKey, FakeUser fakeUser)
        {
            //check pKey is unchanged and user is valid
            var check = _fakeUserService.GetNewPKey(fakeUser.ClientId);
            var isValid = pKey == fakeUser.PKey.Trim() && check == fakeUser.PKey && await _fakeUserService.IsValidFakeUser(fakeUser, false);
            if (fakeUser == null || !isValid)
            {
                return BadRequest("Invalid fake user data.");
            }

            var fakePerson = await _aaudcontext.FakePeople.FindAsync(fakeUser.PKey);
            var fakeIds = await _aaudcontext.FakeIds.FindAsync(fakeUser.PKey);

            if(fakePerson == null || fakeIds == null)
            {
                return NotFound();
            }
            _mapper.Map(fakeUser, fakePerson);
            _mapper.Map(fakeUser, fakeIds);

            await _aaudcontext.SaveChangesAsync();
            return Ok(fakeUser);
        }
    }
}