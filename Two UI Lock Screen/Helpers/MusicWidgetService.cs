using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Two_UI_Lock_Screen.Helpers
{
    public class MusicWidgetService
    {
        private static MusicWidgetService _instance;
        public static MusicWidgetService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MusicWidgetService();
                }
                return _instance;
            }
        }

        // Android-Compatible Media State Properties
        public bool HasActiveTrack { get; private set; }
        public string AppName { get; private set; }
        public string AppIconUrl { get; private set; }
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public string Author { get; private set; }
        public string Writer { get; private set; }
        public string Composer { get; private set; }
        public string AlbumArtist { get; private set; }
        public string MediaUri { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public string TrackId { get; private set; }
        public string VideoId { get { return TrackId; } }
        public long DurationMs { get; private set; }
        public long PositionMs { get; private set; }
        public long BufferedPositionMs { get; private set; }
        public string ErrorMessage { get; private set; }
        public string AudioOutput { get; private set; }
        public int RepeatMode { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string Genre { get; private set; }
        public int TrackNumber { get; private set; }
        public int NumTracks { get; private set; }
        public int Year { get; private set; }
        public int DiscNumber { get; private set; }
        public int RatingStyle { get; private set; }
        public float RatingValue { get; private set; }
        public string QueueTitle { get; private set; }
        public long ActiveQueueItemId { get; private set; }
        public int PlaybackType { get; private set; }
        public int Volume { get; private set; }
        public int MaxVolume { get; private set; }
        public int ShuffleMode { get; private set; }
        public int VolumeControl { get; private set; }
        public string VolumeControlId { get; private set; }
        public string PackageName { get; private set; }
        public string LaunchUri { get; private set; }
        public string Compilation { get; private set; }
        public string Date { get; private set; }
        public int SessionFlags { get; private set; }
        public bool HandlesMediaButtons { get { return (SessionFlags & AndroidSessionFlags.FLAG_HANDLES_MEDIA_BUTTONS) != 0; } }
        public bool HandlesTransportControls { get { return (SessionFlags & AndroidSessionFlags.FLAG_HANDLES_TRANSPORT_CONTROLS) != 0; } }
        public bool HasExclusiveGlobalPriority { get { return (SessionFlags & AndroidSessionFlags.FLAG_EXCLUSIVE_GLOBAL_PRIORITY) != 0; } }
        public List<QueueItemModel> Queue { get; private set; }

        public QueueItemModel ActiveQueueItem
        {
            get
            {
                if (Queue == null || Queue.Count == 0 || ActiveQueueItemId < 0) return null;
                for (int i = 0; i < Queue.Count; i++)
                {
                    if (Queue[i].Id == ActiveQueueItemId) return Queue[i];
                }
                return null;
            }
        }

        // Android AOSP PlaybackState Model
        public int State { get; private set; }
        public long Actions { get; private set; }
        public float PlaybackSpeed { get; private set; }
        public DateTime LastUpdateTimeUtc { get; private set; }

        public bool IsPlaying { get { return State == AndroidPlaybackState.STATE_PLAYING; } }
        public bool IsPaused { get { return State == AndroidPlaybackState.STATE_PAUSED; } }
        public bool IsStopped { get { return State == AndroidPlaybackState.STATE_STOPPED; } }
        public bool IsPlaybackActive { get { return AndroidPlaybackState.IsActive(State); } }
        public bool IsBuffering { get { return State == AndroidPlaybackState.STATE_BUFFERING; } }
        public bool IsError { get { return State == AndroidPlaybackState.STATE_ERROR; } }
        public bool IsFastForwarding { get { return State == AndroidPlaybackState.STATE_FAST_FORWARDING; } }
        public bool IsRewinding { get { return State == AndroidPlaybackState.STATE_REWINDING; } }
        public bool IsConnecting { get { return State == AndroidPlaybackState.STATE_CONNECTING; } }
        public bool IsSkippingToPrev { get { return State == AndroidPlaybackState.STATE_SKIPPING_TO_PREVIOUS; } }
        public bool IsSkippingToNext { get { return State == AndroidPlaybackState.STATE_SKIPPING_TO_NEXT; } }
        public bool IsSkippingToQueueItem { get { return State == AndroidPlaybackState.STATE_SKIPPING_TO_QUEUE_ITEM; } }

        public bool CanPlay { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY) || AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY_PAUSE); } }
        public bool CanPause { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PAUSE) || AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY_PAUSE); } }
        public bool CanSeek { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SEEK_TO); } }
        public bool CanSkipNext { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SKIP_TO_NEXT); } }
        public bool CanSkipPrev { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SKIP_TO_PREVIOUS); } }
        public bool CanFastForward { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_FAST_FORWARD); } }
        public bool CanRewind { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_REWIND); } }
        public bool CanStop { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_STOP); } }
        public bool CanSetRating { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SET_RATING); } }
        public bool CanSetPlaybackSpeed { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SET_PLAYBACK_SPEED); } }
        public bool CanSkipToQueueItem { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_SKIP_TO_QUEUE_ITEM); } }
        public bool CanPrepare { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PREPARE); } }
        public bool CanPrepareFromMediaId { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PREPARE_FROM_MEDIA_ID); } }
        public bool CanPrepareFromSearch { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PREPARE_FROM_SEARCH); } }
        public bool CanPrepareFromUri { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PREPARE_FROM_URI); } }
        public bool CanPlayFromMediaId { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY_FROM_MEDIA_ID); } }
        public bool CanPlayFromSearch { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY_FROM_SEARCH); } }
        public bool CanPlayFromUri { get { return AndroidPlaybackActions.HasAction(Actions, AndroidPlaybackActions.ACTION_PLAY_FROM_URI); } }

        // AOSP Rating Helpers
        public bool IsRated { get { return RatingValue >= 0.0f; } }
        public bool HasHeart { get { return RatingStyle == AndroidRatingStyle.RATING_HEART && RatingValue > 0.0f; } }
        public bool IsThumbUp { get { return RatingStyle == AndroidRatingStyle.RATING_THUMB_UP_DOWN && RatingValue > 0.0f; } }
        public bool IsThumbDown { get { return RatingStyle == AndroidRatingStyle.RATING_THUMB_UP_DOWN && RatingValue < 0.0f; } }
        public float StarRating { get { return (RatingStyle >= AndroidRatingStyle.RATING_3_STARS && RatingStyle <= AndroidRatingStyle.RATING_5_STARS && IsRated) ? RatingValue : AndroidRatingStyle.RATING_NOT_RATED; } }
        public float PercentRating { get { return (RatingStyle == AndroidRatingStyle.RATING_PERCENTAGE && IsRated) ? RatingValue : -1.0f; } }

        public bool IsFavorite { get; private set; }
        public bool IsShuffle { get; private set; }
        public CustomActionModel LeftAction { get; private set; }
        public CustomActionModel RightAction { get; private set; }

        public string ServerEndpoint { get; private set; }

        /// <summary>
        /// Thuật toán nội suy vị trí bài hát thời gian thực theo chuẩn Android (AOSP PlaybackState.getPosition)
        /// Position = BasePosition + PlaybackSpeed * (Now - UpdateTime)
        /// </summary>
        public long CurrentEstimatedPositionMs
        {
            get
            {
                if (State != AndroidPlaybackState.STATE_PLAYING || PlaybackSpeed <= 0.0f)
                {
                    return PositionMs;
                }
                long elapsed = (long)((DateTime.UtcNow - LastUpdateTimeUtc).TotalMilliseconds * PlaybackSpeed);
                long est = PositionMs + elapsed;
                if (DurationMs > 0 && est > DurationMs) est = DurationMs;
                if (est < 0) est = 0;
                return est;
            }
        }

        // Events
        public event Action PlaybackStateChanged;
        public event Action<long, long> PositionTick;

        private DispatcherTimer _pollTimer;
        private DispatcherTimer _positionTimer;
        private bool _isPolling = false;
        private bool _isFetching = false;
        private int _consecutiveErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 3;

        private MusicWidgetService()
        {
            HasActiveTrack = false;
            AppName = "Music Player";
            AppIconUrl = "";
            Title = "";
            Artist = "";
            Album = "";
            Author = "";
            Writer = "";
            Composer = "";
            AlbumArtist = "";
            MediaUri = "";
            ThumbnailUrl = "";
            TrackId = "";
            DurationMs = 0;
            PositionMs = 0;
            BufferedPositionMs = 0;
            ErrorMessage = "";
            AudioOutput = "Điện thoại này";
            RepeatMode = AndroidRepeatMode.REPEAT_MODE_NONE;
            Subtitle = "";
            Description = "";
            Genre = "";
            TrackNumber = 0;
            NumTracks = 0;
            Year = 0;
            DiscNumber = 0;
            RatingStyle = AndroidRatingStyle.RATING_NONE;
            RatingValue = -1.0f;
            QueueTitle = "";
            ActiveQueueItemId = -1;
            PlaybackType = AndroidPlaybackType.PLAYBACK_TYPE_LOCAL;
            Volume = 100;
            MaxVolume = 100;
            ShuffleMode = AndroidShuffleMode.SHUFFLE_MODE_NONE;
            VolumeControl = AndroidVolumeControl.VOLUME_CONTROL_ABSOLUTE;
            VolumeControlId = "";
            PackageName = "";
            LaunchUri = "";
            Compilation = "";
            Date = "";
            Queue = new List<QueueItemModel>();
            State = AndroidPlaybackState.STATE_NONE;
            Actions = AndroidPlaybackActions.DEFAULT_ACTIONS;
            PlaybackSpeed = 1.0f;
            LastUpdateTimeUtc = DateTime.UtcNow;
            IsFavorite = false;
            IsShuffle = false;
            LeftAction = new CustomActionModel { Action = AndroidCustomActions.ACTION_FAVORITE, Name = "Favorite", Icon = "heart", IsActive = false };
            RightAction = new CustomActionModel { Action = AndroidCustomActions.ACTION_SHUFFLE, Name = "Shuffle", Icon = "shuffle", IsActive = false };
            ServerEndpoint = "http://olmp.local";

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(1.5);
            _pollTimer.Tick += PollTimer_Tick;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(500);
            _positionTimer.Tick += PositionTimer_Tick;
        }

        public void Start(string endpoint)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                ServerEndpoint = NormalizeEndpoint(endpoint);
            }

            _isPolling = true;
            _consecutiveErrors = 0;
            _pollTimer.Interval = TimeSpan.FromSeconds(1.5);

            if (!_pollTimer.IsEnabled)
            {
                _pollTimer.Start();
            }

            // Fetch immediately
            var ignored = RefreshTrackAsync();
        }

        public void Stop()
        {
            _isPolling = false;
            if (_pollTimer.IsEnabled)
            {
                _pollTimer.Stop();
            }
            if (_positionTimer.IsEnabled)
            {
                _positionTimer.Stop();
            }
        }

        private static string NormalizeEndpoint(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "http://olmp.local";
            string trimmed = raw.Trim();
            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "http://" + trimmed;
            }
            if (trimmed.EndsWith("/"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            // Nếu người dùng nhập 127.0.0.1 không cổng -> tự động thêm :8088
            if (trimmed.Equals("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "http://127.0.0.1:8088";
            }

            return trimmed;
        }

        /// <summary>
        /// Tự động phân giải URL mạng: ánh xạ các tên miền ảo .local (như olmp.local, nowplaying.local) về 127.0.0.1:8088.
        /// Giữ nguyên cổng cho 127.0.0.1:8088 và các địa chỉ IP cụ thể.
        /// </summary>
        public static string ResolveNetworkUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "http://127.0.0.1:8088";

            try
            {
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    return url;
                }

                string host = uri.Host;
                int port = uri.Port;
                string scheme = uri.Scheme;
                string pathAndQuery = uri.PathAndQuery;

                // 1. Ánh xạ các tên miền ảo .local (olmp.local, nowplaying.local, lockmedia.local, v.v.) và localhost
                if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                    host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    int targetPort = (port == 80 || port <= 0) ? 8088 : port;
                    return string.Format("{0}://127.0.0.1:{1}{2}", scheme, targetPort, pathAndQuery);
                }

                // 2. Với IP 127.0.0.1: nếu không có cổng cụ thể (mặc định HTTP là 80) -> thêm cổng :8088
                if (host == "127.0.0.1" && (port == 80 || port <= 0))
                {
                    return string.Format("{0}://127.0.0.1:8088{1}", scheme, pathAndQuery);
                }

                return url;
            }
            catch
            {
                return url;
            }
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPolling) return;
            await RefreshTrackAsync();
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying) return;
            PositionTick?.Invoke(CurrentEstimatedPositionMs, DurationMs);
        }

        public async Task RefreshTrackAsync()
        {
            if (_isFetching) return;
            _isFetching = true;
            try
            {
                string url = ServerEndpoint + "/api/track";
                string json = await HttpGetAsync(url, 2000);

                if (string.IsNullOrEmpty(json))
                {
                    _consecutiveErrors++;
                    if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    {
                        // Giãn interval thăm dò lên 8 giây để tiết kiệm pin và tránh lãng phí tài nguyên khi server offline
                        if (_pollTimer.Interval.TotalSeconds < 7.0)
                        {
                            _pollTimer.Interval = TimeSpan.FromSeconds(8.0);
                        }

                        if (HasActiveTrack)
                        {
                            HasActiveTrack = false;
                            State = AndroidPlaybackState.STATE_NONE;
                            UpdatePositionTimer();
                            RunOnUI(() => PlaybackStateChanged?.Invoke());
                        }
                    }
                    return;
                }

                _consecutiveErrors = 0;
                // Khôi phục interval nhanh 1.5 giây khi server online
                if (_pollTimer.Interval.TotalSeconds > 2.0)
                {
                    _pollTimer.Interval = TimeSpan.FromSeconds(1.5);
                }
                ParseAndApplyTrackJson(json);
            }
            finally
            {
                _isFetching = false;
            }
        }

        private static bool IsTrackJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            // JSON phải chứa một trong các trường bài hát hoặc cấu trúc trạng thái phát chính thức
            return json.IndexOf("\"hasTrack\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   json.IndexOf("\"title\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   json.IndexOf("\"trackName\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   json.IndexOf("\"track\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   json.IndexOf("\"videoId\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   json.IndexOf("\"trackId\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ParseAndApplyTrackJson(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json) || !IsTrackJson(json))
                {
                    return;
                }

                bool hasTrack = ExtractBool(json, "hasTrack");
                if (!hasTrack && json.Contains("\"track\""))
                {
                    hasTrack = true;
                }

                // Phân tích trạng thái phát theo chuẩn Android PlaybackState
                int parsedState = -1;
                long stateLong = ExtractLong(json, "state");
                if (stateLong > 0 || json.Contains("\"state\":0") || json.Contains("\"state\": 0"))
                {
                    parsedState = (int)stateLong;
                }
                else
                {
                    string stateStr = ExtractString(json, "state").Trim().ToLowerInvariant();
                    if (stateStr == "playing") parsedState = AndroidPlaybackState.STATE_PLAYING;
                    else if (stateStr == "paused") parsedState = AndroidPlaybackState.STATE_PAUSED;
                    else if (stateStr == "buffering") parsedState = AndroidPlaybackState.STATE_BUFFERING;
                    else if (stateStr == "stopped") parsedState = AndroidPlaybackState.STATE_STOPPED;
                    else if (stateStr == "none") parsedState = AndroidPlaybackState.STATE_NONE;
                    else if (stateStr == "fast_forwarding") parsedState = AndroidPlaybackState.STATE_FAST_FORWARDING;
                    else if (stateStr == "rewinding") parsedState = AndroidPlaybackState.STATE_REWINDING;
                    else if (stateStr == "error") parsedState = AndroidPlaybackState.STATE_ERROR;
                    else if (stateStr == "connecting") parsedState = AndroidPlaybackState.STATE_CONNECTING;
                    else if (stateStr == "skipping_to_previous" || stateStr == "skipping_prev") parsedState = AndroidPlaybackState.STATE_SKIPPING_TO_PREVIOUS;
                    else if (stateStr == "skipping_to_next" || stateStr == "skipping_next") parsedState = AndroidPlaybackState.STATE_SKIPPING_TO_NEXT;
                    else if (stateStr == "skipping_to_queue_item") parsedState = AndroidPlaybackState.STATE_SKIPPING_TO_QUEUE_ITEM;
                }

                if (parsedState < 0)
                {
                    bool playing = ExtractBool(json, "isPlaying");
                    if (!playing && ExtractBool(json, "playing")) playing = true;
                    parsedState = playing ? AndroidPlaybackState.STATE_PLAYING : AndroidPlaybackState.STATE_PAUSED;
                }

                if (!hasTrack || parsedState == AndroidPlaybackState.STATE_NONE)
                {
                    if (HasActiveTrack)
                    {
                        HasActiveTrack = false;
                        State = AndroidPlaybackState.STATE_NONE;
                        AppIconUrl = "";
                        BufferedPositionMs = 0;
                        ErrorMessage = "";
                        AudioOutput = "Điện thoại này";
                        RepeatMode = AndroidRepeatMode.REPEAT_MODE_NONE;
                        Subtitle = "";
                        Description = "";
                        Genre = "";
                        TrackNumber = 0;
                        NumTracks = 0;
                        Year = 0;
                        DiscNumber = 0;
                        RatingStyle = AndroidRatingStyle.RATING_NONE;
                        RatingValue = -1.0f;
                        QueueTitle = "";
                        ActiveQueueItemId = -1;
                        PlaybackType = AndroidPlaybackType.PLAYBACK_TYPE_LOCAL;
                        Volume = 100;
                        MaxVolume = 100;
                        ShuffleMode = AndroidShuffleMode.SHUFFLE_MODE_NONE;
                        VolumeControl = AndroidVolumeControl.VOLUME_CONTROL_ABSOLUTE;
                        VolumeControlId = "";
                        PackageName = "";
                        LaunchUri = "";
                        Author = "";
                        Writer = "";
                        Composer = "";
                        AlbumArtist = "";
                        MediaUri = "";
                        Compilation = "";
                        Date = "";
                        SessionFlags = 0;
                        Queue = new List<QueueItemModel>();
                        UpdatePositionTimer();
                        RunOnUI(() => PlaybackStateChanged?.Invoke());
                    }
                    return;
                }

                string appName = ExtractString(json, "appName");
                if (string.IsNullOrEmpty(appName)) appName = ExtractString(json, "app");
                if (string.IsNullOrEmpty(appName)) appName = "Music Player";

                string packageName = ExtractString(json, "packageName");
                if (string.IsNullOrEmpty(packageName)) packageName = ExtractString(json, "package");

                string launchUri = ExtractString(json, "launchUri");
                if (string.IsNullOrEmpty(launchUri)) launchUri = ExtractString(json, "intentUri");
                if (string.IsNullOrEmpty(launchUri)) launchUri = ExtractString(json, "appUri");

                string appIcon = ExtractString(json, "appIcon");
                if (string.IsNullOrEmpty(appIcon)) appIcon = ExtractString(json, "appIconUrl");
                if (string.IsNullOrEmpty(appIcon)) appIcon = ExtractString(json, "displayIconUri");
                if (string.IsNullOrEmpty(appIcon)) appIcon = ExtractString(json, "iconUri");

                string title = ExtractString(json, "displayTitle");
                if (string.IsNullOrEmpty(title)) title = ExtractString(json, "title");
                if (string.IsNullOrEmpty(title)) title = ExtractString(json, "trackName");

                string author = ExtractString(json, "author");
                string writer = ExtractString(json, "writer");
                string composer = ExtractString(json, "composer");
                string albumArtist = ExtractString(json, "albumArtist");
                string mediaUri = ExtractString(json, "mediaUri");

                string artist = ExtractString(json, "artist");
                if (string.IsNullOrEmpty(artist)) artist = ExtractString(json, "artistName");
                if (string.IsNullOrEmpty(artist)) artist = albumArtist;
                if (string.IsNullOrEmpty(artist)) artist = author;
                if (string.IsNullOrEmpty(artist)) artist = composer;

                string album = ExtractString(json, "album");
                if (string.IsNullOrEmpty(album)) album = ExtractString(json, "albumTitle");

                string thumb = ExtractString(json, "thumbnail");
                if (string.IsNullOrEmpty(thumb)) thumb = ExtractString(json, "thumbnailUrl");
                if (string.IsNullOrEmpty(thumb)) thumb = ExtractString(json, "coverArt");
                if (string.IsNullOrEmpty(thumb)) thumb = ExtractString(json, "albumArt");

                string trackId = ExtractString(json, "trackId");
                if (string.IsNullOrEmpty(trackId)) trackId = ExtractString(json, "videoId");
                if (string.IsNullOrEmpty(trackId)) trackId = ExtractString(json, "id");

                long duration = ExtractLong(json, "durationMs");
                if (duration <= 0)
                {
                    long durSec = ExtractLong(json, "duration");
                    if (durSec > 0) duration = durSec * 1000;
                }

                long position = ExtractLong(json, "positionMs");
                if (position <= 0)
                {
                    long posSec = ExtractLong(json, "position");
                    if (posSec > 0) position = posSec * 1000;
                }

                // Vị trí tải đệm (Buffered Position) chuẩn AOSP PlaybackState.getBufferedPosition
                long bufferedPos = ExtractLong(json, "bufferedPositionMs");
                if (bufferedPos <= 0)
                {
                    long bufSec = ExtractLong(json, "bufferedPosition");
                    if (bufSec > 0) bufferedPos = bufSec * 1000;
                }
                if (bufferedPos <= 0) bufferedPos = ExtractLong(json, "bufferedMs");
                if (bufferedPos <= 0) bufferedPos = ExtractLong(json, "bufferMs");
                if (duration > 0 && bufferedPos > duration) bufferedPos = duration;

                // Thông báo lỗi người dùng chuẩn AOSP PlaybackState.getErrorMessage
                string errorMsg = ExtractString(json, "errorMessage");
                if (string.IsNullOrEmpty(errorMsg)) errorMsg = ExtractString(json, "error");
                if (string.IsNullOrEmpty(errorMsg)) errorMsg = ExtractString(json, "message");

                // Thiết bị phát âm thanh (Audio Output Device)
                string audioOut = ExtractString(json, "audioOutput");
                if (string.IsNullOrEmpty(audioOut)) audioOut = ExtractString(json, "outputDevice");
                if (string.IsNullOrEmpty(audioOut)) audioOut = ExtractString(json, "device");
                if (string.IsNullOrEmpty(audioOut)) audioOut = "Điện thoại này";

                // Chế độ lặp lại 3 nấc chuẩn AOSP (Repeat Mode: None / All / One)
                int repMode = AndroidRepeatMode.REPEAT_MODE_NONE;
                long repModeNum = ExtractLong(json, "repeatMode");
                if (repModeNum > 0 || json.Contains("\"repeatMode\":0") || json.Contains("\"repeatMode\": 0"))
                {
                    repMode = (int)repModeNum;
                }
                else
                {
                    string repStr = ExtractString(json, "repeat").Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(repStr)) repStr = ExtractString(json, "repeatMode").Trim().ToLowerInvariant();
                    if (repStr == "one" || repStr == "track" || repStr == "single") repMode = AndroidRepeatMode.REPEAT_MODE_ONE;
                    else if (repStr == "all" || repStr == "true" || repStr == "playlist") repMode = AndroidRepeatMode.REPEAT_MODE_ALL;
                    else repMode = AndroidRepeatMode.REPEAT_MODE_NONE;
                }

                // Dữ liệu mở rộng chuẩn AOSP MediaMetadata & MediaDescription
                string subtitle = ExtractString(json, "subtitle");
                if (string.IsNullOrEmpty(subtitle)) subtitle = ExtractString(json, "displaySubtitle");

                string description = ExtractString(json, "description");
                if (string.IsNullOrEmpty(description)) description = ExtractString(json, "displayDescription");

                string genre = ExtractString(json, "genre");
                string compilation = ExtractString(json, "compilation");
                string date = ExtractString(json, "date");
                int trackNumber = (int)ExtractLong(json, "trackNumber");
                int numTracks = (int)ExtractLong(json, "numTracks");
                if (numTracks <= 0) numTracks = (int)ExtractLong(json, "trackCount");
                int year = (int)ExtractLong(json, "year");
                if (year <= 0)
                {
                    if (!string.IsNullOrEmpty(date) && date.Length >= 4)
                    {
                        int yVal;
                        if (int.TryParse(date.Substring(0, 4), out yVal)) year = yVal;
                    }
                }
                int discNumber = (int)ExtractLong(json, "discNumber");
                string queueTitle = ExtractString(json, "queueTitle");
                if (string.IsNullOrEmpty(queueTitle)) queueTitle = ExtractString(json, "playlistTitle");
                long activeQueueItemId = ExtractLong(json, "activeQueueItemId");
                if (activeQueueItemId <= 0) activeQueueItemId = ExtractLong(json, "activeItemId");

                int playbackType = AndroidPlaybackType.PLAYBACK_TYPE_LOCAL;
                long pTypeVal = ExtractLong(json, "playbackType");
                if (pTypeVal == AndroidPlaybackType.PLAYBACK_TYPE_REMOTE) playbackType = AndroidPlaybackType.PLAYBACK_TYPE_REMOTE;
                else if (ExtractString(json, "playbackType").ToLowerInvariant() == "remote") playbackType = AndroidPlaybackType.PLAYBACK_TYPE_REMOTE;

                int volume = 100;
                long volVal = ExtractLong(json, "volume");
                if (volVal > 0) volume = (int)volVal;

                int maxVol = 100;
                long maxVolVal = ExtractLong(json, "maxVolume");
                if (maxVolVal > 0) maxVol = (int)maxVolVal;

                int volumeControl = AndroidVolumeControl.VOLUME_CONTROL_ABSOLUTE;
                long volCtrlVal = ExtractLong(json, "volumeControl");
                if (volCtrlVal >= 0 && (json.Contains("\"volumeControl\":") || json.Contains("\"volumeControl\" :")))
                {
                    volumeControl = (int)volCtrlVal;
                }
                else
                {
                    string vcStr = ExtractString(json, "volumeControl").Trim().ToLowerInvariant();
                    if (vcStr == "fixed") volumeControl = AndroidVolumeControl.VOLUME_CONTROL_FIXED;
                    else if (vcStr == "relative") volumeControl = AndroidVolumeControl.VOLUME_CONTROL_RELATIVE;
                    else if (vcStr == "absolute") volumeControl = AndroidVolumeControl.VOLUME_CONTROL_ABSOLUTE;
                }

                string volumeControlId = ExtractString(json, "volumeControlId");

                // Cờ phiên làm việc chuẩn AOSP MediaSession.setFlags / MediaController.getFlags
                int sessionFlags = (int)ExtractLong(json, "flags");
                if (sessionFlags <= 0) sessionFlags = (int)ExtractLong(json, "sessionFlags");
                if (sessionFlags <= 0)
                {
                    sessionFlags = AndroidSessionFlags.FLAG_HANDLES_MEDIA_BUTTONS | AndroidSessionFlags.FLAG_HANDLES_TRANSPORT_CONTROLS;
                }

                // Yêu thích & Trộn bài
                bool isFav = ExtractBool(json, "isFavorite");
                if (!isFav && ExtractBool(json, "favorite")) isFav = true;

                bool isShuffle = ExtractBool(json, "isShuffle");
                if (!isShuffle && ExtractBool(json, "shuffle")) isShuffle = true;
                int shuffMode = AndroidShuffleMode.SHUFFLE_MODE_NONE;
                long shuffLong = ExtractLong(json, "shuffleMode");
                if (shuffLong > 0)
                {
                    shuffMode = (int)shuffLong;
                    if (shuffMode != AndroidShuffleMode.SHUFFLE_MODE_NONE) isShuffle = true;
                }
                else if (isShuffle)
                {
                    shuffMode = AndroidShuffleMode.SHUFFLE_MODE_ALL;
                }

                // Hệ thống đánh giá bài hát chuẩn AOSP Rating.java
                int ratingStyle = (int)ExtractLong(json, "ratingStyle");
                float ratingVal = -1.0f;
                string rValStr = ExtractString(json, "ratingValue");
                if (string.IsNullOrEmpty(rValStr)) rValStr = ExtractString(json, "userRating");
                if (string.IsNullOrEmpty(rValStr)) rValStr = ExtractString(json, "rating");
                if (!string.IsNullOrEmpty(rValStr))
                {
                    float rv;
                    if (float.TryParse(rValStr, NumberStyles.Float, CultureInfo.InvariantCulture, out rv))
                    {
                        ratingVal = rv;
                    }
                }
                if (ratingStyle <= 0 && isFav)
                {
                    ratingStyle = AndroidRatingStyle.RATING_HEART;
                    ratingVal = 1.0f;
                }

                // Tốc độ phát (Playback Speed)
                float speed = 1.0f;
                string speedStr = ExtractString(json, "speed");
                if (string.IsNullOrEmpty(speedStr)) speedStr = ExtractString(json, "playbackSpeed");
                if (!string.IsNullOrEmpty(speedStr))
                {
                    float sVal;
                    if (float.TryParse(speedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out sVal) && sVal > 0.0f)
                    {
                        speed = sVal;
                    }
                }

                // Cờ Actions bitmask chuẩn Android
                long actions = 0;
                long actNum = ExtractLong(json, "actions");
                if (actNum > 0)
                {
                    actions = actNum;
                }
                else
                {
                    actions = AndroidPlaybackActions.ACTION_PLAY | AndroidPlaybackActions.ACTION_PAUSE | AndroidPlaybackActions.ACTION_PLAY_PAUSE;
                }

                if (json.Contains("\"canSeek\""))
                {
                    if (ExtractBool(json, "canSeek")) actions |= AndroidPlaybackActions.ACTION_SEEK_TO;
                    else actions &= ~AndroidPlaybackActions.ACTION_SEEK_TO;
                }
                if (json.Contains("\"canSkipNext\""))
                {
                    if (ExtractBool(json, "canSkipNext")) actions |= AndroidPlaybackActions.ACTION_SKIP_TO_NEXT;
                    else actions &= ~AndroidPlaybackActions.ACTION_SKIP_TO_NEXT;
                }
                if (json.Contains("\"canSkipPrev\""))
                {
                    if (ExtractBool(json, "canSkipPrev")) actions |= AndroidPlaybackActions.ACTION_SKIP_TO_PREVIOUS;
                    else actions &= ~AndroidPlaybackActions.ACTION_SKIP_TO_PREVIOUS;
                }

                // Phân tích các nút tùy biến (PlaybackState.CustomAction)
                CustomActionModel leftAct = null;
                CustomActionModel rightAct = null;

                int customActionsIdx = json.IndexOf("\"customActions\":", StringComparison.OrdinalIgnoreCase);
                if (customActionsIdx >= 0)
                {
                    int startArr = json.IndexOf('[', customActionsIdx);
                    int endArr = startArr >= 0 ? json.IndexOf(']', startArr) : -1;
                    if (startArr >= 0 && endArr > startArr)
                    {
                        string arrStr = json.Substring(startArr, endArr - startArr + 1);
                        int objStart = 0;
                        int foundCount = 0;
                        while ((objStart = arrStr.IndexOf('{', objStart)) >= 0)
                        {
                            int objEnd = arrStr.IndexOf('}', objStart);
                            if (objEnd > objStart)
                            {
                                string objStr = arrStr.Substring(objStart, objEnd - objStart + 1);
                                var item = ParseCustomActionJson(objStr);
                                if (item != null)
                                {
                                    if (foundCount == 0) leftAct = item;
                                    else if (foundCount == 1) rightAct = item;
                                    foundCount++;
                                }
                                objStart = objEnd + 1;
                            }
                            else break;
                        }
                    }
                }

                if (leftAct == null)
                {
                    int lIdx = json.IndexOf("\"customActionLeft\":", StringComparison.OrdinalIgnoreCase);
                    if (lIdx >= 0)
                    {
                        int s = json.IndexOf('{', lIdx);
                        int e = s >= 0 ? json.IndexOf('}', s) : -1;
                        if (s >= 0 && e > s) leftAct = ParseCustomActionJson(json.Substring(s, e - s + 1));
                    }
                }

                if (rightAct == null)
                {
                    int rIdx = json.IndexOf("\"customActionRight\":", StringComparison.OrdinalIgnoreCase);
                    if (rIdx >= 0)
                    {
                        int s = json.IndexOf('{', rIdx);
                        int e = s >= 0 ? json.IndexOf('}', s) : -1;
                        if (s >= 0 && e > s) rightAct = ParseCustomActionJson(json.Substring(s, e - s + 1));
                    }
                }

                // Phân tích danh sách hàng đợi (MediaSession.QueueItem)
                List<QueueItemModel> queueList = null;
                int queueIdx = json.IndexOf("\"queue\":", StringComparison.OrdinalIgnoreCase);
                if (queueIdx >= 0)
                {
                    int startArr = json.IndexOf('[', queueIdx);
                    int endArr = startArr >= 0 ? json.IndexOf(']', startArr) : -1;
                    if (startArr >= 0 && endArr > startArr)
                    {
                        queueList = new List<QueueItemModel>();
                        string arrStr = json.Substring(startArr, endArr - startArr + 1);
                        int objStart = 0;
                        while ((objStart = arrStr.IndexOf('{', objStart)) >= 0)
                        {
                            int objEnd = arrStr.IndexOf('}', objStart);
                            if (objEnd > objStart)
                            {
                                string objStr = arrStr.Substring(objStart, objEnd - objStart + 1);
                                var qItem = ParseQueueItemJson(objStr);
                                if (qItem != null)
                                {
                                    queueList.Add(qItem);
                                }
                                objStart = objEnd + 1;
                            }
                            else break;
                        }
                    }
                }

                if (leftAct == null)
                {
                    if (ratingStyle == AndroidRatingStyle.RATING_THUMB_UP_DOWN)
                    {
                        leftAct = new CustomActionModel
                        {
                            Action = AndroidCustomActions.ACTION_THUMBS_UP,
                            Name = "Thumbs Up",
                            Icon = "thumbs_up",
                            IsActive = (ratingVal > 0.0f)
                        };
                    }
                    else if (ratingStyle >= AndroidRatingStyle.RATING_3_STARS && ratingStyle <= AndroidRatingStyle.RATING_5_STARS)
                    {
                        leftAct = new CustomActionModel
                        {
                            Action = AndroidCustomActions.ACTION_RATING,
                            Name = "Rating",
                            Icon = "star",
                            IsActive = (ratingVal > 0.0f)
                        };
                    }
                    else
                    {
                        leftAct = new CustomActionModel
                        {
                            Action = AndroidCustomActions.ACTION_FAVORITE,
                            Name = "Favorite",
                            Icon = "heart",
                            IsActive = isFav
                        };
                    }
                }
                if (rightAct == null)
                {
                    rightAct = new CustomActionModel
                    {
                        Action = AndroidCustomActions.ACTION_SHUFFLE,
                        Name = "Shuffle",
                        Icon = "shuffle",
                        IsActive = isShuffle
                    };
                }

                if (rightAct != null && (rightAct.Action == AndroidCustomActions.ACTION_REPEAT || rightAct.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    rightAct.IsActive = (repMode != AndroidRepeatMode.REPEAT_MODE_NONE);
                    rightAct.Icon = (repMode == AndroidRepeatMode.REPEAT_MODE_ONE) ? "repeat_one" : "repeat";
                }
                if (leftAct != null && (leftAct.Action == AndroidCustomActions.ACTION_REPEAT || leftAct.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    leftAct.IsActive = (repMode != AndroidRepeatMode.REPEAT_MODE_NONE);
                    leftAct.Icon = (repMode == AndroidRepeatMode.REPEAT_MODE_ONE) ? "repeat_one" : "repeat";
                }

                if (rightAct != null && (rightAct.Action == AndroidCustomActions.ACTION_SHUFFLE || rightAct.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    rightAct.IsActive = isShuffle;
                }
                if (leftAct != null && (leftAct.Action == AndroidCustomActions.ACTION_SHUFFLE || leftAct.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    leftAct.IsActive = isShuffle;
                }

                bool actionsChanged = (LeftAction == null || RightAction == null ||
                                       LeftAction.Action != leftAct.Action ||
                                       LeftAction.Icon != leftAct.Icon ||
                                       LeftAction.IsActive != leftAct.IsActive ||
                                       RightAction.Action != rightAct.Action ||
                                       RightAction.Icon != rightAct.Icon ||
                                       RightAction.IsActive != rightAct.IsActive);

                bool changed = (!HasActiveTrack ||
                                AppName != appName ||
                                AppIconUrl != appIcon ||
                                Title != title ||
                                Artist != artist ||
                                ThumbnailUrl != thumb ||
                                State != parsedState ||
                                Actions != actions ||
                                IsFavorite != isFav ||
                                IsShuffle != isShuffle ||
                                RepeatMode != repMode ||
                                ErrorMessage != errorMsg ||
                                AudioOutput != audioOut ||
                                Subtitle != subtitle ||
                                TrackNumber != trackNumber ||
                                NumTracks != numTracks ||
                                Year != year ||
                                DiscNumber != discNumber ||
                                RatingStyle != ratingStyle ||
                                ShuffleMode != shuffMode ||
                                QueueTitle != queueTitle ||
                                PlaybackType != playbackType ||
                                VolumeControl != volumeControl ||
                                VolumeControlId != volumeControlId ||
                                PackageName != packageName ||
                                LaunchUri != launchUri ||
                                Author != author ||
                                Writer != writer ||
                                Composer != composer ||
                                AlbumArtist != albumArtist ||
                                MediaUri != mediaUri ||
                                Compilation != compilation ||
                                Date != date ||
                                SessionFlags != sessionFlags ||
                                actionsChanged ||
                                Math.Abs(PositionMs - position) > 2500 ||
                                Math.Abs(BufferedPositionMs - bufferedPos) > 4000);

                HasActiveTrack = true;
                AppName = appName;
                AppIconUrl = appIcon ?? "";
                Title = string.IsNullOrEmpty(title) ? "Unknown Track" : title;
                Artist = string.IsNullOrEmpty(artist) ? appName : artist;
                Album = album ?? "";
                Author = author ?? "";
                Writer = writer ?? "";
                Composer = composer ?? "";
                AlbumArtist = albumArtist ?? "";
                MediaUri = mediaUri ?? "";
                ThumbnailUrl = thumb ?? "";
                TrackId = trackId ?? "";
                DurationMs = duration;
                PositionMs = position;
                BufferedPositionMs = bufferedPos;
                ErrorMessage = errorMsg ?? "";
                AudioOutput = audioOut ?? "Điện thoại này";
                RepeatMode = repMode;
                Subtitle = subtitle ?? "";
                Description = description ?? "";
                Genre = genre ?? "";
                TrackNumber = trackNumber;
                NumTracks = numTracks;
                Year = year;
                DiscNumber = discNumber;
                RatingStyle = ratingStyle;
                RatingValue = ratingVal;
                QueueTitle = queueTitle ?? "";
                ActiveQueueItemId = activeQueueItemId;
                PlaybackType = playbackType;
                Volume = volume;
                MaxVolume = maxVol;
                ShuffleMode = shuffMode;
                VolumeControl = volumeControl;
                VolumeControlId = volumeControlId ?? "";
                PackageName = packageName ?? "";
                LaunchUri = launchUri ?? "";
                Compilation = compilation ?? "";
                Date = date ?? "";
                SessionFlags = sessionFlags;
                if (queueList != null)
                {
                    Queue = queueList;
                }
                else if (Queue == null)
                {
                    Queue = new List<QueueItemModel>();
                }
                State = parsedState;
                Actions = actions;
                PlaybackSpeed = speed;
                LastUpdateTimeUtc = DateTime.UtcNow;
                LeftAction = leftAct;
                RightAction = rightAct;
                IsFavorite = leftAct.Action == AndroidCustomActions.ACTION_FAVORITE ? leftAct.IsActive : isFav;
                IsShuffle = rightAct.Action == AndroidCustomActions.ACTION_SHUFFLE ? rightAct.IsActive : isShuffle;

                UpdatePositionTimer();

                if (changed)
                {
                    RunOnUI(() => PlaybackStateChanged?.Invoke());
                }
                else
                {
                    RunOnUI(() => PositionTick?.Invoke(CurrentEstimatedPositionMs, DurationMs));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[MusicWidgetService] Parse error: " + ex.Message);
            }
        }

        private void UpdatePositionTimer()
        {
            RunOnUI(() =>
            {
                if (IsPlaying)
                {
                    if (!_positionTimer.IsEnabled) _positionTimer.Start();
                }
                else
                {
                    if (_positionTimer.IsEnabled) _positionTimer.Stop();
                }
            });
        }

        public async Task SendPlayAsync()
        {
            State = AndroidPlaybackState.STATE_PLAYING;
            PlaybackSpeed = 1.0f;
            LastUpdateTimeUtc = DateTime.UtcNow;
            UpdatePositionTimer();
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = ServerEndpoint + "/api/play";
            string res = await HttpGetAsync(url, 1500);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                await RefreshTrackAsync();
            }
        }

        public async Task SendPauseAsync()
        {
            PositionMs = CurrentEstimatedPositionMs;
            State = AndroidPlaybackState.STATE_PAUSED;
            PlaybackSpeed = 0.0f;
            LastUpdateTimeUtc = DateTime.UtcNow;
            UpdatePositionTimer();
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = ServerEndpoint + "/api/pause";
            string res = await HttpGetAsync(url, 1500);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                await RefreshTrackAsync();
            }
        }

        public async Task SendSkipNextAsync()
        {
            if (!CanSkipNext) return;

            // Đặt trạng thái Buffering tạm thời để người dùng thấy phản hồi tức thì mà widget vẫn giữ nguyên trên màn hình
            State = AndroidPlaybackState.STATE_BUFFERING;
            PositionMs = 0;
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = ServerEndpoint + "/api/next";
            string res = await HttpGetAsync(url, 2000);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                // Nếu server chưa trả về track info trong response hoặc trả về ack ("status":"next"),
                // đợi audio task chuyển bài rồi chủ động làm mới
                await Task.Delay(250);
                await RefreshTrackAsync();
            }
        }

        public async Task SendSkipPrevAsync()
        {
            if (!CanSkipPrev) return;

            // Đặt trạng thái Buffering tạm thời để người dùng thấy phản hồi tức thì mà widget vẫn giữ nguyên trên màn hình
            State = AndroidPlaybackState.STATE_BUFFERING;
            PositionMs = 0;
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = ServerEndpoint + "/api/prev";
            string res = await HttpGetAsync(url, 2000);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                // Nếu server chưa trả về track info trong response hoặc trả về ack ("status":"prev"),
                // đợi audio task chuyển bài rồi chủ động làm mới
                await Task.Delay(250);
                await RefreshTrackAsync();
            }
        }

        public async Task SendSeekAsync(long positionMs)
        {
            if (!CanSeek) return;
            PositionMs = positionMs;
            LastUpdateTimeUtc = DateTime.UtcNow;
            RunOnUI(() => PositionTick?.Invoke(PositionMs, DurationMs));

            string url = string.Format("{0}/api/seek?pos={1}", ServerEndpoint, positionMs);
            await HttpGetAsync(url, 1500);
        }

        public async Task SendCustomActionAsync(string actionName, string value = null)
        {
            if (string.IsNullOrEmpty(actionName)) return;
            string url = string.Format("{0}/api/action?name={1}", ServerEndpoint, Uri.EscapeDataString(actionName));
            if (!string.IsNullOrEmpty(value))
            {
                url += "&value=" + Uri.EscapeDataString(value);
            }
            await HttpGetAsync(url, 1500);
        }

        public async Task SendFavoriteAsync(bool favorite)
        {
            IsFavorite = favorite;
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format("{0}/api/favorite?value={1}", ServerEndpoint, favorite ? "true" : "false");
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_FAVORITE, favorite ? "true" : "false");
        }

        public async Task SendShuffleAsync(bool shuffle)
        {
            IsShuffle = shuffle;
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format("{0}/api/shuffle?value={1}", ServerEndpoint, shuffle ? "true" : "false");
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_SHUFFLE, shuffle ? "true" : "false");
        }

        public async Task SendFastForwardAsync()
        {
            string url = ServerEndpoint + "/api/fastforward";
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_FAST_FORWARD);
        }

        public async Task SendRewindAsync()
        {
            string url = ServerEndpoint + "/api/rewind";
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_REWIND);
        }

        public async Task SendStopAsync()
        {
            State = AndroidPlaybackState.STATE_STOPPED;
            PlaybackSpeed = 0.0f;
            UpdatePositionTimer();
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = ServerEndpoint + "/api/stop";
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_STOP);
        }

        public async Task SendPlaybackSpeedAsync(float speed)
        {
            if (speed <= 0.0f) return;
            PlaybackSpeed = speed;
            LastUpdateTimeUtc = DateTime.UtcNow;
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format(CultureInfo.InvariantCulture, "{0}/api/speed?value={1:F2}", ServerEndpoint, speed);
            await HttpGetAsync(url, 1500);
        }

        public async Task SendRepeatModeAsync(int mode)
        {
            RepeatMode = mode;
            string modeStr = "none";
            if (mode == AndroidRepeatMode.REPEAT_MODE_ALL) modeStr = "all";
            else if (mode == AndroidRepeatMode.REPEAT_MODE_ONE) modeStr = "one";

            if (RightAction != null && (RightAction.Action == AndroidCustomActions.ACTION_REPEAT || RightAction.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                RightAction.IsActive = (mode != AndroidRepeatMode.REPEAT_MODE_NONE);
                RightAction.Icon = (mode == AndroidRepeatMode.REPEAT_MODE_ONE) ? "repeat_one" : "repeat";
            }
            if (LeftAction != null && (LeftAction.Action == AndroidCustomActions.ACTION_REPEAT || LeftAction.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                LeftAction.IsActive = (mode != AndroidRepeatMode.REPEAT_MODE_NONE);
                LeftAction.Icon = (mode == AndroidRepeatMode.REPEAT_MODE_ONE) ? "repeat_one" : "repeat";
            }

            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format("{0}/api/repeat?mode={1}&value={2}", ServerEndpoint, mode, modeStr);
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_REPEAT, modeStr);
        }

        public async Task CycleRepeatModeAsync()
        {
            int nextMode;
            if (RepeatMode == AndroidRepeatMode.REPEAT_MODE_NONE)
            {
                nextMode = AndroidRepeatMode.REPEAT_MODE_ALL;
            }
            else if (RepeatMode == AndroidRepeatMode.REPEAT_MODE_ALL)
            {
                nextMode = AndroidRepeatMode.REPEAT_MODE_ONE;
            }
            else
            {
                nextMode = AndroidRepeatMode.REPEAT_MODE_NONE;
            }
            await SendRepeatModeAsync(nextMode);
        }

        public async Task SendRatingAsync(int style, float value)
        {
            RatingStyle = style;
            RatingValue = value;
            if (style == AndroidRatingStyle.RATING_HEART)
            {
                IsFavorite = (value > 0.0f);
            }
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format(CultureInfo.InvariantCulture, "{0}/api/rating?style={1}&value={2:F2}", ServerEndpoint, style, value);
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_RATING, string.Format(CultureInfo.InvariantCulture, "{0}:{1:F2}", style, value));
        }

        public async Task SendShuffleModeAsync(int mode)
        {
            ShuffleMode = mode;
            IsShuffle = (mode != AndroidShuffleMode.SHUFFLE_MODE_NONE);
            if (RightAction != null && (RightAction.Action == AndroidCustomActions.ACTION_SHUFFLE || RightAction.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                RightAction.IsActive = IsShuffle;
            }
            if (LeftAction != null && (LeftAction.Action == AndroidCustomActions.ACTION_SHUFFLE || LeftAction.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                LeftAction.IsActive = IsShuffle;
            }
            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string url = string.Format("{0}/api/shuffle?mode={1}&value={2}", ServerEndpoint, mode, IsShuffle ? "true" : "false");
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_SHUFFLE, mode.ToString());
        }

        public async Task CycleShuffleModeAsync()
        {
            int nextMode = (ShuffleMode == AndroidShuffleMode.SHUFFLE_MODE_NONE) ? AndroidShuffleMode.SHUFFLE_MODE_ALL : AndroidShuffleMode.SHUFFLE_MODE_NONE;
            await SendShuffleModeAsync(nextMode);
        }

        public async Task SendSkipToQueueItemAsync(long id)
        {
            ActiveQueueItemId = id;
            string url = string.Format("{0}/api/queue/skip?id={1}", ServerEndpoint, id);
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_SKIP_TO_QUEUE_ITEM, id.ToString());
        }

        public async Task SendVolumeAsync(int volume)
        {
            Volume = volume;
            string url = string.Format("{0}/api/volume?value={1}", ServerEndpoint, volume);
            await HttpGetAsync(url, 1500);
        }

        public async Task SendAdjustVolumeAsync(int direction)
        {
            string url = string.Format("{0}/api/volume/adjust?direction={1}", ServerEndpoint, direction);
            await HttpGetAsync(url, 1500);
        }

        public async Task SendPrepareAsync()
        {
            string url = ServerEndpoint + "/api/prepare";
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PREPARE);
        }

        public async Task SendPrepareFromMediaIdAsync(string mediaId)
        {
            if (string.IsNullOrEmpty(mediaId)) return;
            string url = string.Format("{0}/api/prepare?mediaId={1}", ServerEndpoint, Uri.EscapeDataString(mediaId));
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PREPARE_FROM_MEDIA_ID, mediaId);
        }

        public async Task SendPrepareFromSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            string url = string.Format("{0}/api/prepare?query={1}", ServerEndpoint, Uri.EscapeDataString(query));
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PREPARE_FROM_SEARCH, query);
        }

        public async Task SendPrepareFromUriAsync(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return;
            string url = string.Format("{0}/api/prepare?uri={1}", ServerEndpoint, Uri.EscapeDataString(uri));
            await HttpGetAsync(url, 1500);
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PREPARE_FROM_URI, uri);
        }

        public async Task SendPlayFromMediaIdAsync(string mediaId)
        {
            if (string.IsNullOrEmpty(mediaId)) return;
            string url = string.Format("{0}/api/play?mediaId={1}", ServerEndpoint, Uri.EscapeDataString(mediaId));
            string res = await HttpGetAsync(url, 1500);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                await RefreshTrackAsync();
            }
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PLAY_FROM_MEDIA_ID, mediaId);
        }

        public async Task SendPlayFromSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            string url = string.Format("{0}/api/play?query={1}", ServerEndpoint, Uri.EscapeDataString(query));
            string res = await HttpGetAsync(url, 1500);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                await RefreshTrackAsync();
            }
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PLAY_FROM_SEARCH, query);
        }

        public async Task SendPlayFromUriAsync(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return;
            string url = string.Format("{0}/api/play?uri={1}", ServerEndpoint, Uri.EscapeDataString(uri));
            string res = await HttpGetAsync(url, 1500);
            if (!string.IsNullOrEmpty(res) && IsTrackJson(res))
            {
                ParseAndApplyTrackJson(res);
            }
            else
            {
                await RefreshTrackAsync();
            }
            await SendCustomActionAsync(AndroidCustomActions.ACTION_PLAY_FROM_URI, uri);
        }

        public async Task ExecuteCustomActionAsync(CustomActionModel action, bool nextState)
        {
            if (action == null || string.IsNullOrEmpty(action.Action)) return;

            // 1. Chu trình 3 nấc lặp lại (Repeat: Off -> All -> One)
            if (action.Action == AndroidCustomActions.ACTION_REPEAT || action.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await CycleRepeatModeAsync();
                return;
            }

            // 2. Trộn bài (Shuffle)
            if (action.Action == AndroidCustomActions.ACTION_SHUFFLE || action.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await CycleShuffleModeAsync();
                return;
            }

            // 2. Tua nhanh (Fast Forward)
            if (action.Action == AndroidCustomActions.ACTION_FAST_FORWARD || action.Action.IndexOf("fast_forward", StringComparison.OrdinalIgnoreCase) >= 0 || action.Action.IndexOf("forward", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await SendFastForwardAsync();
                return;
            }

            // 3. Tua lùi (Rewind)
            if (action.Action == AndroidCustomActions.ACTION_REWIND || action.Action.IndexOf("rewind", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await SendRewindAsync();
                return;
            }

            // 4. Dừng hẳn (Stop)
            if (action.Action == AndroidCustomActions.ACTION_STOP || action.Action.IndexOf("stop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await SendStopAsync();
                return;
            }

            // 5. Thumbs Up / Thumbs Down chuẩn AOSP Rating
            if (action.Action == AndroidCustomActions.ACTION_THUMBS_UP || action.Action.IndexOf("thumbs_up", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                action.IsActive = nextState;
                RunOnUI(() => PlaybackStateChanged?.Invoke());
                await SendRatingAsync(AndroidRatingStyle.RATING_THUMB_UP_DOWN, nextState ? 1.0f : 0.0f);
                return;
            }
            if (action.Action == AndroidCustomActions.ACTION_THUMBS_DOWN || action.Action.IndexOf("thumbs_down", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                action.IsActive = nextState;
                RunOnUI(() => PlaybackStateChanged?.Invoke());
                await SendRatingAsync(AndroidRatingStyle.RATING_THUMB_UP_DOWN, nextState ? -1.0f : 0.0f);
                return;
            }

            // 6. Favorite / Shuffle / CustomAction
            action.IsActive = nextState;
            if (action.Action == AndroidCustomActions.ACTION_FAVORITE)
            {
                IsFavorite = nextState;
            }
            else if (action.Action == AndroidCustomActions.ACTION_SHUFFLE)
            {
                IsShuffle = nextState;
            }

            RunOnUI(() => PlaybackStateChanged?.Invoke());

            string val = nextState ? "true" : "false";
            if (action.Action == AndroidCustomActions.ACTION_FAVORITE)
            {
                await SendFavoriteAsync(nextState);
            }
            else if (action.Action == AndroidCustomActions.ACTION_SHUFFLE)
            {
                await SendShuffleAsync(nextState);
            }
            else
            {
                await SendCustomActionAsync(action.Action, val);
            }
        }

        private static Task<string> HttpGetAsync(string url, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                string resolved = ResolveNetworkUrl(url);

                // Thêm timestamp để tránh triệt để WinINet / Silverlight WebClient cache kết quả
                string sep = resolved.Contains("?") ? "&" : "?";
                string cacheBustingUrl = string.Format("{0}{1}_t={2}", resolved, sep, DateTime.UtcNow.Ticks);

                var client = new WebClient();
                client.Headers[HttpRequestHeader.UserAgent] = "TwoUILockScreen/1.0";
                try
                {
                    client.Headers[HttpRequestHeader.CacheControl] = "no-cache, no-store, must-revalidate";
                    client.Headers[HttpRequestHeader.Pragma] = "no-cache";
                }
                catch { }

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(timeoutMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    try { client.CancelAsync(); } catch { }
                    tcs.TrySetResult(null);
                };

                client.DownloadStringCompleted += (s, e) =>
                {
                    timer.Stop();
                    if (e.Error != null || e.Cancelled)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        tcs.TrySetResult(e.Result);
                    }
                };

                Uri requestUri;
                if (!Uri.TryCreate(cacheBustingUrl, UriKind.Absolute, out requestUri))
                {
                    tcs.TrySetResult(null);
                    return tcs.Task;
                }

                timer.Start();
                client.DownloadStringAsync(requestUri);
            }
            catch
            {
                tcs.TrySetResult(null);
            }
            return tcs.Task;
        }

        #region Simple JSON Parsing Helpers

        private static string ExtractString(string json, string key)
        {
            try
            {
                string pattern = "\"" + key + "\":\"";
                int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return "";
                int start = idx + pattern.Length;
                int end = json.IndexOf('"', start);
                if (end < 0) return "";
                string val = json.Substring(start, end - start);
                return UnescapeJson(val);
            }
            catch { return ""; }
        }

        private static long ExtractLong(string json, string key)
        {
            try
            {
                string pattern = "\"" + key + "\":";
                int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return 0;
                int start = idx + pattern.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', ' ', '\r', '\n' }, start);
                if (end < 0) end = json.Length;
                string val = json.Substring(start, end - start).Trim().Trim('"');
                long res;
                if (long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out res))
                {
                    return res;
                }
                return 0;
            }
            catch { return 0; }
        }

        private static bool ExtractBool(string json, string key)
        {
            try
            {
                string pattern = "\"" + key + "\":";
                int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return false;
                int start = idx + pattern.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', ' ', '\r', '\n' }, start);
                if (end < 0) end = json.Length;
                string val = json.Substring(start, end - start).Trim().Trim('"').ToLowerInvariant();
                return val == "true" || val == "1";
            }
            catch { return false; }
        }

        private static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t");
        }

        private static CustomActionModel ParseCustomActionJson(string objJson)
        {
            if (string.IsNullOrEmpty(objJson)) return null;
            string act = ExtractString(objJson, "action");
            if (string.IsNullOrEmpty(act)) act = ExtractString(objJson, "id");
            if (string.IsNullOrEmpty(act)) return null;

            string name = ExtractString(objJson, "name");
            string icon = ExtractString(objJson, "icon");
            bool active = ExtractBool(objJson, "isActive");
            if (!active) active = ExtractBool(objJson, "active");
            string val = ExtractString(objJson, "value");

            return new CustomActionModel
            {
                Action = act,
                Name = name,
                Icon = icon,
                IsActive = active,
                Value = val
            };
        }

        private static QueueItemModel ParseQueueItemJson(string objJson)
        {
            if (string.IsNullOrEmpty(objJson)) return null;
            long id = ExtractLong(objJson, "id");
            string title = ExtractString(objJson, "title");
            if (string.IsNullOrEmpty(title)) title = ExtractString(objJson, "displayTitle");
            string subtitle = ExtractString(objJson, "subtitle");
            if (string.IsNullOrEmpty(subtitle)) subtitle = ExtractString(objJson, "displaySubtitle");
            string desc = ExtractString(objJson, "description");
            string mediaId = ExtractString(objJson, "mediaId");
            string icon = ExtractString(objJson, "iconUri");
            if (string.IsNullOrEmpty(icon)) icon = ExtractString(objJson, "icon");
            string mediaUri = ExtractString(objJson, "mediaUri");
            long btFolderType = ExtractLong(objJson, "btFolderType");
            if (btFolderType <= 0) btFolderType = ExtractLong(objJson, "folderType");

            return new QueueItemModel
            {
                Id = id,
                Title = title ?? "",
                Subtitle = subtitle ?? "",
                Description = desc ?? "",
                MediaId = mediaId ?? "",
                IconUri = icon ?? "",
                MediaUri = mediaUri ?? "",
                BtFolderType = btFolderType
            };
        }

        #endregion

        private static void RunOnUI(Action action)
        {
            if (Deployment.Current != null && Deployment.Current.Dispatcher != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try { action(); } catch { }
                });
            }
            else
            {
                try { action(); } catch { }
            }
        }
    }
}
