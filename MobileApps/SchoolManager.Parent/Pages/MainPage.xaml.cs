using SchoolManager.Parent.Models;
using SchoolManager.Parent.PageModels;

namespace SchoolManager.Parent.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}