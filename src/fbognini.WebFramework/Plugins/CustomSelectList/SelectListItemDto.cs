namespace fbognini.WebFramework.Plugins.CustomSelectList
{
    public class SelectListItemDto
    {
        public SelectListItemDto(
            string value
            , string text)
        {
            Value = value;
            Text = text;
        }
        public string Value { get; set; }

        public string Text { get; set; }

    }
}
