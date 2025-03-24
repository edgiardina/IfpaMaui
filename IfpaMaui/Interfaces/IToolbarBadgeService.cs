namespace Ifpa.Interfaces
{
    public interface IToolbarBadgeService
    {
        void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor);
    }
}
