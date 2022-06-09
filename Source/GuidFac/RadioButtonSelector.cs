namespace PrivateDeveloperInc.GuidFac;

using System.Windows.Controls;

internal class RadioButtonSelector
{
    private readonly RadioButton[] buttons;
    private int selected;

    public RadioButtonSelector(int selected, params RadioButton[] buttons)
    {
        this.selected = selected;
        this.buttons = buttons;
    }

    public void Next()
    {
        this.selected++;
        if (this.selected >= this.buttons.Length)
        {
            this.selected = 0;
        }

        this.buttons[this.selected].IsChecked = true;
    }

    public void Previous()
    {
        this.selected--;
        if (this.selected < 0)
        {
            this.selected = this.buttons.Length - 1;
        }

        this.buttons[this.selected].IsChecked = true;
    }
}
