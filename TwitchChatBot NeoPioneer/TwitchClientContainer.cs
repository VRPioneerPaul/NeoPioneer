using System.Runtime.ConstrainedExecution;
using TwitchLib.Api;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Bits;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Schedule;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace TwitchChatBot_NeoPioneer
{
    internal class TwitchClientContainer
    {
        public TwitchClient? Client;
        public ConnectionCredentials? Credentials;
        public TwitchAPI? API;
        public HypeTrain? hypeTrain;

        public string BotUsername = "NeoPioneer";
        public string BotOAuth = "user_access_token_bot_account";
        public string secret = "client_secret";
        public string clientId = "client_id";
        public string refreshToken = "refresh_token";
        public string broadcasterOAuth = "user_access_token_broadcaster", broadcaster_refresh_token = "refresh_token_broadcaster";

        public void sendMessage(string message)
        {
            Client.SendMessage(Client.JoinedChannels[0], message);
        }

        public void Initialize()
        {
            LoadEnv();

            clientId = Environment.GetEnvironmentVariable("client_id") ?? clientId;
            secret = Environment.GetEnvironmentVariable("client_secret") ?? secret;
            BotOAuth = Environment.GetEnvironmentVariable("user_access_token") ?? BotOAuth;
            refreshToken = Environment.GetEnvironmentVariable("refresh_token") ?? refreshToken;
            broadcasterOAuth = Environment.GetEnvironmentVariable("broadcasterOAuth") ?? broadcasterOAuth;
            broadcaster_refresh_token = Environment.GetEnvironmentVariable("broadcaster_refresh_token") ?? broadcaster_refresh_token;

            Client = new TwitchClient();
            Credentials = new ConnectionCredentials(BotUsername, BotOAuth);
            Client.OnConnected += OnConnected;
            Client.OnJoinedChannel += JoinedChannel;
            Client.OnMessageReceived += MessageRecieved;
            Client.OnChatCommandReceived += ChatCommand;
            Client.OnNewSubscriber += NewSubscriber;
            Client.OnLog += OnLog;
            Client.Initialize(Credentials);
            Client.Connect();
            API = new TwitchAPI();
            API.Settings.ClientId = clientId;
            API.Settings.Secret = secret;
            API.Settings.AccessToken = BotOAuth;
        }

        private void NewSubscriber(object? sender, TwitchLib.Client.Events.OnNewSubscriberArgs e) => sendMessage($"Willkommen an Bord, @{e.Subscriber.DisplayName}. Ein neuer Pionier ist dabei.");
        private void ReSubscriber(object? sender, TwitchLib.Client.Events.OnReSubscriberArgs e) => sendMessage($"@{e.ReSubscriber.DisplayName} bleibt an bord. {e.ReSubscriber.Months} Monate als Pionier. Danke für den Support.");
        private void GiftSub(object? sender, TwitchLib.Client.Events.OnGiftedSubscriptionArgs e) => sendMessage($"@{e.GiftedSubscription.DisplayName} holt {e.GiftedSubscription.MsgParamRecipientDisplayName} an Bord. Ein Geschenk-Abo für einen neuen Pionier.");
        private void CommunityGiftSub(object? sender, TwitchLib.Client.Events.OnCommunitySubscriptionArgs e) => sendMessage($"@{e.GiftedSubscription.DisplayName} bringt {e.GiftedSubscription.MsgParamMassGiftCount} neue Pioniere an Bord. Starker Support. Er hat insgesamt jetzt {e.GiftedSubscription.MsgParamSenderCount} Subs der Community gegeben.");

        private void hypeTrainBeginn(object? sender, HypeTrainBegin e)
        {
            sendMessage($"Der Hype Train kommt angefahren Pioniere. Kommt und springt auf er fährt ab um {e.ExpiresAt:t}");
        }
        private void hypeTrainProgress(object? sender, HypeTrainProgress e)
        {
            sendMessage($"Pioniere der Hype Train ist aufgelevelt. Er ist jetzt LvL {e.Level} mit {e.Progress} Punkten");
        }
        private void hypeTrainEnd(object? sender, HypeTrainEnd e)
        {
            string top = e.TopContributions != null && e.TopContributions.Length > 0 ? string.Join(", ", e.TopContributions.Select(t => t.UserName)) : "keine";

            sendMessage($"Pioniere Tchu-Tchu der Zug ist abgefahren. Der Top Mitwirkende Pionier war {top} der Hype Train Cooldown endet um {e.CooldownEndsAt:t}");
        }

        //Commands
        private async void ChatCommand(object? sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {
            if (e.Command.CommandText.Equals("beep", StringComparison.OrdinalIgnoreCase))
            {
                sendMessage("Boop!");
            }

            if (e.Command.CommandText.Equals("game", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                {
                    
                    string newGame = e.Command.ChatMessage.Message.Substring(e.Command.CommandText.Length + 1).Trim();
                    var gameResponse = await API.Helix.Games.GetGamesAsync(gameNames: new List<string> { newGame });
                    string gameId = gameResponse.Games.FirstOrDefault()?.Id ?? "";

                    var request = new ModifyChannelInformationRequest
                    {
                        GameId = gameId
                    };

                    await API.Helix.Channels.ModifyChannelInformationAsync(e.Command.ChatMessage.RoomId, request, broadcasterOAuth);

                }
            }

            if (e.Command.CommandText.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                {
                    string newTitle = e.Command.ChatMessage.Message.Substring(e.Command.CommandText.Length + 2).Trim();

                    var request = new ModifyChannelInformationRequest
                    {
                        Title = $"[Ger|Eng] | [{newTitle}] | [!cmds !fa !dc !socials] | [Lurkers Welcome]"
                    };

                    await API.Helix.Channels.ModifyChannelInformationAsync(e.Command.ChatMessage.RoomId, request, broadcasterOAuth);
                }
            }
        }


        //Logs und Debug
        private void OnLog(object? sender, TwitchLib.Client.Events.OnLogArgs e) => Console.WriteLine(e.Data);
        private void MessageRecieved(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e) => Console.WriteLine($"Message from {e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
        private void JoinedChannel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Connected to a Channel named: {e.Channel}");
            sendMessage("Hallo Chat, Ich bin da!");
        }
        private void OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Console.WriteLine("I have Connected");
            Client.JoinChannel("vrpioneerpaul");
        }


        private void LoadEnv()
        {
            try
            {
                var envPath = LocateEnvFile();
                if (envPath == null) return;

                foreach (var rawLine in File.ReadAllLines(envPath))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim().Trim('"');

                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der .env-Datei: {ex.Message}");
            }
        }
        private void SaveEnv(Dictionary<string, string> updates)
        {
            try
            {
                var envPath = LocateEnvFile();
                if (envPath == null)
                {
                    envPath = Path.Combine(AppContext.BaseDirectory, ".env");
                    File.WriteAllText(envPath, "");
                }

                var lines = File.Exists(envPath) ? File.ReadAllLines(envPath).ToList() : new List<string>();

                void SetOrAdd(string key, string value)
                {
                    var idx = lines.FindIndex(l => l.TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
                    var entry = $"{key}={value}";
                    if (idx >= 0) lines[idx] = entry;
                    else lines.Add(entry);
                }

                foreach (var kv in updates)
                    SetOrAdd(kv.Key, kv.Value);

                File.WriteAllLines(envPath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern der .env-Datei: {ex.Message}");
            }
        }
        private string? LocateEnvFile()
        {
            try
            {
                var dir = AppContext.BaseDirectory;
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir, ".env");
                    if (File.Exists(candidate)) return candidate;
                    var parent = Directory.GetParent(dir);
                    dir = parent?.FullName;
                }
            }
            catch
            {
            }
            return null;
        }
    }
}