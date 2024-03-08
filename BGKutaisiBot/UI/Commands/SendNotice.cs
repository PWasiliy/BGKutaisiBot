using BGKutaisiBot.Types;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace BGKutaisiBot.UI.Commands
{
	internal class SendNotice : BotCommand
	{
		public SendNotice(Func<ITelegramBotClient?> getBotClient) : base("отправить афишу", getBotClient)
		{
			const string TEXT_MESSAGE =
				  "🏆  [Настольные игры](t.me/kutaisi\\_offline\\_rus/5077) за 5₾ \\#цена\r\n"
				+ "🗓️  Сегодня \\#{0} c 19:00 до 00:00\r\n"
				+ "[📍](goo.gl/maps/LfxoBVq7ytk4ZdP97)  Клуб настольных игр [Summer Set](t.me/summersetkutaisi) — [Google Maps](goo.gl/maps/LfxoBVq7ytk4ZdP97)\r\n\r\n"

				+ "Обязательно ставьте 👍, если планируете присоединится\r\n"
				+ "[Ответы на F\\.A\\.Q\\.](t.me/bg\\_kutaisi/{1}) {2}в канале [BGK](t.me/bg\\_kutaisi)";

			async Task Function(string[] args)
			{
				await new TextMessage(string.Format(TEXT_MESSAGE, DateTime.Now.ToString("dMMMMyyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),
					Environment.GetEnvironmentVariable("NOTICE_FAQ_MESSAGE_ID") ?? "0", args.Length == 1 ? "" : $"и открытый [опрос по играм](t.me/bg\\_kutaisi/{args[1]}) ")) 
					{ ParseMode = ParseMode.MarkdownV2 }.SendTextMessageAsync(args[0], this.BotClient);
			}

			this.Add(1, Function);
			this.Add(2, Function);
		}
	}
}