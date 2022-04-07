using System;
using System.Globalization;

namespace fbognini.WebFramework.Plugins.FullCalendar
{
    public class FcParameters
    {
        private string start;
        private string end;

        public string Start
        {
            get => start;
            set
            {
                start = value;
                StartDate = DateTime.ParseExact(value, "yyyy-MM-ddThh:mm:sszzz", CultureInfo.InvariantCulture);
            }
        }
        public string End
        {
            get => end;
            set
            {
                end = value;
                EndDate = DateTime.ParseExact(value, "yyyy-MM-ddThh:mm:sszzz", CultureInfo.InvariantCulture).AddSeconds(-1);
            }
        }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
    }

    public class FcEventResult
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string ResourceId { get; set; }
        public string Color { get; set; }
        public object ExtendedProps { get; set; }
    }

    public class FcResourceResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}
