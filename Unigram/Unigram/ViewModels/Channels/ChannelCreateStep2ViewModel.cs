﻿using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Services;
using Unigram.ViewModels.Supergroups;

namespace Unigram.ViewModels.Channels
{
    public class ChannelCreateStep2ViewModel : SupergroupEditViewModelBase
    {
        public ChannelCreateStep2ViewModel(IClientService clientService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(clientService, settingsService, aggregator)
        {
        }

        protected override async void SendExecute()
        {
            var chat = _chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypeSupergroup supergroup)
            {
                var item = ClientService.GetSupergroup(supergroup.SupergroupId);
                var cache = ClientService.GetSupergroupFull(supergroup.SupergroupId);

                if (item == null || cache == null)
                {
                    return;
                }

                var username = _isPublic ? Username?.Trim() ?? string.Empty : string.Empty;

                if (!string.Equals(username, item.EditableUsername()))
                {
                    var response = await ClientService.SendAsync(new SetSupergroupUsername(item.Id, username));
                    if (response is Error error)
                    {
                        if (error.TypeEquals(ErrorType.CHANNELS_ADMIN_PUBLIC_TOO_MUCH))
                        {
                            HasTooMuchUsernames = true;
                            NavigationService.ShowLimitReached(new PremiumLimitTypeCreatedPublicChatCount());
                        }
                        // TODO:

                        return;
                    }
                }

                NavigationService.NavigateToChat(chat);
                NavigationService.GoBackAt(0, false);
                //NavigationService.Navigate(typeof(ChannelCreateStep3Page), chat.Id);
            }
        }
    }
}
