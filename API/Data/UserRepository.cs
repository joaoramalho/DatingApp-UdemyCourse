using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
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

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _scontext.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _scontext.SaveChangesAsync() > 0;
        }

        public void Update(AppUser user)
        {
            _scontext.Entry(user).State = EntityState.Modified;
        }

        public async Task<IEnumerable<MemberDTO>> GetMembersAsync()
        {
            return await _scontext.Users.ProjectTo<MemberDTO>(_smapper.ConfigurationProvider).ToListAsync();
        }

        public async Task<MemberDTO> GetMemberAsync(string username)
        {
            return await _scontext.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDTO>(_smapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }
    }
}