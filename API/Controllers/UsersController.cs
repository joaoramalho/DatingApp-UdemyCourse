using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _suserRepository;
        private readonly IMapper _smapper;
        private readonly IPhotoService _sphotoService;
        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _sphotoService = photoService;
            _smapper = mapper;
            _suserRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            var user = await _suserRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUsername = user.UserName;

            if(string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = user.Gender == "male" ? "female" : "male";

            var users = await _suserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(
                users.CurrentPage,
                users.PageSize,
                users.TotalCount,
                users.TotalPages);

            return Ok(users);
        }

        // api/users/3
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            return await _suserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            var user = await _suserRepository.GetUserByUsernameAsync(User.GetUsername());

            _smapper.Map(memberUpdateDTO, user);

            _suserRepository.Update(user);

            if (await _suserRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            var user = await _suserRepository.GetUserByUsernameAsync(User.GetUsername());

            var result = await _sphotoService.AddPhotoAsync(file);

            if(result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if(user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if(await _suserRepository.SaveAllAsync())
            {
                return CreatedAtRoute("GetUser", new {username = user.UserName}, _smapper.Map<PhotoDTO>(photo));
            }

            return BadRequest("Problem solving request.");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _suserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);

            if(currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;
            if(await _suserRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _suserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo == null) return NotFound();

            if(photo.IsMain) return BadRequest("You cannot delete your main photo");

            if(photo.PublicId != null)
            {
                var result = await _sphotoService.DeletePhotoAsync(photo.PublicId);
                if(result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if(await _suserRepository.SaveAllAsync()) return Ok();

            return BadRequest("Failed to delete photo");
        }
    }
}