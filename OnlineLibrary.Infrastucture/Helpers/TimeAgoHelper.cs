namespace OnlineLibrary.Infrastructure.Helpers
{
    public static class TimeAgoHelper
    {
        public static string TimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} mins ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} days ago";

            return dateTime.ToLocalTime().ToString("dd-MM-yyyy");
        }
    }
}
