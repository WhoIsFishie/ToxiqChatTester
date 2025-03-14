using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;

namespace ToxiqChatTester
{
    public partial class MainForm : Form
    {
        private string _jwtToken;
        private HubConnection _chatHubConnection;
        private HubConnection _notificationHubConnection;
        private Guid _currentConversationId;
        private HttpClient _httpClient;
        private string _baseUrl = "https://api.toxiq.xyz"; // Change this to your server URL
        private ChatHubClient _chatHubClient;
        private HubConnection _notificationConnection;
        private AuthenticationHelper _authHelper;
        private string _userId;

        // Models for Conversations and Messages
        private List<Conversation> _conversations = new List<Conversation>();

        public MainForm()
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        private void OnInspectToken(object sender, EventArgs e)
        {
            using (var tokenDebugForm = new TokenDebugForm(_jwtToken))
            {
                tokenDebugForm.ShowDialog();
            }
        }

        private async void OnTestConnection(object sender, EventArgs e)
        {
            try
            {
                UpdateStatus("Testing API connection...");

                // Test status endpoint
                var response = await _httpClient.GetAsync("api/WebSocketStatus");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    UpdateStatus($"API Connection successful: {content}");
                }
                else
                {
                    UpdateStatus($"API Connection failed: {response.StatusCode}");
                }

                // Test hub connections
                if (_chatHubConnection != null)
                {
                    UpdateStatus($"Chat Hub State: {_chatHubConnection.State}");
                    if (_chatHubConnection.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _chatHubConnection.StartAsync();
                            UpdateStatus($"Successfully reconnected to Chat Hub");
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Failed to reconnect to Chat Hub: {ex.Message}");
                        }
                    }
                }

                if (_notificationHubConnection != null)
                {
                    UpdateStatus($"Notification Hub State: {_notificationHubConnection.State}");
                    if (_notificationHubConnection.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _notificationHubConnection.StartAsync();
                            UpdateStatus($"Successfully reconnected to Notification Hub");
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Failed to reconnect to Notification Hub: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection test failed: {ex.Message}");
                MessageBox.Show($"Connection test failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            ShowLoginDialog();
        }

        private void ShowLoginDialog()
        {
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    _jwtToken = loginForm.JwtToken;
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

                    // Print token claims for debugging
                    DumpTokenClaims();

                    // Initialize authentication helper
                    _authHelper = new AuthenticationHelper(_baseUrl, _jwtToken);
                    _userId = _authHelper.GetUserIdFromToken();

                    UpdateStatus($"Extracted user ID from token: {_userId}");

                    if (string.IsNullOrEmpty(_userId))
                    {
                        MessageBox.Show("Could not extract user ID from the JWT token. Authentication may fail.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    InitializeSignalRConnections();
                }
                else
                {
                    Application.Exit();
                }
            }
        }
        private async void InitializeSignalRConnections()
        {
            UpdateStatus("Initializing SignalR connections...");

            try
            {
                // First, validate the token
                bool isTokenValid = await _authHelper.ValidateToken();
                if (!isTokenValid)
                {
                    UpdateStatus("Token validation failed. Please check your JWT token.");
                    MessageBox.Show("The provided JWT token could not be validated. Please check if it's correct and not expired.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Connect to Chat Hub
                var chatConnection = await _authHelper.TryConnectToHub("hubs/chat", UpdateStatus);
                if (chatConnection != null)
                {
                    _chatHubClient = new ChatHubClient(chatConnection, _userId, UpdateStatus);
                    _chatHubClient.SetMessageCallback(message => AddMessageToChat(message));

                    // Try a manual connection status message to verify
                    UpdateStatus("Chat hub connection initialized and ready");
                }
                else
                {
                    UpdateStatus("Failed to connect to chat hub");
                }

                // Connect to Notification Hub
                _notificationConnection = await _authHelper.TryConnectToHub("hubs/notification", UpdateStatus);
                if (_notificationConnection != null)
                {
                    // Set up notification event handlers
                    _notificationConnection.On<string>("Connected", message =>
                    {
                        UpdateStatus($"Notification Hub: {message}");
                    });

                    _notificationConnection.On<Notification>("ReceiveNotification", notification =>
                    {
                        UpdateStatus($"Notification: {notification.Text}");
                    });

                    UpdateStatus("Notification hub connection initialized and ready");
                }
                else
                {
                    UpdateStatus("Failed to connect to notification hub");
                }

                // If we got this far, try to load conversations
                if (_chatHubClient != null)
                {
                    LoadConversations();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing SignalR: {ex.Message}");
                MessageBox.Show($"Error initializing SignalR connections: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private async void LoadConversations()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Chat/conversations");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _conversations = JsonConvert.DeserializeObject<List<Conversation>>(content);

                    lstConversations.Items.Clear();
                    foreach (var conversation in _conversations)
                    {
                        lstConversations.Items.Add(conversation.ConversationName);
                    }

                    UpdateStatus("Conversations loaded");
                }
                else
                {
                    UpdateStatus($"Error loading conversations: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading conversations: {ex.Message}");
            }
        }

        private async void lstConversations_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstConversations.SelectedIndex >= 0)
            {
                try
                {
                    int index = lstConversations.SelectedIndex;
                    _currentConversationId = _conversations[index].Id;

                    // Ensure the chat hub client is available
                    if (_chatHubClient == null)
                    {
                        UpdateStatus("Chat hub client not initialized. Reconnecting...");
                        InitializeSignalRConnections();
                        return;
                    }

                    // Try to join the conversation
                    bool joined = await _chatHubClient.JoinConversation(_currentConversationId);

                    if (joined)
                    {
                        // Load conversation messages
                        await LoadMessages(_currentConversationId);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to join conversation: {_currentConversationId}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error selecting conversation: {ex.Message}");
                    MessageBox.Show($"Error selecting conversation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private async Task LoadMessages(Guid conversationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Chat/conversations/{conversationId}/messages");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var messageResponse = JsonConvert.DeserializeObject<MessageResponse>(content);

                    txtChat.Clear();
                    foreach (var message in messageResponse.Messages)
                    {
                        AddMessageToChat(message);
                    }

                    UpdateStatus($"Loaded {messageResponse.Messages.Count} messages");
                }
                else
                {
                    // Try alternative endpoint
                    response = await _httpClient.GetAsync($"api/conversations/{conversationId}/messages");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var messageResponse = JsonConvert.DeserializeObject<MessageResponse>(content);

                        txtChat.Clear();
                        foreach (var message in messageResponse.Messages)
                        {
                            AddMessageToChat(message);
                        }

                        UpdateStatus($"Loaded {messageResponse.Messages.Count} messages from alternative endpoint");
                    }
                    else
                    {
                        UpdateStatus($"Error loading messages: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading messages: {ex.Message}");
            }
        }


        private void AddMessageToChat(Message message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddMessageToChat(message)));
                return;
            }

            // Find the sender's name
            string senderName = message.SenderID == Guid.Empty ? "System" :
                _conversations
                    .SelectMany(c => c.Users)
                    .FirstOrDefault(u => u.UserId == message.SenderID)?.Name ??
                message.SenderID.ToString();

            string formattedMessage;

            if (message.SenderID == Guid.Empty)
            {
                formattedMessage = $"[SYSTEM] {message.Content}\r\n";
            }
            else
            {
                formattedMessage = $"[{message.Date}] {senderName}: {message.Content}\r\n";
            }

            txtChat.AppendText(formattedMessage);
            txtChat.ScrollToCaret();
        }
        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (_currentConversationId == Guid.Empty)
            {
                MessageBox.Show("Please select a conversation first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                return;
            }

            try
            {
                // Ensure chat hub client is connected
                if (_chatHubClient == null || _chatHubClient.GetConnectionState() != HubConnectionState.Connected)
                {
                    UpdateStatus("Chat hub not connected. Reconnecting...");
                    await _chatHubClient?.ReconnectIfNeeded();
                }

                // First approach: Try sending via SignalR hub
                bool sentViaHub = false;
                if (_chatHubClient != null)
                {
                    sentViaHub = await _chatHubClient.SendMessage(_currentConversationId, txtMessage.Text);
                }

                // Second approach: If hub send fails, try the REST API
                if (!sentViaHub)
                {
                    var message = new SendMessageDto
                    {
                        Content = txtMessage.Text,
                        Type = MessageType.Text
                    };

                    var json = JsonConvert.SerializeObject(message);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"api/Chat/conversations/{_currentConversationId}/messages", content);

                    if (response.IsSuccessStatusCode)
                    {
                        UpdateStatus("Message sent via REST API");
                    }
                    else
                    {
                        // Try alternative endpoint
                        response = await _httpClient.PostAsync($"api/conversations/{_currentConversationId}/messages", content);

                        if (!response.IsSuccessStatusCode)
                        {
                            UpdateStatus($"Error sending message: {response.StatusCode}");
                            return;
                        }
                    }
                }

                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error sending message: {ex.Message}");
                MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DumpTokenClaims()
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(_jwtToken) as JwtSecurityToken;

                if (jsonToken != null)
                {
                    var claims = jsonToken.Claims.ToList();
                    UpdateStatus($"Token contains {claims.Count} claims:");
                    foreach (var claim in claims)
                    {
                        UpdateStatus($"  {claim.Type}: {claim.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error parsing token: {ex.Message}");
            }
        }


        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateStatus(message)));
                return;
            }

            lblStatus.Text = message;
        }

        private async void btnNewConversation_Click(object sender, EventArgs e)
        {
            using (var newConversationForm = new NewConversationForm(_httpClient))
            {
                if (newConversationForm.ShowDialog() == DialogResult.OK)
                {
                    LoadConversations();
                }
            }
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_chatHubConnection != null)
            {
                await _chatHubConnection.DisposeAsync();
            }

            if (_notificationHubConnection != null)
            {
                await _notificationHubConnection.DisposeAsync();
            }
        }

        private System.Windows.Forms.ListBox lstConversations;
        private System.Windows.Forms.TextBox txtChat;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnNewConversation;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
    }

    public partial class LoginForm : Form
    {
        public string JwtToken { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtToken.Text))
            {
                MessageBox.Show("Please enter a JWT token");
                return;
            }

            JwtToken = txtToken.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void InitializeComponent()
        {
            this.txtToken = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtToken
            // 
            this.txtToken.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtToken.Location = new System.Drawing.Point(12, 29);
            this.txtToken.Multiline = true;
            this.txtToken.Name = "txtToken";
            this.txtToken.Size = new System.Drawing.Size(426, 83);
            this.txtToken.TabIndex = 0;
            // 
            // btnLogin
            // 
            this.btnLogin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogin.Location = new System.Drawing.Point(282, 119);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(75, 23);
            this.btnLogin.TabIndex = 1;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(363, 119);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "JWT Token";
            // 
            // LoginForm
            // 
            this.AcceptButton = this.btnLogin;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(450, 154);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.txtToken);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Login";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtToken;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
    }

    public partial class NewConversationForm : Form
    {
        private HttpClient _httpClient;

        public NewConversationForm(HttpClient httpClient)
        {
            _httpClient = httpClient;
            InitializeComponent();
        }

        private async void btnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserId.Text) && chkIsDirectMessage.Checked)
            {
                MessageBox.Show("Please enter a user ID for direct message");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtGroupName.Text) && !chkIsDirectMessage.Checked)
            {
                MessageBox.Show("Please enter a group name");
                return;
            }

            try
            {
                if (chkIsDirectMessage.Checked)
                {
                    // Create direct message conversation
                    Guid userId;
                    if (!Guid.TryParse(txtUserId.Text.Trim(), out userId))
                    {
                        MessageBox.Show("Invalid user ID format. Please use a valid GUID.");
                        return;
                    }

                    var response = await _httpClient.PostAsync($"api/Chat/conversations/direct/{userId}", null);
                    if (response.IsSuccessStatusCode)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Error creating direct message: {response.StatusCode}\r\n{await response.Content.ReadAsStringAsync()}");
                    }
                }
                else
                {
                    // Create group conversation
                    var participantIds = new List<Guid>();

                    // Parse comma-separated list of user IDs
                    if (!string.IsNullOrWhiteSpace(txtParticipants.Text))
                    {
                        var userIdStrings = txtParticipants.Text.Split(',');
                        foreach (var userIdString in userIdStrings)
                        {
                            Guid userId;
                            if (Guid.TryParse(userIdString.Trim(), out userId))
                            {
                                participantIds.Add(userId);
                            }
                        }
                    }

                    var createGroupDto = new
                    {
                        Name = txtGroupName.Text.Trim(),
                        ParticipantIds = participantIds
                    };

                    var json = JsonConvert.SerializeObject(createGroupDto);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync("api/Chat/conversations/group", content);
                    if (response.IsSuccessStatusCode)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Error creating group: {response.StatusCode}\r\n{await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating conversation: {ex.Message}");
            }
        }

        private void chkIsDirectMessage_CheckedChanged(object sender, EventArgs e)
        {
            txtUserId.Enabled = chkIsDirectMessage.Checked;
            txtGroupName.Enabled = !chkIsDirectMessage.Checked;
            txtParticipants.Enabled = !chkIsDirectMessage.Checked;
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.chkIsDirectMessage = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtGroupName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtParticipants = new System.Windows.Forms.TextBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "User ID:";
            // 
            // txtUserId
            // 
            this.txtUserId.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserId.Location = new System.Drawing.Point(126, 38);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(278, 23);
            this.txtUserId.TabIndex = 1;
            // 
            // chkIsDirectMessage
            // 
            this.chkIsDirectMessage.AutoSize = true;
            this.chkIsDirectMessage.Checked = true;
            this.chkIsDirectMessage.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIsDirectMessage.Location = new System.Drawing.Point(12, 12);
            this.chkIsDirectMessage.Name = "chkIsDirectMessage";
            this.chkIsDirectMessage.Size = new System.Drawing.Size(109, 19);
            this.chkIsDirectMessage.TabIndex = 0;
            this.chkIsDirectMessage.Text = "Direct Message";
            this.chkIsDirectMessage.UseVisualStyleBackColor = true;
            this.chkIsDirectMessage.CheckedChanged += new System.EventHandler(this.chkIsDirectMessage_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Group Name:";
            // 
            // txtGroupName
            // 
            this.txtGroupName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGroupName.Enabled = false;
            this.txtGroupName.Location = new System.Drawing.Point(126, 67);
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(278, 23);
            this.txtGroupName.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 99);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(108, 15);
            this.label3.TabIndex = 5;
            this.label3.Text = "Participants (GUIDs):";
            // 
            // txtParticipants
            // 
            this.txtParticipants.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtParticipants.Enabled = false;
            this.txtParticipants.Location = new System.Drawing.Point(126, 96);
            this.txtParticipants.Name = "txtParticipants";
            this.txtParticipants.Size = new System.Drawing.Size(278, 23);
            this.txtParticipants.TabIndex = 3;
            // 
            // btnCreate
            // 
            this.btnCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreate.Location = new System.Drawing.Point(248, 136);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(75, 23);
            this.btnCreate.TabIndex = 4;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(329, 136);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // NewConversationForm
            // 
            this.AcceptButton = this.btnCreate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(416, 171);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.txtParticipants);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtGroupName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.chkIsDirectMessage);
            this.Controls.Add(this.txtUserId);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewConversationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Conversation";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.CheckBox chkIsDirectMessage;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtGroupName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtParticipants;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnCancel;
    }

    // Model classes to match the API response formats
    public class Conversation
    {
        public Guid Id { get; set; }
        public string ConversationName { get; set; }
        public DateTime ChatStarted { get; set; }
        public List<ConversationUser> Users { get; set; } = new List<ConversationUser>();
        public bool IsGroup { get; set; }
    }

    public class ConversationUser
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public DateTime JoinedDate { get; set; }
    }

    public class Message
    {
        public Guid Id { get; set; }
        public Guid SenderID { get; set; }
        public Guid RecipientID { get; set; }
        public DateTime Date { get; set; }
        public Guid? ReplyTo { get; set; }
        public MessageType Type { get; set; }
        public string Content { get; set; }
    }

    public class SendMessageDto
    {
        public string Content { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public Guid? ReplyToMessageId { get; set; }
    }

    public class MessageResponse
    {
        public List<Message> Messages { get; set; }
        public int TotalPages { get; set; }
        public long TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Text { get; set; }
        public string Caption { get; set; }
        public string Url { get; set; }
        public string TeleUrl { get; set; }
        public string AppUrl { get; set; }
        public int Type { get; set; }
        public DateTime? Date { get; set; }
    }

    public enum MessageType
    {
        admin_action = 0,
        Sticker = 1,
        Text = 2,
        Image = 3,
        Comment = 4,
        Post = 5,
        Audio = 6,
        Video = 7
    }
}