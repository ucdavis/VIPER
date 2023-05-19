namespace Viper.Classes
{
    public class NavMenuItem
    {
        public string MenuItemText { get; set; } = "";
        public string MenuItemURL { get; set; } = "";
        public bool IsHeader { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
