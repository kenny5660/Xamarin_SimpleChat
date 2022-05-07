﻿
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SimpleChatApp.CommonTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static SimpleChatApp.GrpcService.ChatService;

namespace Xamarin_Chat_2
{
    public class MessageDataWithColor
    {
        public MessageData messageData { get; set;}
        public Color UserColor { get; set; }
        public MessageDataWithColor(MessageData _messageData)
        {
            messageData = _messageData;
            UserColor = Color.FromUint(((uint)messageData.UserLogin.GetHashCode()) | 0xFF222222);
        }

    }
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatPage : ContentPage
    {
        public ObservableCollection<MessageDataWithColor> Messages { get; set; }

        private string _login;
        private string _password;
        private string _sid;

        public ChatServiceClient ChatServiceClient { get; set; }

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

        public string Sid
        {
            get => _sid;
            set
            {
                _sid = value;
                OnPropertyChanged();
            }
        }

        public ChatPage()
        {
            InitializeComponent();
            Messages = new ObservableCollection<MessageDataWithColor>();
            BindingContext = this;
            Messages.CollectionChanged += MessagesCollectionChanged;
        }

        public ChatPage(ChatServiceClient chatServiceClient = default,
                                string login = default,
                                string password = default,
                                string sid = default) : this()
        {

            ChatServiceClient = chatServiceClient;
            Login = login ?? Login;
            Password = password ?? Password;
            Sid = sid ?? Sid;
             LoadLogs().ContinueWith(Subcribe, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void MessagesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MessagesList.ScrollTo(Messages[Messages.Count - 1], ScrollToPosition.End, true);
        }

        private async Task LoadLogs()
        {
            try
            {
                var ans = await ChatServiceClient.GetLogsAsync(new SimpleChatApp.GrpcService.TimeIntervalRequest()
                {
                    Sid = new SimpleChatApp.GrpcService.Guid() { Guid_ = Sid },
                    StartTime = Timestamp.FromDateTime(DateTime.MinValue.ToUniversalTime()),
                    EndTime = Timestamp.FromDateTime(DateTime.MaxValue.ToUniversalTime())
                });

                foreach (var message in ans.Logs)
                {
                    Messages.Add(new MessageDataWithColor(message.Convert()));
                    await Task.Yield();
                }
                await Task.Yield();
                if (Messages.Count > 0)
                    MessagesList.ScrollTo(Messages[Messages.Count - 1], ScrollToPosition.End, true);
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
                await Navigation.PopAsync();
            }
        }

        public async Task Subcribe(Task task)
        {
            IAsyncStreamReader<SimpleChatApp.GrpcService.Messages> stream;
            try
            {
                stream = ChatServiceClient.Subscribe(new SimpleChatApp.GrpcService.Guid() { Guid_ = Sid }).ResponseStream;
                while (await stream.MoveNext())
                {
                    var displayTask = Task.CompletedTask;
                    switch (stream.Current.ActionStatus)
                    {
                        case SimpleChatApp.GrpcService.ActionStatus.Allowed:
                            displayTask = Task.CompletedTask;
                            break;

                        case SimpleChatApp.GrpcService.ActionStatus.Forbidden:
                            displayTask = DisplayAlert("Alert", $"Forbidden action!", "Ok");
                            break;

                        case SimpleChatApp.GrpcService.ActionStatus.WrongSid:
                            displayTask = DisplayAlert("Alert", $"Wrong sid!", "Ok");
                            break;

                        case SimpleChatApp.GrpcService.ActionStatus.ServerError:
                            displayTask = DisplayAlert("Alert", $"Server error!", "Ok");
                            break;
                    }
                    await displayTask;
                    if (stream.Current.ActionStatus == SimpleChatApp.GrpcService.ActionStatus.Allowed)
                    {
                        foreach (var messageData in stream.Current.Logs)
                        {
                             Messages.Add(new MessageDataWithColor(messageData.Convert()));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
                await Unsubcribe();
                
            }
            finally
            {
                await DisplayAlert("Alert", $"Server close connetction", "OK");
                await Navigation.PopAsync();
            }
        }

        private async Task Unsubcribe()
        {
            try
            {
                await ChatServiceClient.UnsubscribeAsync(new SimpleChatApp.GrpcService.Guid() { Guid_ = Sid });
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
                await Navigation.PopAsync();
            }
        }

        private async void SendButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(MessageTextEditor.Text))
                {
                    return;
                }

                var text = MessageTextEditor.Text.Replace("\r", "\r\n");
                MessageTextEditor.Text = string.Empty;
                var ans = await ChatServiceClient.WriteAsync(new SimpleChatApp.GrpcService.OutgoingMessage()
                {
                    Sid = new SimpleChatApp.GrpcService.Guid() { Guid_ = Sid },
                    Text = text
                });

                var task = Task.CompletedTask;
                switch (ans.ActionStatus)
                {
                    case SimpleChatApp.GrpcService.ActionStatus.Allowed:
                        task = Task.CompletedTask;
                        break;

                    case SimpleChatApp.GrpcService.ActionStatus.Forbidden:
                        task = DisplayAlert("Alert", $"Forbidden action!", "Ok");
                        break;

                    case SimpleChatApp.GrpcService.ActionStatus.WrongSid:
                        task = DisplayAlert("Alert", $"Wrong sid!", "Ok");
                        break;

                    case SimpleChatApp.GrpcService.ActionStatus.ServerError:
                        task = DisplayAlert("Alert", $"Server error!", "Ok");
                        break;
                }
                await task;
            }
            catch (RpcException ex)
            {
                await DisplayAlert("Error", $"Status: {ex.Status.StatusCode}{Environment.NewLine}Detail: {ex.Status.Detail}", "OK");
                await Navigation.PopAsync();
            }
        }
    }
}