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
    class Example
    {
        public const string ApiConsumerKey = "CHANGE_ME";
        public const string ApiConsumerSecret = "CHANGE_ME";

        public const string Host = "sandbox.evernote.com";
        public const string BaseUrl = "https://" + Host;

        /// <summary>
        /// Demonstrates the Evernote API by posting an example note in the user's default notebook.
        /// Be sure to register an account at http://sandbox.evernote.com (production accounts will not work here!)
        /// </summary>
        public static void PostExampleNote( string username, string password )
        {
            // Login to Evernote.
            AuthenticationResult auth = Authenticate( username, password );

            // Connect to the note store.
            NoteStore.Client noteClient = new NoteStore.Client( new TBinaryProtocol( new THttpClient( new Uri( BaseUrl + "/edam/note/" + auth.User.ShardId ) ) ) );

            // Find the default notebook.
            Notebook notebook = FindDefaultNotebook( noteClient.listNotebooks( auth.AuthenticationToken ) );

            // Create a new note in it.
            Note createdNote = noteClient.createNote( auth.AuthenticationToken, MakeDummyNote( notebook ) );

            Console.WriteLine( "Successfully created new note with GUID: " + createdNote.Guid );
        }

        /// <summary>
        /// Logs into Evernote with the given username and password.
        /// </summary>
        private static AuthenticationResult Authenticate( string username, string password )
        {
            try
            {
                UserStore.Client userClient = new UserStore.Client( new TBinaryProtocol( new THttpClient( new Uri( BaseUrl + "/edam/user" ) ) ) );

                if ( !userClient.checkVersion( "Evernote CS Example", Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MAJOR, Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MINOR ) )
                    throw new Exception( "Our version of Evernote's API is out of date. Please bug @philltopia" );

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
                Title = "Test note from HellForge!",
                Content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\">" +
                    "<en-note>Here's the Evernote logo:<br/>" +
                    "<en-media type=\"image/png\" hash=\"" + Md5HashToHex(hash) + "\"/>" +
                    "</en-note>",
            };

            // Attach the image to the note as a resource.
            note.Resources = new List<Resource>( );
            note.Resources.Add( new Resource { Mime = "image/png", Data = new Data { Size = image.Length, BodyHash = hash, Body = image } } );

            return note;
        }

        /// <summary>
        /// Given the login failure from Evernote, determines what's wrong.
        /// This is still rather crude and should actually return separate exceptions.
        /// </summary>
        private static Exception DecodeLoginFailure( EDAMUserException ex )
        {
            String parameter = ex.Parameter;
            EDAMErrorCode errorCode = ex.ErrorCode;

            string errorMessage = "Authentication failed (parameter: " + parameter + " errorCode: " + errorCode + ")" + Environment.NewLine;

            if ( errorCode == EDAMErrorCode.INVALID_AUTH )
            {
                if ( parameter == "consumerKey" )
                    errorMessage += "Your consumer key was not accepted by " + Host + Environment.NewLine;
                else if ( parameter == "username" )
                {
                    errorMessage += "You must authenticate using a username and password from " + Host + Environment.NewLine;
                    if ( Host != "www.evernote.com" )
                    {
                        errorMessage += "Note that your production Evernote account will not work on " + Host + "," + Environment.NewLine;
                        errorMessage += "You must register for a separate test account at https://" + Host + "/Registration.action" + Environment.NewLine;
                    }
                }
                else if ( parameter == "password" )
                    errorMessage += "The password that you entered is incorrect" + Environment.NewLine;
            }

            return new Exception( errorMessage );
        }

        /// <summary>
        /// Returns the hexadecimal representation of the given MD5 hash.
        /// </summary>
        private static string Md5HashToHex( byte[] hash )
        {
            return BitConverter.ToString( hash ).Replace( "-", "" ).ToLower( );
        }
    }
}
