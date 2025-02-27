﻿using System.Collections.Generic;
using Telegram.Td.Api;

namespace Unigram.Navigation.Services
{
    public class NavigationState : Dictionary<string, object>
    {
        public static NavigationState GetInvoice(InputInvoice invoice)
        {
            return invoice switch
            {
                InputInvoiceMessage message => GetInvoice(message.ChatId, message.MessageId),
                InputInvoiceName name => GetInvoice(name.Name),
                _ => null
            };
        }

        public static NavigationState GetInvoice(long chatId, long messageId)
        {
            return new NavigationState { { "chatId", chatId }, { "messageId", messageId } };
        }

        public static NavigationState GetInvoice(string name)
        {
            return new NavigationState { { "name", name } };
        }

        public static NavigationState GetChatMember(long chatId, MessageSender memberId)
        {
            if (memberId is MessageSenderUser user)
            {
                return new NavigationState { { "chatId", chatId }, { "senderUserId", user.UserId } };
            }
            else if (memberId is MessageSenderChat chat)
            {
                return new NavigationState { { "chatId", chatId }, { "senderChatId", chat.ChatId } };
            }

            return new NavigationState();
        }

        public static NavigationState GetSwitchQuery(string query, long botId)
        {
            return new NavigationState { { "switch_query", query }, { "switch_bot", botId } };
        }
    }
}
