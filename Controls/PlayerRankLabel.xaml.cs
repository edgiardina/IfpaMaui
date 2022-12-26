using PinballApi.Extensions;

namespace Ifpa.Controls
{
	public partial class PlayerRankLabel : Label
	{

        public string Rank
        {
            get
            {
                return (string)base.GetValue(RankProperty);
            }
            set
            {
                if (this.Rank != value)
                    base.SetValue(RankProperty, value);
            }
        }

        public static BindableProperty RankProperty = BindableProperty.Create(
			  propertyName: "Rank",
			  returnType: typeof(string),
			  declaringType: typeof(Label),
			  defaultValue: string.Empty,
			  defaultBindingMode: BindingMode.OneWay,
			  propertyChanged: HandleRankPropertyChanged);

        private static void HandleRankPropertyChanged(
                BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue.ToString() == "0")
            {
                ((PlayerRankLabel)bindable).Text = "Not Ranked";
            }
            else
            {
                ((PlayerRankLabel)bindable).Text = (int.Parse(newValue.ToString())).OrdinalSuffix();
            }
        }

    }
}