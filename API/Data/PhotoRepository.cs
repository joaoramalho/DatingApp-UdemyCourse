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
    public class PhotoRepository : IPhotoRepository
    {
        private readonly DataContext _scontext;
        private readonly IMapper _smapper;
        public PhotoRepository(DataContext context, IMapper mapper)
        {
            _smapper = mapper;
            _scontext = context;
        }

        public async Task<Photo> GetPhotoById(int id)
        {
            return await _scontext.Photos.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<PhotoForApprovalDTO>> GetUnapprovedPhotos()
        {
            return await _scontext.Photos
                    .IgnoreQueryFilters()
                    .Where(x => !x.IsApproved)
                    .ProjectTo<PhotoForApprovalDTO>(_smapper.ConfigurationProvider).ToListAsync();
        }

        public void RemovePhoto(Photo photo)
        {
            _scontext.Photos.Remove(photo);
        }
    }
}