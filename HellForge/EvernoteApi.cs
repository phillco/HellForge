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
        private static log4net.ILog log = log4net.LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod( ).DeclaringType );

        public static bool LoginNeeded { get { return ( authentication == null ); } }

        //=======================================================
        //
        // Configuration
        //
        //=======================================================

        // No way we can obfuscate these in an open-source app...
        public const string ApiConsumerKey = "philltopia";
        public const string ApiConsumerSecret = "b1959c61538223f2";

        // Set the environment here (sandbox or www).
        public const string Host = "www.evernote.com";
        public const string BaseUrl = "https://" + Host;

        private static AuthenticationResult authentication;

        public static void Login( )
        {
            Login( Configuration.CurrentSettings.EvernoteUsername, Configuration.CurrentSettings.EvernotePassword );
        }

        /// <summary>
        /// Logs into Evernote.
        /// </summary>
        public static void Login( string username, string password )
        {
            // Login to Evernote.
            log.Info( "Logging in to Evernote..." );
            authentication = Authenticate( username, password );
        }        

        /// <summary>
        /// Fetches the first n suitable notes to tweet.
        /// </summary>
        public static List<Note> GetNotesToTweet( int numNotesWanted )
        {
            if ( LoginNeeded )
                Login( );

            // Connect to the note store.  
            log.Info( "Fetching notebooks..." );
            NoteStore.Client noteClient = new NoteStore.Client( new TBinaryProtocol( new THttpClient( new Uri( BaseUrl + "/edam/note/" + authentication.User.ShardId ) ) ) );

            // Find the specified notebook.
            List<Notebook> notebooks = noteClient.listNotebooks( authentication.AuthenticationToken );
            Notebook notebook = notebooks.Find( delegate( Notebook n ) { return n.Name == "Users in Hell"; } );

            // Get a list of the most recent notes.
            NoteList notes = noteClient.findNotes( authentication.AuthenticationToken, new NoteFilter { Ascending = true, Order = 1, NotebookGuid = notebook.Guid }, 0, 300 );
            log.Info( "Found " + notes.Notes.Count + " notes in the notebook." );
            
            // Filter down to the first n untweeted notes. Download the attached images. And return!
            return FetchResources( FilterNotes( notes.Notes, numNotesWanted), noteClient );
        }

        /// <summary>
        /// Removes notes that have already been tweeted or are too long. Limits to the first "numWanted" notes.
        /// </summary>
        public static List<Note> FilterNotes( List<Note> notes, int numWanted )
        {
            // Filter and return.
            List<Note> suitable = new List<Note>( );
            foreach ( Note note in notes )
            {
                if ( !GuidCache.Contains( note.Guid ) && note.Title.Length <= 119 )
                    suitable.Add( note );

                if ( suitable.Count == numWanted )
                    break;
            }

            return suitable;
        }

        /// <summary>
        /// Downloads the resource attachments for every note in the list.
        /// </summary>
        public static List<Note> FetchResources( List<Note> notes, NoteStore.Client noteClient )
        {
            log.Info( "Fetching resources for " + notes.Count + " note(s)..." );

            foreach( Note note in notes )
            {
                Resource fetched = noteClient.getResource( authentication.AuthenticationToken, note.Resources[0].Guid, true, false, false, false );
                note.Resources[0] = fetched;
            }

            return notes;
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
