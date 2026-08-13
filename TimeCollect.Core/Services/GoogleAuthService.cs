using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace TimeCollect.Core.Services
{
    /// <summary>
    /// Handles the OAuth2 browser authentication flow and token management.
    /// </summary>
    public class GoogleAuthService
    {
        /// <summary>
        /// Authenticates the user and returns an initialized SheetsService.
        /// Triggers a browser login if the local token is missing or expired.
        /// </summary>
        public static SheetsService Authenticate()
        {
            // Define the required scope (Read-only access to spreadsheets)
            string[] scopes = { SheetsService.Scope.SpreadsheetsReadonly };
            string applicationName = "TimeCollect App";

            // Map the token storage to the user's Windows AppData directory
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tokenFolderPath = Path.Combine(appDataPath, "TimeCollect", "google_credentials");

            // Look for the static credentials.json in the application's execution directory
            string credentialsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "google_credentials", "credentials.json");

            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException($"The Google API credentials file was not found at: {credentialsPath}");
            }

            UserCredential credential;

            // Load the client secrets and execute the authorization flow
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                // FileDataStore automatically manages the token.json file in the specified AppData path.
                // If the token doesn't exist or is invalid, this call blocks and opens the browser.
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenFolderPath, true)).Result;
            }

            // Return the authenticated service pipeline ready for data extraction
            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }
    }
}