using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _suserManager;
        private readonly IUnitOfWork _sunitOfWork;
        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _sunitOfWork = unitOfWork;
            _suserManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _suserManager.Users
                        .Include(r => r.UserRoles)
                        .ThenInclude(r => r.Role)
                        .OrderBy(u => u.UserName)
                        .Select(u => new
                        {
                            u.Id,
                            Username = u.UserName,
                            Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                        })
                        .ToListAsync();

            return Ok(users);
        }

        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(",").ToArray();

            var user = await _suserManager.FindByNameAsync(username);

            if (user == null) return NotFound("Could not find user");

            var userRoles = await _suserManager.GetRolesAsync(user);

            var result = await _suserManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            result = await _suserManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded) return BadRequest("Failed to remove from roles");

            return Ok(await _suserManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<IEnumerable<PhotoForApprovalDTO>> GetPhotosForApproval()
        {
            return await _sunitOfWork.PhotoRepository.GetUnapprovedPhotos();
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve-photo/{photoId}")]
        public async Task<ActionResult> ApprovePhoto(int photoId)
        {
            var photo = await _sunitOfWork.PhotoRepository.GetPhotoById(photoId);
            photo.IsApproved = true;
            
            var user = await _sunitOfWork.UserRepository.GetUserByPhotoId(photo.Id);
            
            if(!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

            if(await _sunitOfWork.Complete()){
                return Ok();
            }

            return BadRequest("Error while saving photo details");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("reject-photo/{photoId}")]
        public async Task<ActionResult> RejectPhoto(int photoId)
        {
            var photo = await _sunitOfWork.PhotoRepository.GetPhotoById(photoId);

            if(photo == null)
            {
                return BadRequest("The photo does not exist");
            }

            if(!photo.IsApproved)
            {
                return BadRequest("The photo is already not approved");
            }

            photo.IsApproved = false;


            if(await _sunitOfWork.Complete()){
                return Ok();
            }

            return BadRequest("Error while saving photo details");
        }
    }
}