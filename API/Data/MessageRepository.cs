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

        public void AddGroup(Group group)
        {
            _scontext.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            _scontext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _scontext.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _scontext.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _scontext.Groups
                    .Include(c => c.Connections)
                    .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
                    .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _scontext.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _scontext.Groups
                    .Include(x => x.Connections)
                    .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _scontext.Messages
                    .OrderByDescending(m => m.MessageSent)
                    .ProjectTo<MessageDTO>(_smapper.ConfigurationProvider)
                    .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipentUsername == messageParams.Username && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.SenderUsername == messageParams.Username && u.SenderDeleted == false),
                _ => query.Where(u => u.RecipentUsername == messageParams.Username && u.RecipientDeleted == false && u.DateRead == null)
            };

            return await PagedList<MessageDTO>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await _scontext.Messages
                            .Where(m => m.Recipient.UserName == currentUsername 
                                    && m.RecipientDeleted == false
                                    && m.Sender.UserName == recipientUsername
                                    || m.Recipient.UserName == recipientUsername
                                    && m.Sender.UserName == currentUsername 
                                    && m.SenderDeleted == false
                            ).OrderByDescending(m => m.MessageSent)
                            .ProjectTo<MessageDTO>(_smapper.ConfigurationProvider)
                            .ToListAsync();

            var unreadMessages = messages.Where(
                m => m.DateRead == null && m.RecipentUsername == currentUsername
                ).ToList();

            if(unreadMessages.Any())
            {
                foreach(var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return messages;
        }

        public void RemoveConnection(Connection connection)
        {
            _scontext.Connections.Remove(connection);
        }
    }
}