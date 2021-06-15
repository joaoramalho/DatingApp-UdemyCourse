using System.Threading.Tasks;
using API.Interfaces;
using AutoMapper;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _scontext;
        private readonly IMapper _smapper;
        public UnitOfWork(DataContext context, IMapper mapper)
        {
            _smapper = mapper;
            _scontext = context;

        }
        public IUserRepository UserRepository => new UserRepository(_scontext, _smapper);

        public IMessageRepository MessageRepository => new MessageRepository(_scontext, _smapper);

        public ILikesRepository LikesRepository => new LikesRepository(_scontext);
        public IPhotoRepository PhotoRepository => new PhotoRepository(_scontext, _smapper);

        public async Task<bool> Complete()
        {
            return await _scontext.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return _scontext.ChangeTracker.HasChanges();
        }
    }
}