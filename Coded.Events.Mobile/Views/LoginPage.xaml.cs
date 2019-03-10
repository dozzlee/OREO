using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Coded.Events.Mobile.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            BackgroundColor = Models.Constants.BackgroundColor;
            Lbl_Username.TextColor = Models.Constants.MainTextColor;
            Lbl_Password.TextColor = Models.Constants.MainTextColor;
            ActivitySpinner.IsVisible = false;
            LoginIcon.HeightRequest = Models.Constants.LoginIconHeight;

            Entry_Username.Completed += (s, e) => Entry_Password.Focus();
            Entry_Password.Completed += (s, e) => SignInProcedure(s, e);
        }

        void SignInProcedure(object sender, EventArgs e)
        {
            Models.User user = new Models.User(Entry_Username.Text, Entry_Password.Text);
            if(user.CheckInformation())
            {
                DisplayAlert("Login","Login Success", "Okay");
            }
            else
            {
                DisplayAlert("Login", "Login Not Correct, empyty username or password", "Okay");
            }
        }
    }
}
