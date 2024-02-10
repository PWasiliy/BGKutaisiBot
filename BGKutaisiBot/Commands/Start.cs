﻿using BGKutaisiBot.Types;
using Telegram.Bot.Types.Enums;

namespace BGKutaisiBot.Commands
{
	internal class Start : Command
	{
		public override TextMessage Respond(string? input, out bool finished)
		{
			finished = true;
			return new TextMessage("Здравствуйте, вас приветствует бот\\-помощник для канала [BGK](t.me/bg\\_kutaisi)") { ParseMode = ParseMode.MarkdownV2};
		}
	}
}