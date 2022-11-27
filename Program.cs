using System;
using System.Linq;
using TL;
using TL.Methods;
using WTelegram;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using YoutubeExplode;
using AngleSharp.Io;
using System.Net;

internal class Program
{
    readonly static int id = 18965744;
    readonly static string k = "137bd6ba7f4e94e7d9c6b2468a35a36c";
    readonly static WTelegram.Client client = new WTelegram.Client(id, k);
    static async Task DoLogin(string loginInfo) // (add this method to your code)
    {
        
        while (client.User == null)
            switch (await client.Login(loginInfo)) // returns which config is needed to continue login
            {
                case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                default: loginInfo = null; break;
            }
        Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
    }
    private static async Task Client_OnUpdate(IObject arg)
    {
        Console.WriteLine("Working client_onupdate");
        if (arg is not UpdatesBase updates) return;
        //updates.CollectUsersChats(Users, Chats);
        foreach (var update in updates.UpdateList)
            switch (update)
            {
                case UpdateNewMessage unm: await Message_handler(unm.message); break;
                //case UpdateNewMessageBase unm: await Message_handler(unm.message); break;
            }
    }
    static async Task Message_handler(MessageBase messageBase)
    {
        Console.WriteLine("Working Message_hadler");
        Message message = new Message();
        switch (messageBase)
        {
            case Message m: message = m;
                break;
        }
        string text = message.message;
        string[] url = text.Split(' ').ToArray();

        var chats = await client.Messages_GetAllDialogs();
        InputPeer peer = chats.users[message.From.ID];

        YoutubeClient youtube = new YoutubeClient();
        var stream = await youtube.Videos.Streams.GetManifestAsync(url[3]);
        var thub = await youtube.Videos.GetAsync(url[3]);

        var photo_url = thub.Thumbnails.OrderByDescending(i => i.Resolution.Area).First().Url;
        WebClient pj = new WebClient();
        pj.DownloadFile(photo_url, "photo.jpg");
        var photo = await client.UploadFileAsync("photo.jpg");

        if (url[1] == "video")
        {
            var video = stream.GetMuxedStreams().Where(i => i.VideoQuality.ToString() == url[2]).OrderByDescending(i=> i.Size).First();

            string filename = $"{url[0 ]}.{video.Container.Name}";
            await youtube.Videos.Streams.DownloadAsync(video, filename);

            var file = await client.UploadFileAsync(filename);
            
            await client.SendMessageAsync(peer, "sdbs",  new InputMediaUploadedDocument
            {
                file = file,
                mime_type = $"video/{video.Container.Name}",
                thumb = photo,
                flags = InputMediaUploadedDocument.Flags.has_thumb,
                attributes = new[] { new DocumentAttributeVideo { flags = DocumentAttributeVideo.Flags.supports_streaming }}
            } );
            try
            {
                System.IO.File.Delete(filename);
            }
            catch(Exception e)
            {
                InputPeer mainadmin = chats.users[907267780];
                await client.SendMessageAsync(mainadmin, text: e.Message);
            }
        }
        else
        {
            
            var audio = stream.GetAudioOnlyStreams().OrderByDescending(i => i.Size).First();
            string filename = $"{url[0]}.{audio.Container.Name}";
            await youtube.Videos.Streams.DownloadAsync(audio, filename);
            var file = await client.UploadFileAsync(filename);
            var media = new InputMediaUploadedDocument
            {
                file = file,
                thumb = photo,
                mime_type = $"audio/{audio.Container.Name}",
                flags = InputMediaUploadedDocument.Flags.has_thumb,
                attributes = new[] {new DocumentAttributeAudio { title = thub.Title , flags = DocumentAttributeAudio.Flags.has_title} }
            };
            
            await client.SendMessageAsync(peer, "dsv", media);
        }
    }
    static async Task Main(string[] args)
    {
        await DoLogin("+992988087717");
        //client.OnUpdate()
        using (client)
        {
            client.OnUpdate += Client_OnUpdate;
            
            Console.ReadKey();
        }
    }
}