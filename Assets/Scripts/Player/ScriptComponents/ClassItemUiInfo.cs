public class ItemUiInfo // So we can define item info to be displayed in the ui when collected
{
    public string name;
    public string tooltip;

    public ItemUiInfo(string name, string tooltip)
    {
        this.name = name;
        this.tooltip = tooltip;
    }
}