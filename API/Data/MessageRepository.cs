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
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _scontext;
        private readonly IMapper _smapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _smapper = mapper;
            _scontext = context;
        }

        public void AddMessage(Message message)
        {
            _scontext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _scontext.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _scontext.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _scontext.Messages
                    .OrderByDescending(m => m.MessageSent)
                    .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username && u.SenderDeleted == false),
                _ => query.Where(u => u.Recipient.UserName == messageParams.Username && u.RecipientDeleted == false && u.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDTO>(_smapper.ConfigurationProvider);

            return await PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await _scontext.Messages
                            .Include(u => u.Sender).ThenInclude(p => p.Photos)
                            .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                            .Where(m => m.Recipient.UserName == currentUsername 
                                    && m.RecipientDeleted == false
                                    && m.Sender.UserName == recipientUsername
                                    || m.Recipient.UserName == recipientUsername
                                    && m.Sender.UserName == currentUsername 
                                    && m.SenderDeleted == false
                            ).OrderByDescending(m => m.MessageSent)
                            .ToListAsync();

            var unreadMessages = messages.Where(
                m => m.DateRead == null && m.Recipient.UserName == currentUsername
                ).ToList();

            if(unreadMessages.Any())
            {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.Now;
                }

                await _scontext.SaveChangesAsync();
            }

            return _smapper.Map<IEnumerable<MessageDTO>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _scontext.SaveChangesAsync() > 0;
        }
    }
}