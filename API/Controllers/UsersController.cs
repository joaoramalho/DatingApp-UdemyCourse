using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _suserRepository;
        private readonly IMapper _smapper;
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _smapper = mapper;
            _suserRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers()
        {
            var users = await _suserRepository.GetMembersAsync();
            return Ok(users);
        }

        // api/users/3
        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            return await _suserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _suserRepository.GetUserByUsernameAsync(username);

            _smapper.Map(memberUpdateDTO, user);

            _suserRepository.Update(user);

            if(await _suserRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }
    }
}