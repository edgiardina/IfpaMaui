namespace Ifpa.Controls
{
    /// <summary>
    /// ContentView that renders with iOS inset grouped table style and Material Cards on Android
    /// </summary>
    public class InsetTableView : ContentView
    {
        public static readonly BindableProperty SectionTitleProperty = 
            BindableProperty.Create(nameof(SectionTitle), typeof(string), typeof(InsetTableView), string.Empty);

        public string SectionTitle
        {
            get => (string)GetValue(SectionTitleProperty);
            set => SetValue(SectionTitleProperty, value);
        }

        public InsetTableView()
        {
            BackgroundColor = Colors.Transparent;
        }
    }
}