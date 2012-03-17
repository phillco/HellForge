using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twitterizer;
using System.Diagnostics;

namespace HellForge
{
    /// <summary>
    /// Posts things to Twitter.
    /// </summary>
    class TwitterPoster
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod( ).DeclaringType );

        //=======================================================
        //
        // Configuration
        //
        //=======================================================

        // No way we can obfuscate these in an open-source app...
        private const string ConsumerKey = "93BtlyLKFg858DZdPgqrGw";
        private const string ConsumerSecret = "bAwtww2B0MY4FE2aiRm5MZqqFAivs1J44KI3rBIQ";

        /// <summary>
        /// Tweets the given message and image; returns the result.
        /// </summary>
        /// <param name="message"></param>
        public static bool Tweet( string message, byte[] resource )
        {
            // Check if we need to authenticate first
            if ( string.IsNullOrEmpty( Configuration.CurrentSettings.TwitterAccessToken ) || string.IsNullOrEmpty( Configuration.CurrentSettings.TwitterAccessSecret ) )
                AcquireAuthentication( );

            // Build tokens...
            OAuthTokens tokens = new OAuthTokens
            {
                 ConsumerKey = ConsumerKey,
                 ConsumerSecret = ConsumerSecret,
                 AccessToken = Configuration.CurrentSettings.TwitterAccessToken,
                 AccessTokenSecret = Configuration.CurrentSettings.TwitterAccessSecret
             };

            TwitterResponse<TwitterStatus> response = TwitterStatus.UpdateWithMedia( tokens, message, resource );
            if ( response.Result == RequestResult.Success )            
                log.Info( "Tweeted: " + message );
            else
            {
                if ( response.Result == RequestResult.Unauthorized )
                    AcquireAuthentication( );

                log.Error( "Error during tweet: " + response.Result + " / " + response.ErrorMessage );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Requests permission to integrate the application with the user's twitter account by opening the auth page on twitter.com,
        /// then asks the user to enter the PIN on the console..
        /// 
        /// The token is saved to the configuration.
        /// </summary>
        public static void AcquireAuthentication( )
        {
            log.Info( "Authenticating aplication with Twitter..." );

            // Obtain a request token and calculate the URL to run.
            OAuthTokenResponse requestToken = OAuthUtility.GetRequestToken( ConsumerKey, ConsumerSecret, "oob" );
            Uri authorizationUri = OAuthUtility.BuildAuthorizationUri( requestToken.Token );

            // Open the URL in a browser.
            System.Diagnostics.Process.Start( authorizationUri.ToString( ) );

            // Have the user enter the PIN.
            Console.Write( "Please approve the application from Twitter and enter the PIN here: " );
            string pin = Console.ReadLine( );

            // Calculate the access token and save it.
            try
            {
                OAuthTokenResponse token = OAuthUtility.GetAccessToken( ConsumerKey, ConsumerSecret, requestToken.Token, pin );
                Configuration.CurrentSettings.TwitterAccessToken = token.Token;
                Configuration.CurrentSettings.TwitterAccessSecret = token.TokenSecret;
                Configuration.CurrentSettings.Save( );
            }
            catch ( TwitterizerException )
            {
                Console.WriteLine( "Invalid PIN or unable to verify..." );
            }
        }
    }
}
