using Microsoft.AspNetCore.SignalR.Client;

namespace ToxiqChatTester
{
    public class ChatHubClient
    {
        private HubConnection _hubConnection;
        private Action<string> _statusCallback;
        private Action<Message> _messageCallback;
        private string _userId;

        public ChatHubClient(HubConnection connection, string userId, Action<string> statusCallback)
        {
            _hubConnection = connection;
            _statusCallback = statusCallback;
            _userId = userId;

            // Set up event handlers
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // Common message handler
            _hubConnection.On<Message>("ReceiveMessage", message =>
            {
                _messageCallback?.Invoke(message);
            });

            // System message handler
            _hubConnection.On<string>("SystemMessage", message =>
            {
                _statusCallback?.Invoke($"[SYSTEM] {message}");
            });

            // Connection status handler
            _hubConnection.On<string>("ChatConnected", message =>
            {
                _statusCallback?.Invoke($"Chat Hub: {message}");
            });

            // Joined conversation handler
            _hubConnection.On<Guid>("JoinedConversation", conversationId =>
            {
                _statusCallback?.Invoke($"Joined conversation: {conversationId}");
            });

            // Reconnection handlers
            _hubConnection.Reconnecting += error =>
            {
                _statusCallback?.Invoke($"Reconnecting to chat hub... Reason: {error?.Message ?? "Connection lost"}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _statusCallback?.Invoke($"Reconnected to chat hub. ConnectionId: {connectionId}");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                if (error != null)
                {
                    _statusCallback?.Invoke($"Connection closed with error: {error.Message}");
                }
                else
                {
                    _statusCallback?.Invoke("Connection closed");
                }
                return Task.CompletedTask;
            };
        }

        public void SetMessageCallback(Action<Message> callback)
        {
            _messageCallback = callback;
        }

        public async Task<bool> JoinConversation(Guid conversationId)
        {
            try
            {
                _statusCallback?.Invoke($"Attempting to join conversation: {conversationId}");

                // Ensure connection is active
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    _statusCallback?.Invoke("Connection is not active. Reconnecting...");
                    await _hubConnection.StartAsync();
                }

                // Try different approaches for joining
                try
                {
                    // Approach 1: Standard method with Guid
                    await _hubConnection.InvokeAsync("JoinConversation", conversationId);
                    _statusCallback?.Invoke($"Successfully joined conversation: {conversationId}");
                    return true;
                }
                catch (Exception ex1)
                {
                    _statusCallback?.Invoke($"Approach 1 failed: {ex1.Message}");

                    try
                    {
                        // Approach 2: Try with string GUID
                        await _hubConnection.InvokeAsync("JoinConversation", conversationId.ToString());
                        _statusCallback?.Invoke($"Successfully joined conversation (string approach): {conversationId}");
                        return true;
                    }
                    catch (Exception ex2)
                    {
                        _statusCallback?.Invoke($"Approach 2 failed: {ex2.Message}");

                        try
                        {
                            // Approach 3: Try with user ID and conversation ID
                            if (!string.IsNullOrEmpty(_userId))
                            {
                                await _hubConnection.InvokeAsync("JoinConversation", _userId, conversationId.ToString());
                                _statusCallback?.Invoke($"Successfully joined conversation (with user ID): {conversationId}");
                                return true;
                            }
                        }
                        catch (Exception ex3)
                        {
                            _statusCallback?.Invoke($"Approach 3 failed: {ex3.Message}");
                            throw new AggregateException("All join approaches failed", new[] { ex1, ex2, ex3 });
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _statusCallback?.Invoke($"Error joining conversation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMessage(Guid conversationId, string message)
        {
            try
            {
                // Attempt to invoke the SendMessage method on the hub
                await _hubConnection.InvokeAsync("SendMessage", conversationId, message);
                return true;
            }
            catch (Exception ex)
            {
                _statusCallback?.Invoke($"Error sending message: {ex.Message}");
                return false;
            }
        }

        public HubConnectionState GetConnectionState()
        {
            return _hubConnection.State;
        }

        public async Task<bool> ReconnectIfNeeded()
        {
            if (_hubConnection.State != HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _statusCallback?.Invoke($"Reconnection failed: {ex.Message}");
                    return false;
                }
            }
            return true;
        }
    }
}