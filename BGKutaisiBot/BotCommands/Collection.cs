﻿using BGKutaisiBot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;
using Tesera;
using Tesera.Models;
using Tesera.Types.Enums;
using System.Text;
using BGKutaisiBot.Types.Exceptions;
using Telegram.Bot.Types;

namespace BGKutaisiBot.Commands
{
	internal class Collection : Types.BotCommand
	{
		enum SortBy { Titles, Players, Playtimes, Ratings }
		static readonly Lazy<TeseraClient> _lazyTeseraClient = new();

		static TextMessage GetTextMessage(string userLogin, SortBy sortBy)
		{
			var gamesInfo = _lazyTeseraClient.Value.Get<IEnumerable<CustomCollectionGameInfo>>(new Tesera.API.Collections.Base(CollectionType.Own, userLogin, GamesType.All));
			if (gamesInfo is null)
				throw new CancelException(CancelException.Cancel.Current, $"Не удалось получить список игр из коллекции пользователя {userLogin}");

			List<GameInfo> games = [];
			foreach (CustomCollectionGameInfo item in gamesInfo)
				if (!item.Game.IsAddition && !string.IsNullOrEmpty(item.Game.Alias))
				{
					GameInfoResponse? game = _lazyTeseraClient.Value.Get<GameInfoResponse>(new Tesera.API.Games(item.Game.Alias));
					if (game is not null)
						games.Add(game.Game);
				}

			if (games.Count == 0)
				throw new CancelException(CancelException.Cancel.Current, $"Не удалось получить информацию об играх из коллекции пользователя {userLogin}");

			games.Sort((GameInfo x, GameInfo y) =>
			{
				switch (sortBy)
				{
					default:
					case SortBy.Titles:
						return string.Compare(x.Title, y.Title);
					case SortBy.Playtimes:
						int result = x.PlaytimeMax.CompareTo(y.PlaytimeMax);
						return result == 0 ? x.PlaytimeMin.CompareTo(y.PlaytimeMin) : result;
					case SortBy.Players:
						result = x.PlayersMax.CompareTo(y.PlayersMax);
						return result == 0 ? x.PlayersMin.CompareTo(y.PlayersMin) : result;
					case SortBy.Ratings:
						return x.N10Rating.CompareTo(y.N10Rating);
				}
			});

			int i = 0;
			Regex regex = new("(\\.|-|\\(|\\)|!|\\+)");
			StringBuilder stringBuilder = new();
			foreach (GameInfo game in games)
				if (!string.IsNullOrEmpty(game.Title))
				{
					string title = regex.Replace(game.Title, (Match match) => $"\\{match.Groups[0].Value}");
					string? playersCount = game.GetPlayersCount();
					if (!string.IsNullOrEmpty(playersCount))
						playersCount = regex.Replace(playersCount, (Match match) => $"\\{match.Groups[0].Value}");

					stringBuilder.AppendLine($"{++i}\\. [{title}](tesera.ru/game/{game.Alias?.Replace("-", "\\-")})");
					stringBuilder.AppendLine($"  {(i > 9 ? "  " : string.Empty)}"
						+ $"{(playersCount is null ? string.Empty : $"  👥{playersCount}")}"
						+ $"{(game.N10Rating == 0 ? string.Empty : $"  ⭐️{game.N10Rating}")}"
						+ $"{(game.PlaytimeMin == 0 ? string.Empty : $"  ⏳{(game.PlaytimeMin == game.PlaytimeMax || game.PlaytimeMax == 0 ? game.PlaytimeMin : $"{game.PlaytimeMin}\\-{game.PlaytimeMax}")}")}");
				}

			SortBy[] values = Enum.GetValues<SortBy>();
			List<InlineKeyboardButton> buttons = new() { Capacity = values.Length - 1 };
			for (i = 0; i < values.Length; i++)
				if (values[i] != sortBy)
				{
					string callbackData = Types.BotCommand.GetCallbackData(typeof(Collection), "GetCollection", [userLogin, Enum.GetName(values[i])]);
					buttons.Add(new InlineKeyboardButton(values[i] switch { SortBy.Titles => "🔤", SortBy.Players => "👥", SortBy.Playtimes => "⏳", SortBy.Ratings => "⭐️" })
						{ CallbackData = callbackData });
				}

			return new TextMessage(stringBuilder.ToString()) { ParseMode = ParseMode.MarkdownV2, ReplyMarkup = new InlineKeyboardMarkup(buttons), DisableWebPagePreview = true };
		}
		
		public static TextMessage GetCollection(string userLogin, string value)
		{
			if (!Enum.TryParse(typeof(SortBy), value, out object? result))
				throw new CancelException(CancelException.Cancel.Current, $"не удалось выделить тип сортировки из \"{value}\"");

			return GetTextMessage(userLogin, (SortBy)result);			
		}

		public static string Description { get => "Коллекции настольных игр для игротек"; }
		public override TextMessage Respond(string? messageText, out bool finished)
		{
			finished = true;
			Dictionary<string, string> logins = [];
			const string USER_ALIAS_VARIABLE_NAME_PREFIX = "COLLECTION_OWNER_LOGIN_";

			int i = 1;
			while (true)
				if (Environment.GetEnvironmentVariable(USER_ALIAS_VARIABLE_NAME_PREFIX + i++) is { } value)
					logins.Add(value.ToLower(), value);
				else
					break;

			if (logins.Count == 0)
				throw new CancelException(CancelException.Cancel.Current, "в переменных среды отсутствуют логины пользователей Tesera.ru");

			List<UserFullInfo> users = [];
			foreach (string login in logins.Keys)
				if (_lazyTeseraClient.Value.Get<UserFullInfoResponse>(new Tesera.API.User(login))?.User is { } user)
					users.Add(user);

			if (users.Count == 0)
				throw new CancelException(CancelException.Cancel.Current, "не удалось получить данные пользователей Tesera.ru");

			const string COLLECTION_URL_FORMAT = "tesera.ru/user/{0}/games/owns/";
			string UserToString(UserFullInfo user) => $"[{logins[user.Login]}]({string.Format(COLLECTION_URL_FORMAT, logins[user.Login])}) \\({user.Name}\\)";
			string text = "Настольные игры для игротек хранятся в " + users.Count switch
			{
				1 => "коллекции " + UserToString(users[0]),
				2 => $"коллекциях {UserToString(users[0])} и {UserToString(users[1])}",
				_ => "коллекциях:" + string.Concat(users.ConvertAll<string>((UserFullInfo user) => " " + UserToString(user) + (user == users.Last() ? string.Empty : ",")))
			} + "\\. Чью коллекцию вы хотите посмотреть?";

			IReplyMarkup replyMarkup = new InlineKeyboardMarkup(users.ConvertAll<InlineKeyboardButton>((UserFullInfo user) => new InlineKeyboardButton(logins[user.Login] + $"({user.Name})")
				{ CallbackData = GetCallbackData(typeof(Collection), "GetCollection", [logins[user.Login], "Titles"]) }));

			return new TextMessage(text) { ParseMode = ParseMode.MarkdownV2, ReplyMarkup = replyMarkup };
		}
	}
}