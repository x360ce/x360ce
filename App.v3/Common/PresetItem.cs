namespace x360ce.App
{
    public class PresetItem
    {
        public PresetItem(string type, string name)
        {
            Type = type;
            Name = name;
        }
        public string Type { get; set; }
        public string Name { get; set; }
    }
}
