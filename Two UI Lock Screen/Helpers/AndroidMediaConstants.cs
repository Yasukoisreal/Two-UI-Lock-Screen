using System;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Các mã trạng thái phát nhạc chuẩn từ Android Open Source Project (AOSP) - android.media.session.PlaybackState
    /// </summary>
    public static class AndroidPlaybackState
    {
        public const int STATE_NONE = 0;
        public const int STATE_STOPPED = 1;
        public const int STATE_PAUSED = 2;
        public const int STATE_PLAYING = 3;
        public const int STATE_FAST_FORWARDING = 4;
        public const int STATE_REWINDING = 5;
        public const int STATE_BUFFERING = 6;
        public const int STATE_ERROR = 7;
        public const int STATE_CONNECTING = 8;
        public const int STATE_SKIPPING_TO_PREVIOUS = 9;
        public const int STATE_SKIPPING_TO_NEXT = 10;
        public const int STATE_SKIPPING_TO_QUEUE_ITEM = 11;

        public const long PLAYBACK_POSITION_UNKNOWN = -1L;

        public static string GetStateName(int state)
        {
            switch (state)
            {
                case STATE_NONE: return "NONE";
                case STATE_STOPPED: return "STOPPED";
                case STATE_PAUSED: return "PAUSED";
                case STATE_PLAYING: return "PLAYING";
                case STATE_FAST_FORWARDING: return "FAST_FORWARDING";
                case STATE_REWINDING: return "REWINDING";
                case STATE_BUFFERING: return "BUFFERING";
                case STATE_ERROR: return "ERROR";
                case STATE_CONNECTING: return "CONNECTING";
                case STATE_SKIPPING_TO_PREVIOUS: return "SKIPPING_TO_PREVIOUS";
                case STATE_SKIPPING_TO_NEXT: return "SKIPPING_TO_NEXT";
                case STATE_SKIPPING_TO_QUEUE_ITEM: return "SKIPPING_TO_QUEUE_ITEM";
                default: return "UNKNOWN";
            }
        }

        public static bool IsActive(int state)
        {
            switch (state)
            {
                case STATE_BUFFERING:
                case STATE_CONNECTING:
                case STATE_FAST_FORWARDING:
                case STATE_PLAYING:
                case STATE_REWINDING:
                case STATE_SKIPPING_TO_PREVIOUS:
                case STATE_SKIPPING_TO_NEXT:
                case STATE_SKIPPING_TO_QUEUE_ITEM:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Cờ bitmask năng lực điều khiển chuẩn từ Android - android.media.session.PlaybackState.Actions
    /// </summary>
    public static class AndroidPlaybackActions
    {
        public const long ACTION_STOP = 1L << 0;
        public const long ACTION_PAUSE = 1L << 1;
        public const long ACTION_PLAY = 1L << 2;
        public const long ACTION_REWIND = 1L << 3;
        public const long ACTION_SKIP_TO_PREVIOUS = 1L << 4;
        public const long ACTION_SKIP_TO_NEXT = 1L << 5;
        public const long ACTION_FAST_FORWARD = 1L << 6;
        public const long ACTION_SET_RATING = 1L << 7;
        public const long ACTION_SEEK_TO = 1L << 8;
        public const long ACTION_PLAY_PAUSE = 1L << 9;
        public const long ACTION_PLAY_FROM_MEDIA_ID = 1L << 10;
        public const long ACTION_PLAY_FROM_SEARCH = 1L << 11;
        public const long ACTION_SKIP_TO_QUEUE_ITEM = 1L << 12;
        public const long ACTION_PLAY_FROM_URI = 1L << 13;
        public const long ACTION_PREPARE = 1L << 14;
        public const long ACTION_PREPARE_FROM_MEDIA_ID = 1L << 15;
        public const long ACTION_PREPARE_FROM_SEARCH = 1L << 16;
        public const long ACTION_PREPARE_FROM_URI = 1L << 17;
        public const long ACTION_SET_PLAYBACK_SPEED = 1L << 22;

        public const long DEFAULT_ACTIONS = ACTION_PLAY | ACTION_PAUSE | ACTION_PLAY_PAUSE |
                                            ACTION_SKIP_TO_PREVIOUS | ACTION_SKIP_TO_NEXT |
                                            ACTION_SEEK_TO;

        public static bool HasAction(long actionsBitmask, long action)
        {
            return (actionsBitmask & action) != 0;
        }
    }

    /// <summary>
    /// Các khóa dữ liệu bài hát chuẩn từ Android - android.media.MediaMetadata
    /// </summary>
    public static class AndroidMediaMetadata
    {
        public const string METADATA_KEY_TITLE = "android.media.metadata.TITLE";
        public const string METADATA_KEY_ARTIST = "android.media.metadata.ARTIST";
        public const string METADATA_KEY_ALBUM = "android.media.metadata.ALBUM";
        public const string METADATA_KEY_AUTHOR = "android.media.metadata.AUTHOR";
        public const string METADATA_KEY_WRITER = "android.media.metadata.WRITER";
        public const string METADATA_KEY_COMPOSER = "android.media.metadata.COMPOSER";
        public const string METADATA_KEY_DURATION = "android.media.metadata.DURATION";
        public const string METADATA_KEY_ALBUM_ART_URI = "android.media.metadata.ALBUM_ART_URI";
        public const string METADATA_KEY_ART_URI = "android.media.metadata.ART_URI";
        public const string METADATA_KEY_DISPLAY_TITLE = "android.media.metadata.DISPLAY_TITLE";
        public const string METADATA_KEY_DISPLAY_SUBTITLE = "android.media.metadata.DISPLAY_SUBTITLE";
        public const string METADATA_KEY_DISPLAY_DESCRIPTION = "android.media.metadata.DISPLAY_DESCRIPTION";
        public const string METADATA_KEY_DISPLAY_ICON_URI = "android.media.metadata.DISPLAY_ICON_URI";
        public const string METADATA_KEY_MEDIA_ID = "android.media.metadata.MEDIA_ID";
        public const string METADATA_KEY_MEDIA_URI = "android.media.metadata.MEDIA_URI";
        public const string METADATA_KEY_USER_RATING = "android.media.metadata.USER_RATING";
        public const string METADATA_KEY_RATING = "android.media.metadata.RATING";
        public const string METADATA_KEY_ERROR_MESSAGE = "android.media.metadata.ERROR_MESSAGE";
        public const string METADATA_KEY_BUFFERED_POSITION = "android.media.metadata.BUFFERED_POSITION";
        public const string METADATA_KEY_AUDIO_OUTPUT = "android.media.metadata.AUDIO_OUTPUT";
        public const string METADATA_KEY_REPEAT_MODE = "android.media.metadata.REPEAT_MODE";
        public const string METADATA_KEY_GENRE = "android.media.metadata.GENRE";
        public const string METADATA_KEY_YEAR = "android.media.metadata.YEAR";
        public const string METADATA_KEY_TRACK_NUMBER = "android.media.metadata.TRACK_NUMBER";
        public const string METADATA_KEY_NUM_TRACKS = "android.media.metadata.NUM_TRACKS";
        public const string METADATA_KEY_ALBUM_ARTIST = "android.media.metadata.ALBUM_ARTIST";
        public const string METADATA_KEY_DISC_NUMBER = "android.media.metadata.DISC_NUMBER";
        public const string METADATA_KEY_QUEUE_TITLE = "android.media.metadata.QUEUE_TITLE";
        public const string METADATA_KEY_COMPILATION = "android.media.metadata.COMPILATION";
        public const string METADATA_KEY_DATE = "android.media.metadata.DATE";
        public const string METADATA_KEY_ART = "android.media.metadata.ART";
        public const string METADATA_KEY_ALBUM_ART = "android.media.metadata.ALBUM_ART";
        public const string METADATA_KEY_DISPLAY_ICON = "android.media.metadata.DISPLAY_ICON";
        public const string METADATA_KEY_BT_FOLDER_TYPE = "android.media.metadata.BT_FOLDER_TYPE";
    }

    /// <summary>
    /// Các cờ phiên làm việc chuẩn AOSP - android.media.session.MediaSession
    /// </summary>
    public static class AndroidSessionFlags
    {
        public const int FLAG_HANDLES_MEDIA_BUTTONS = 1 << 0;
        public const int FLAG_HANDLES_TRANSPORT_CONTROLS = 1 << 1;
        public const int FLAG_EXCLUSIVE_GLOBAL_PRIORITY = 1 << 16;
    }

    /// <summary>
    /// Các hằng số mở rộng mô tả media chuẩn AOSP - android.media.MediaDescription
    /// </summary>
    public static class AndroidMediaDescription
    {
        public const string EXTRA_BT_FOLDER_TYPE = "android.media.extra.BT_FOLDER_TYPE";

        public const long BT_FOLDER_TYPE_MIXED = 0;
        public const long BT_FOLDER_TYPE_TITLES = 1;
        public const long BT_FOLDER_TYPE_ALBUMS = 2;
        public const long BT_FOLDER_TYPE_ARTISTS = 3;
        public const long BT_FOLDER_TYPE_GENRES = 4;
        public const long BT_FOLDER_TYPE_PLAYLISTS = 5;
        public const long BT_FOLDER_TYPE_YEARS = 6;
    }

    /// <summary>
    /// Các loại thư mục duyệt media Bluetooth AVRCP 1.5 chuẩn AOSP - android.media.MediaDescription
    /// </summary>
    public static class AndroidBtFolderType
    {
        public const long BT_FOLDER_TYPE_MIXED = 0;
        public const long BT_FOLDER_TYPE_TITLES = 1;
        public const long BT_FOLDER_TYPE_ALBUMS = 2;
        public const long BT_FOLDER_TYPE_ARTISTS = 3;
        public const long BT_FOLDER_TYPE_GENRES = 4;
        public const long BT_FOLDER_TYPE_PLAYLISTS = 5;
        public const long BT_FOLDER_TYPE_YEARS = 6;
    }

    /// <summary>
    /// Mô hình mục trong hàng đợi phát chuẩn AOSP - android.media.session.MediaSession.QueueItem
    /// </summary>
    public class QueueItemModel
    {
        public const long UNKNOWN_ID = -1L;

        public long Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string MediaId { get; set; }
        public string IconUri { get; set; }
        public string MediaUri { get; set; }
        public long BtFolderType { get; set; }

        public QueueItemModel()
        {
            Id = UNKNOWN_ID;
            Title = "";
            Subtitle = "";
            Description = "";
            MediaId = "";
            IconUri = "";
            MediaUri = "";
            BtFolderType = AndroidBtFolderType.BT_FOLDER_TYPE_MIXED;
        }
    }

    /// <summary>
    /// Các hành động mở rộng (Custom Actions) chuẩn tương thích Android - PlaybackState.CustomAction
    /// </summary>
    public static class AndroidCustomActions
    {
        public const string ACTION_FAVORITE = "android.media.action.FAVORITE";
        public const string ACTION_SHUFFLE = "android.media.action.SHUFFLE";
        public const string ACTION_REPEAT = "android.media.action.REPEAT";
        public const string ACTION_REWIND = "android.media.action.REWIND";
        public const string ACTION_FAST_FORWARD = "android.media.action.FAST_FORWARD";
        public const string ACTION_THUMBS_UP = "android.media.action.THUMBS_UP";
        public const string ACTION_THUMBS_DOWN = "android.media.action.THUMBS_DOWN";
        public const string ACTION_STOP = "android.media.action.STOP";
        public const string ACTION_RATING = "android.media.action.RATING";
        public const string ACTION_PREPARE = "android.media.action.PREPARE";
        public const string ACTION_PREPARE_FROM_MEDIA_ID = "android.media.action.PREPARE_FROM_MEDIA_ID";
        public const string ACTION_PREPARE_FROM_SEARCH = "android.media.action.PREPARE_FROM_SEARCH";
        public const string ACTION_PREPARE_FROM_URI = "android.media.action.PREPARE_FROM_URI";
        public const string ACTION_PLAY_FROM_MEDIA_ID = "android.media.action.PLAY_FROM_MEDIA_ID";
        public const string ACTION_PLAY_FROM_SEARCH = "android.media.action.PLAY_FROM_SEARCH";
        public const string ACTION_PLAY_FROM_URI = "android.media.action.PLAY_FROM_URI";
        public const string ACTION_SKIP_TO_QUEUE_ITEM = "android.media.action.SKIP_TO_QUEUE_ITEM";
    }

    /// <summary>
    /// Mô hình nút tùy biến tương đương PlaybackState.CustomAction của Android AOSP
    /// </summary>
    public class CustomActionModel
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
        public string Value { get; set; }

        public CustomActionModel()
        {
            Action = "";
            Name = "";
            Icon = "";
            IsActive = false;
            Value = "";
        }
    }

    /// <summary>
    /// Chế độ lặp lại (Repeat Mode)
    /// </summary>
    public static class AndroidRepeatMode
    {
        public const int REPEAT_MODE_NONE = 0;
        public const int REPEAT_MODE_ONE = 1;
        public const int REPEAT_MODE_ALL = 2;
        public const int REPEAT_MODE_GROUP = 3;
    }

    /// <summary>
    /// Chế độ trộn bài (Shuffle Mode)
    /// </summary>
    public static class AndroidShuffleMode
    {
        public const int SHUFFLE_MODE_NONE = 0;
        public const int SHUFFLE_MODE_ALL = 1;
        public const int SHUFFLE_MODE_GROUP = 2;
    }

    /// <summary>
    /// Các kiểu đánh giá nội dung chuẩn từ Android - android.media.Rating
    /// </summary>
    public static class AndroidRatingStyle
    {
        public const int RATING_NONE = 0;
        public const int RATING_HEART = 1;
        public const int RATING_THUMB_UP_DOWN = 2;
        public const int RATING_3_STARS = 3;
        public const int RATING_4_STARS = 4;
        public const int RATING_5_STARS = 5;
        public const int RATING_PERCENTAGE = 6;

        public const float RATING_NOT_RATED = -1.0f;
    }

    /// <summary>
    /// Loại thiết bị phát lại âm thanh chuẩn AOSP - android.media.session.MediaController.PlaybackInfo
    /// </summary>
    public static class AndroidPlaybackType
    {
        public const int PLAYBACK_TYPE_LOCAL = 1;
        public const int PLAYBACK_TYPE_REMOTE = 2;
    }

    /// <summary>
    /// Kiểu điều khiển âm lượng chuẩn AOSP - android.media.VolumeProvider
    /// </summary>
    public static class AndroidVolumeControl
    {
        public const int VOLUME_CONTROL_FIXED = 0;
        public const int VOLUME_CONTROL_RELATIVE = 1;
        public const int VOLUME_CONTROL_ABSOLUTE = 2;
    }

    /// <summary>
    /// Các hướng điều chỉnh âm lượng chuẩn AOSP - android.media.AudioManager / MediaController
    /// </summary>
    public static class AndroidVolumeAdjust
    {
        public const int ADJUST_RAISE = 1;
        public const int ADJUST_LOWER = -1;
        public const int ADJUST_SAME = 0;
        public const int ADJUST_MUTE = -100;
        public const int ADJUST_UNMUTE = 100;
        public const int ADJUST_TOGGLE_MUTE = 101;
    }

    /// <summary>
    /// Mã kết quả xử lý sự kiện phím media chuẩn AOSP - android.media.session.MediaSessionManager
    /// </summary>
    public static class AndroidMediaKeyResult
    {
        public const int RESULT_MEDIA_KEY_NOT_HANDLED = 0;
        public const int RESULT_MEDIA_KEY_HANDLED = 1;
    }
}

