using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _scontext;
        private readonly IMapper _smapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            _smapper = mapper;
            _scontext = context;

        }
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _scontext.Users.Include(p => p.Photos).ToListAsync();
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _scontext.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByPhotoId(int id)
        {
            return await _scontext.Photos
                        .Where(x => x.Id == id)
                        .Select(t => t.AppUser)
                        .FirstOrDefaultAsync();
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _scontext.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public void Update(AppUser user)
        {
            _scontext.Entry(user).State = EntityState.Modified;
        }

        public async Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams)
        {
            var query = _scontext.Users.AsQueryable();

            query = query.Where(u => u.UserName != userParams.CurrentUsername);
            query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            query = query.Include(p => p.Photos).Where(x => x.Photos.Any(p => p.IsApproved));

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };

            return await PagedList<MemberDTO>.CreateAsync(
                query.ProjectTo<MemberDTO>(_smapper.ConfigurationProvider).AsNoTracking(),
                userParams.PageNumber,
                userParams.PageSize);
        }

        public async Task<MemberDTO> GetMemberAsync(string username, bool? isCurrentUser)
        {
            var query = _scontext.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDTO>(_smapper.ConfigurationProvider)
                .AsQueryable();

                if((bool)isCurrentUser){
                    query = query.IgnoreQueryFilters();
                }

                return await query.FirstOrDefaultAsync();
        }

        public async Task<string> GetUserGender(string username)
        {
            return await _scontext.Users
                    .Where(x => x.UserName == username)
                    .Select(x => x.Gender)
                    .FirstOrDefaultAsync();
        }
    }
}