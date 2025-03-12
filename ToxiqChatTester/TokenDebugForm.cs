using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ToxiqChatTester
{
    public partial class TokenDebugForm : Form
    {
        private string _token;

        public TokenDebugForm(string token)
        {
            InitializeComponent();
            _token = token;
        }

        private void TokenDebugForm_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_token))
            {
                txtTokenInfo.Text = "No token provided";
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(_token) as JwtSecurityToken;

                if (jsonToken == null)
                {
                    txtTokenInfo.Text = "Invalid token format";
                    return;
                }

                txtTokenInfo.Text = $"Token Analysis:\r\n";
                txtTokenInfo.Text += $"Issuer: {jsonToken.Issuer}\r\n";
                txtTokenInfo.Text += $"Audience: {jsonToken.Audiences.FirstOrDefault() ?? "None"}\r\n";

                // Check expiration
                DateTime expiration = jsonToken.ValidTo.ToLocalTime();
                bool isExpired = expiration < DateTime.Now;
                txtTokenInfo.Text += $"Expiration: {expiration} ({(isExpired ? "EXPIRED" : "Valid")})\r\n";

                // Check issued time
                DateTime issuedAt = jsonToken.IssuedAt.ToLocalTime();
                txtTokenInfo.Text += $"Issued At: {issuedAt}\r\n";

                // Display claims
                txtTokenInfo.Text += "\r\nClaims:\r\n";
                foreach (var claim in jsonToken.Claims)
                {
                    txtTokenInfo.Text += $"- {claim.Type}: {claim.Value}\r\n";
                }

                // Check for required claims for your API
                bool hasNameId = jsonToken.Claims.Any(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
                bool hasName = jsonToken.Claims.Any(c => c.Type == "unique_name" || c.Type == ClaimTypes.Name);

                txtTokenInfo.Text += "\r\nRequired Claims Check:\r\n";
                txtTokenInfo.Text += $"- NameIdentifier (UserId): {(hasNameId ? "Present" : "MISSING")}\r\n";
                txtTokenInfo.Text += $"- Name (Username): {(hasName ? "Present" : "MISSING")}\r\n";
            }
            catch (Exception ex)
            {
                txtTokenInfo.Text = $"Error analyzing token: {ex.Message}";
            }
        }



        private System.Windows.Forms.TextBox txtTokenInfo;
        private System.Windows.Forms.Button btnClose;
    }
}
