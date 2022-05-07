using Grpc.Core;
using SimpleChatApp.GrpcService;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin_Chat_2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private string _login;
        private string _password;

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
        public LoginPage()
        {
            BindingContext = this;
            InitializeComponent();
        }
        public LoginPage(ChatService.ChatServiceClient chatServiceClient = default,
                             string login = default,
                             string password = default) : this()
        {
            ChatServiceClient = chatServiceClient;
            Login = login ?? Login;
            Password = password ?? Password;
        }
        private async void LoginButtonClicked(object sender, EventArgs e)
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

            try
            {
                ChatServiceClient = new ChatService.ChatServiceClient(new Channel("10.0.2.2:30051", ChannelCredentials.Insecure));
                var ans = await ChatServiceClient.LogInAsync(new AuthorizationData()
                {
                    ClearActiveConnection = true,
                    UserData = new UserData()
                    {
                        Login = Login,
                        PasswordHash = SimpleChatApp.CommonTypes.SHA256.GetStringHash(Password)
                    },
                });
                switch (ans.Status)
                {
                    case SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationSuccessfull:
                        await DisplayAlert("Alert", "Authorization successfull", "OK");
                        break;

                    case SimpleChatApp.GrpcService.AuthorizationStatus.WrongLoginOrPassword:
                        await DisplayAlert("Alert", "Wrong Login Or Password", "OK");
                        break;

                    case SimpleChatApp.GrpcService.AuthorizationStatus.AnotherConnectionActive:
                        await DisplayAlert("Alert", "Another connection active", "OK");
                        break;

                    case SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationError:
                        await DisplayAlert("Alert", "Authorization Error", "OK");
                        break;

                    default:
                        throw new NotImplementedException();
                }


                if (ans.Status is SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationSuccessfull)
                {
                    await Navigation.PushAsync(new ChatPage(ChatServiceClient, Login, Password, ans.Sid.Guid_));
                }
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{System.Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
            }

        }
        private async void RegisterButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}