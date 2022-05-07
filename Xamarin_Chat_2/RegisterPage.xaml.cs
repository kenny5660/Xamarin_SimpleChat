using Grpc.Core;
using SimpleChatApp.GrpcService;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace Xamarin_Chat_2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterPage : ContentPage
    {
        private string _login;
        private string _password;
        private string _rePassword;


        public ChatService.ChatServiceClient ChatServiceClient { get; set; }
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }
        public string RePassword
        {
            get => _rePassword;
            set
            {
                _rePassword = value;
                OnPropertyChanged();
            }
        }


        public RegisterPage()
        {
            BindingContext = this;
            InitializeComponent();
        }


        private async void RegisterButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Login))
            {
                await DisplayAlert("Alert", "Login is empty", "OK");
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                await DisplayAlert("Alert", "Password is empty", "OK");
                return;
            }
            if (Password != RePassword)
            {
                await DisplayAlert("Alert", "Try to repeat the password again", "OK");
                return;
            }
            try
            {
                ChatServiceClient = new ChatService.ChatServiceClient(new Channel("10.0.2.2:30051", ChannelCredentials.Insecure));
                var ans = await ChatServiceClient.RegisterNewUserAsync(new SimpleChatApp.GrpcService.UserData()
                {
                    Login = Login,
                    PasswordHash = SimpleChatApp.CommonTypes.SHA256.GetStringHash(Password)
                });
                switch (ans.Status)
                {
                    case SimpleChatApp.GrpcService.RegistrationStatus.RegistrationSuccessfull:
                        await DisplayAlert("Alert", "Registration Successfull", "OK");
                        break;

                    case SimpleChatApp.GrpcService.RegistrationStatus.LoginAlreadyExist:
                        await DisplayAlert("Alert", "Login already exist", "OK");
                        break;

                    case SimpleChatApp.GrpcService.RegistrationStatus.BadInput:
                        await DisplayAlert("Alert", "Bad login or password", "OK");
                        break;

                    case SimpleChatApp.GrpcService.RegistrationStatus.RegistratioError:
                        await DisplayAlert("Alert", "Server error", "OK");
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (ans.Status is SimpleChatApp.GrpcService.RegistrationStatus.RegistrationSuccessfull)
                {
                    await Navigation.PushAsync(new LoginPage(ChatServiceClient, Login, Password));
                }
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{System.Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
            }
        }
    }
}