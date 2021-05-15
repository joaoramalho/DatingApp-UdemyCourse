using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper _smapper;
        private readonly IHubContext<PresenceHub> _spresenceHub;
        private readonly PresenceTracker _stracker;
        private readonly IUnitOfWork _sunitOfWork;
        public MessageHub(IMapper mapper, IUnitOfWork unitOfWork, IHubContext<PresenceHub> presenceHub, PresenceTracker tracker)
        {
            _sunitOfWork = unitOfWork;
            _stracker = tracker;
            _spresenceHub = presenceHub;
            _smapper = mapper;

        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _sunitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            if(_sunitOfWork.HasChanges()) await _sunitOfWork.Complete();

            await Clients.Caller.SendAsync("RecieveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDTO.RecipientUsername.ToLower())
                throw new HubException("You cannot send messages to yourself");

            var sender = await _sunitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _sunitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipentUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _sunitOfWork.MessageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.Now;
            }
            else
            {
                var connections = await _stracker.GetConnectionsForUser(recipient.UserName);

                if (connections != null)
                {
                    await _spresenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _sunitOfWork.MessageRepository.AddMessage(message);

            if (await _sunitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _smapper.Map<MessageDTO>(message));
            }
        }

        public async Task UserIsTyping()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Clients.Group(groupName).SendAsync("UserIsTyping");
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _sunitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if (group == null)
            {
                group = new Group(groupName);
                _sunitOfWork.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (await _sunitOfWork.Complete()) return group;

            throw new HubException("Failed to join group");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _sunitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);

            _sunitOfWork.MessageRepository.RemoveConnection(connection);

            if (await _sunitOfWork.Complete()) return group;

            throw new HubException("Failed to remove from group");
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}