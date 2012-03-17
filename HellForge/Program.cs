using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HellForge
{
    class Program
    {
        static void Main( string[] args )
        {
            Configuration.Initialize( );

            if ( Configuration.CurrentSettings.EvernoteUsername == Configuration.DefaultSettings.EvernoteUsername )
            {
                Console.WriteLine( "Please edit HellForge.ini to set your Evernote username and password." );
                return;
            }

            TweetMaestro.FetchAndTweet( 1 );
        }
    }
}
