using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository _suserRepository;
        private readonly ILikesRepository _slikesRepository;

        public LikesController(IUserRepository userRepository, ILikesRepository likesRepository)
        {
            _slikesRepository = likesRepository;
            _suserRepository = userRepository;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await _suserRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _slikesRepository.GetUserWithLikes(sourceUserId);

            if(likedUser == null) return NotFound();

            if(sourceUser.UserName == username) return BadRequest("You cannot like yourself");

            var userLike = await _slikesRepository.GetUserLike(sourceUserId, likedUser.Id);

            if(userLike != null) return BadRequest("You already like this user");

            userLike = new Entities.UserLike 
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);

            if(await _suserRepository.SaveAllAsync()) return Ok();

            return BadRequest("Failed to like user");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDTO>>> GetUserLikes([FromQuery] LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            var users = await _slikesRepository.GetUserLikes(likesParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }
    }
}