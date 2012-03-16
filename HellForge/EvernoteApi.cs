using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Error;
using System.Security.Cryptography;
using System.IO;

namespace HellForge
{
    /// <summary>
    /// Evernote's example API code.
    /// </summary>
    class EvernoteApi
    {
        //=======================================================
        //
        // Configuration
        //
        //=======================================================

        // Insert your application's API key here (must be a "Client Application key", get one here: http://dev.evernote.com/documentation/cloud/)
        public const string ApiConsumerKey = "CHANGE_ME";
        public const string ApiConsumerSecret = "CHANGE_ME";

        // Set the environment here (sandbox or www).
        public const string Host = "sandbox.evernote.com";
        public const string BaseUrl = "https://" + Host;

        private static AuthenticationResult authentication;

        /// <summary>
        /// Logs into Evernote.
        /// Be sure to register an account at http://sandbox.evernote.com (production accounts will not work here!)
        /// </summary>
        public static void Login( string username, string password )
        {
            // Login to Evernote.
            authentication = Authenticate( username, password );           
        }

        /// <summary>
        /// Fetches the last 10 notes and tweets them.
        /// </summary>
        public static void TweetRecentNotes()
        {
             // Connect to the note store.
            NoteStore.Client noteClient = new NoteStore.Client( new TBinaryProtocol( new THttpClient( new Uri( BaseUrl + "/edam/note/" + authentication.User.ShardId ) ) ) );

            // Find the default notebook.
            Notebook notebook = FindDefaultNotebook( noteClient.listNotebooks( authentication.AuthenticationToken ) );

            // Get a list of the most recent notes.
            NoteList notes = noteClient.findNotes( authentication.AuthenticationToken, new NoteFilter { Ascending = false, Order = 1 }, 0, 10 );

            // Tweet them!
            foreach ( Note note in notes.Notes )
                TweetNote( note, noteClient, authentication );
        }

        /// <summary>
        /// Tweets the given note.
        /// </summary>
        private static void TweetNote( Note note, NoteStore.Client noteClient, AuthenticationResult auth )
        {
            string tweet = note.Title;

            if ( note.Resources != null && note.Resources.Count > 0 )
            {
                // Re-fetch the resource from the server to get its content.
                Resource fetched = noteClient.getResource( auth.AuthenticationToken, note.Resources[0].Guid, true, false, false, false );

                // Open the resource...
                if ( fetched.Data != null && fetched.Data.Body != null )
                {
                    TwitterPoster.Tweet( tweet, fetched.Data.Body );
                    return;
                }
            }
        }

        /// <summary>
        /// Logs into Evernote with the given username and password.
        /// </summary>
        private static AuthenticationResult Authenticate( string username, string password )
        {
            try
            {
                UserStore.Client userClient = new UserStore.Client( new TBinaryProtocol( new THttpClient( new Uri( BaseUrl + "/edam/user" ) ) ) );

                if ( !userClient.checkVersion( "EvernoteSharp", Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MAJOR, Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MINOR ) )
                    throw new OldApiException( );

                return userClient.authenticate( username, password, ApiConsumerKey, ApiConsumerSecret );
            }
            catch ( EDAMUserException ex )
            {
                throw DecodeLoginFailure( ex );
            }
        }

        /// <summary>
        /// Finds the user's default notebook (where new notes should go) from the list.
        /// </summary>
        private static Notebook FindDefaultNotebook( List<Notebook> notebooks )
        {
            if ( notebooks.Count == 0 )
                return null;

            Notebook defaultNotebook = notebooks.Find( delegate( Notebook n ) { return n.DefaultNotebook; } );
            if ( defaultNotebook == null )
                defaultNotebook = notebooks[0];

            return defaultNotebook;
        }

        /// <summary>
        /// Creates a sample note to be placed in the given notebook.
        /// </summary>
        private static Note MakeDummyNote( Notebook createIn )
        {
            byte[] image = File.ReadAllBytes( "enlogo.png" );
            byte[] hash = new MD5CryptoServiceProvider( ).ComputeHash( image );

            // Create the note.
            Note note = new Note
            {
                NotebookGuid = createIn.Guid,
                Title = "Test note from EvernoteSharp!",
                Content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\">" +
                    "<en-note>Here's the Evernote logo:<br/>" +
                    "<en-media type=\"image/png\" hash=\"" + Md5HashToHex( hash ) + "\"/>" +
                    "</en-note>",
            };

            // Attach the image to the note as a resource.
            note.Resources = new List<Resource>( );
            note.Resources.Add( new Resource { Mime = "image/png", Data = new Data { Size = image.Length, BodyHash = hash, Body = image } } );

            return note;
        }

        /// <summary>
        /// Determines what went wrong given the login failure from Evernote.
        /// </summary>
        private static Exception DecodeLoginFailure( EDAMUserException ex )
        {
            String invalidParameter = ex.Parameter;

            if ( ex.ErrorCode == EDAMErrorCode.INVALID_AUTH )
            {
                if ( invalidParameter == "consumerKey" )
                    return new BadConsumerKeyException( Host );
                else if ( invalidParameter == "password" )
                    return new BadPasswordException( );
                else if ( invalidParameter == "username" )
                    return new BadUsernameException( );
            }

            return new AuthenticationFailedException( "Authentication failed (parameter: " + invalidParameter + " errorCode: " + ex.ErrorCode + ")" + Environment.NewLine );
        }

        /// <summary>
        /// Returns the hexadecimal representation of the given MD5 hash.
        /// </summary>
        private static string Md5HashToHex( byte[] hash )
        {
            return BitConverter.ToString( hash ).Replace( "-", "" ).ToLower( );
        }

        //=======================================================
        //
        // Exceptions
        //
        //=======================================================

        /// <summary>A generic API failure.</summary>
        public class EvernoteApiException : Exception { public EvernoteApiException( string mesage ) : base( mesage ) { } }

        /// <summary>A generic authentication failure.</summary>
        public class AuthenticationFailedException : EvernoteApiException { public AuthenticationFailedException( string mesage ) : base( mesage ) { } }

        /// <summary>Thrown when Evernote has modified their API and this program is no longer compatible.</summary>
        public class OldApiException : AuthenticationFailedException { public OldApiException( ) : base( "This program's Evernote API is out of date." ) { } }

        /// <summary>Thrown when this application's consumer key was not recognized by Evernote.</summary>
        public class BadConsumerKeyException : AuthenticationFailedException { public BadConsumerKeyException( string host ) : base( "This program's Evernote consumer key was not accepted by " + Host + "." ) { } }

        /// <summary>Thrown when the user's username was invalid. (Remember, sandbox.evernote.com and evernote.com accounts are separate)</summary>
        public class BadUsernameException : AuthenticationFailedException { public BadUsernameException( ) : base( "The username that you entered does not exist." ) { } }

        /// <summary>Thrown when the user's password was invalid.</summary>
        public class BadPasswordException : AuthenticationFailedException { public BadPasswordException( ) : base( "The password that you entered is incorrect." ) { } }

    }
}
